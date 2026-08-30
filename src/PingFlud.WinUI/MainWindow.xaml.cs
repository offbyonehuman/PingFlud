using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using PingFlud.Application;
using PingFlud.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI;

namespace PingFlud.WinUI;

public sealed partial class MainWindow : Window
{
    private readonly IAppStateStore _stateStore;
    private readonly AppState _state;

    public MainViewModel ViewModel { get; }
    public ObservableCollection<string> TargetHistory { get; }

    public MainWindow()
    {
        _stateStore = new JsonAppStateStore();
        _state = _stateStore.Load();
        TargetHistory = new ObservableCollection<string>(_state.History);
        ViewModel = new MainViewModel(new PingScanRunner(), new DispatcherQueueUiDispatcher(DispatcherQueue))
        {
            Settings = _state.Settings
        };
        InitializeComponent();

        ApplyState();
        SystemBackdrop = new MicaBackdrop();
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        ViewModel.Results.CollectionChanged += (_, _) =>
        {
            EmptyState.Visibility = ViewModel.Results.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            Bindings.Update();
        };
        Closed += (_, _) =>
        {
            ViewModel.Dispose();
            _state.Settings = ViewModel.Settings;
            SaveStateSafely();
        };

        ConfigureWindow();
    }

    private void ConfigureWindow()
    {
        var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
        if (displayArea is null) return;

        var width = Math.Min(1420, displayArea.WorkArea.Width);
        var height = Math.Min(900, displayArea.WorkArea.Height);
        appWindow.Resize(new SizeInt32(width, height));
        var x = displayArea.WorkArea.X + Math.Max(0, (displayArea.WorkArea.Width - width) / 2);
        var y = displayArea.WorkArea.Y + Math.Max(0, (displayArea.WorkArea.Height - height) / 2);
        appWindow.Move(new PointInt32(x, y));
    }

    private void ApplyState()
    {
        Title = _state.Title;
        TitleText.Text = _state.Title;
        SubtitleText.Text = _state.Subtitle;
        Navigation.RequestedTheme = _state.ThemeName == "Daylight" ? ElementTheme.Light : ElementTheme.Dark;
        UpdateThemeResources();
    }

    private void UpdateThemeResources()
    {
        var resources = Microsoft.UI.Xaml.Application.Current.Resources;

        switch (_state.ThemeName)
        {
            case "Midnight":
                ApplyMidnightTheme(resources);
                break;
            case "Nebula":
                ApplyNebulaTheme(resources);
                break;
            case "Daylight":
                ApplyDaylightTheme(resources);
                break;
            default:
                ApplyGraphiteTheme(resources);
                break;
        }
    }

    private static void ApplyGraphiteTheme(ResourceDictionary resources)
    {
        resources["GraphiteWindowColor"] = Color.FromArgb(255, 18, 18, 18);
        resources["GraphiteSurfaceColor"] = Color.FromArgb(255, 30, 30, 30);
        resources["GraphiteRaisedColor"] = Color.FromArgb(255, 42, 42, 42);
        resources["GraphiteBorderColor"] = Color.FromArgb(255, 70, 70, 70);
        resources["GraphiteMutedColor"] = Color.FromArgb(255, 180, 180, 180);
        resources["ApplicationPageBackgroundThemeBrush"] = new SolidColorBrush((Color)resources["GraphiteWindowColor"]);
        resources["CardBackgroundBrush"] = new SolidColorBrush((Color)resources["GraphiteSurfaceColor"]);
        resources["CardBorderBrush"] = new SolidColorBrush((Color)resources["GraphiteBorderColor"]);
        resources["MutedTextBrush"] = new SolidColorBrush((Color)resources["GraphiteMutedColor"]);
    }

    private static void ApplyMidnightTheme(ResourceDictionary resources)
    {
        resources["ApplicationPageBackgroundThemeBrush"] = new SolidColorBrush(Color.FromArgb(255, 26, 32, 48));
        resources["CardBackgroundBrush"] = new SolidColorBrush(Color.FromArgb(255, 35, 39, 51));
        resources["CardBorderBrush"] = new SolidColorBrush(Color.FromArgb(255, 72, 78, 96));
        resources["MutedTextBrush"] = new SolidColorBrush(Color.FromArgb(255, 192, 192, 192));
    }

    private static void ApplyNebulaTheme(ResourceDictionary resources)
    {
        resources["ApplicationPageBackgroundThemeBrush"] = new SolidColorBrush(Color.FromArgb(255, 18, 18, 18));
        resources["CardBackgroundBrush"] = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30));
        resources["CardBorderBrush"] = new SolidColorBrush(Color.FromArgb(255, 70, 70, 70));
        resources["MutedTextBrush"] = new SolidColorBrush(Color.FromArgb(255, 180, 180, 180));
    }

    private static void ApplyDaylightTheme(ResourceDictionary resources)
    {
        resources["ApplicationPageBackgroundThemeBrush"] = new SolidColorBrush(Color.FromArgb(255, 243, 243, 243));
        resources["CardBackgroundBrush"] = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
        resources["CardBorderBrush"] = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200));
        resources["MutedTextBrush"] = new SolidColorBrush(Color.FromArgb(255, 120, 120, 120));
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e) =>
        await StartAndPersistAsync();

    private async Task StartAndPersistAsync()
    {
        await ViewModel.StartScanAsync();
        if (ViewModel.Status != "Scan complete") return;

        _state.Remember(ViewModel.Targets);
        TargetHistory.Clear();
        foreach (var target in _state.History) TargetHistory.Add(target);
        _state.Settings = ViewModel.Settings;
        SaveStateSafely();
    }

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            ViewMode = PickerViewMode.List
        };
        picker.FileTypeFilter.Add(".txt");
        picker.FileTypeFilter.Add(".csv");
        WinRT.Interop.InitializeWithWindow.Initialize(
            picker,
            WinRT.Interop.WindowNative.GetWindowHandle(this));

        var file = await picker.PickSingleFileAsync();
        if (file is null) return;

        try
        {
            ViewModel.Targets = new TargetListImporter().Import(file.Path);
            TargetsBox.Focus(FocusState.Programmatic);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await ShowMessageAsync("Import failed", ex.Message);
        }
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
        {
            XamlRoot = Content.XamlRoot,
            Title = title,
            Content = message,
            CloseButtonText = "OK"
        };
        await dialog.ShowAsync();
    }

    private async void SettingsButton_Click(object sender, RoutedEventArgs e) =>
        await ShowSettingsAsync();

    private async void SettingsNavigation_Tapped(object sender, TappedRoutedEventArgs e) =>
        await ShowSettingsAsync();

    private async void DocumentationNavigation_Tapped(object sender, TappedRoutedEventArgs e) =>
        await ShowDocumentationAsync();

    private async void AboutNavigation_Tapped(object sender, TappedRoutedEventArgs e) =>
        await ShowMessageAsync(
            "About Ping Flud",
            "Ping Flud 1.5.2\nFast, transparent network reachability checks.\n\nCopyright © 2026 OffByOneHuman\nLicensed under MIT.");

    private async void SyntaxHelpButton_Click(object sender, RoutedEventArgs e) =>
        await ShowDocumentationAsync();

    private Task ShowDocumentationAsync() => ShowMessageAsync(
        "Target syntax",
        "Enter one or more targets separated by commas or new lines.\n\n" +
        "Examples:\n• 192.168.1.1\n• server.example\n• 10.0.0.0/24\n• 192.168.1.10-192.168.1.20\n\n" +
        "Use Scan settings to adjust timeout, concurrency, retries, DNS lookup, and safety limits.");

    private async Task ShowSettingsAsync()
    {
        var dialog = new SettingsDialog(_state)
        {
            XamlRoot = Content.XamlRoot,
            RequestedTheme = Navigation.RequestedTheme
        };
        await dialog.ShowAsync();
        if (!dialog.Saved) return;

        ViewModel.Settings = _state.Settings;
        ApplyState();
        SaveStateSafely();
    }

    private void StopButton_Click(object sender, RoutedEventArgs e) =>
        ViewModel.StopScan();

    private void FilterBox_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
    {
        ViewModel.Filter = FilterBox.SelectedIndex switch
        {
            1 => ResultFilter.Responding,
            2 => ResultFilter.NotResponding,
            _ => ResultFilter.All
        };
    }

    private void SearchBox_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e) =>
        ViewModel.Search = SearchBox.Text;

    private void ResultHeader_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string propertyName }) ViewModel.SortBy(propertyName);
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e) =>
        ViewModel.ClearResults();

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = ResultsList.SelectedItems.Cast<ScanResult>().ToArray();
        if (selected.Length == 0) return;

        var text = string.Join(Environment.NewLine, selected.Select(row =>
            string.Join('\t', row.Target, row.Address, row.HostName, row.Status, row.RoundtripMs?.ToString() ?? string.Empty)));
        var package = new DataPackage();
        package.SetText(text);
        Clipboard.SetContent(package);
    }

    private async void TargetsBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter || !ViewModel.CanStart) return;
        e.Handled = true;
        await StartAndPersistAsync();
    }

    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = $"pingflud-{DateTime.Now:yyyyMMdd-HHmmss}"
        };
        picker.FileTypeChoices.Add("CSV (comma-separated)", new List<string> { ".csv" });
        picker.FileTypeChoices.Add("HTML (web report)", new List<string> { ".html" });
        picker.FileTypeChoices.Add("HTML (Excel)", new List<string> { ".xhtml" });
        picker.FileTypeChoices.Add("Text (tab-separated)", new List<string> { ".txt" });
        picker.FileTypeChoices.Add("PDF document", new List<string> { ".pdf" });
        picker.FileTypeChoices.Add("PNG image", new List<string> { ".png" });
        WinRT.Interop.InitializeWithWindow.Initialize(
            picker,
            WinRT.Interop.WindowNative.GetWindowHandle(this));

        var file = await picker.PickSaveFileAsync();
        if (file is null) return;

        ViewModel.ExportKind = file.FileType switch
        {
            ".csv" => ExportKind.Csv,
            ".html" => ExportKind.Html,
            ".xhtml" => ExportKind.SpreadsheetHtml,
            ".txt" => ExportKind.Txt,
            ".pdf" => ExportKind.Pdf,
            ".png" => ExportKind.PngImage,
            _ => ExportKind.Csv
        };
        ViewModel.ExportPath = file.Path;

        try
        {
            await ((AsyncRelayCommand)ViewModel.ExportCommand).ExecuteAsync();
            if (ViewModel.Status == "Export complete")
                await ShowMessageAsync("Export complete", $"Results written to {file.Path}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await ShowMessageAsync("Export failed", ex.Message);
        }
    }

    private void SaveStateSafely()
    {
        try
        {
            _stateStore.Save(_state);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Persistence is best-effort; a failed save must not prevent shutdown or scanning.
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e) =>
        Bindings.Update();
}
