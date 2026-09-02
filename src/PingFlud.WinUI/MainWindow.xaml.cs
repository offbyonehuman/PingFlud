using System.Collections.ObjectModel;
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
    private bool _syncingResultSelection;

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
        TargetsBox.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(TargetsBox_KeyDown), true);

        ApplyState();
        SystemBackdrop = new MicaBackdrop();
        ViewModel.Results.CollectionChanged += (_, _) =>
        {
            EmptyState.Visibility = ViewModel.Results.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
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
        UpdateThemeResources();
        var isDark = _state.ThemeName == AppearanceModes.DarkMode;
        ThemeToggleButton.IsChecked = isDark;
        ThemeIcon.Glyph = isDark ? "\uE708" : "\uE706";
        var elementTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;
        RootGrid.RequestedTheme = elementTheme;
        Navigation.RequestedTheme = elementTheme;
    }

    private void UpdateThemeResources()
    {
        var resources = Microsoft.UI.Xaml.Application.Current.Resources;

        ApplyThemeResources(resources, ToThemePalette(AppearanceModes.Get(_state.ThemeName)));
    }

    private static ThemePalette ToThemePalette(AppearancePalette palette) => new(
        ToColor(palette.WindowBackground),
        ToColor(palette.Surface),
        ToColor(palette.SurfaceRaised),
        ToColor(palette.Border),
        ToColor(palette.MutedForeground),
        ToColor(palette.Header),
        ToColor(palette.Accent),
        ToColor(palette.AccentForeground),
        ToColor(palette.Selection),
        ToColor(palette.Success),
        ToColor(palette.Danger));

    private static Color ToColor(uint argb) => Color.FromArgb(
        (byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);

    private static void ApplyThemeResources(ResourceDictionary resources, ThemePalette palette)
    {
        resources["GraphiteWindowColor"] = palette.WindowBackground;
        resources["GraphiteSurfaceColor"] = palette.Surface;
        resources["GraphiteRaisedColor"] = palette.RaisedSurface;
        resources["GraphiteBorderColor"] = palette.Border;
        resources["GraphiteMutedColor"] = palette.MutedText;
        resources["AccentColor"] = palette.Accent;
        resources["SelectionColor"] = palette.Selection;
        resources["AccentForegroundColor"] = palette.AccentForeground;
        resources["AccentBrush"] = new SolidColorBrush(palette.Accent);
        resources["SelectionBrush"] = new SolidColorBrush(palette.Selection);
        resources["AccentForegroundBrush"] = new SolidColorBrush(palette.AccentForeground);
        resources["ToggleButtonBackground"] = new SolidColorBrush(palette.Accent);
        resources["ToggleButtonBackgroundPointerOver"] = new SolidColorBrush(palette.Accent);
        resources["ToggleButtonBackgroundPressed"] = new SolidColorBrush(palette.Accent);
        resources["ToggleButtonForeground"] = new SolidColorBrush(palette.AccentForeground);
        resources["ToggleButtonForegroundPointerOver"] = new SolidColorBrush(palette.AccentForeground);
        resources["ToggleButtonForegroundPressed"] = new SolidColorBrush(palette.AccentForeground);
        resources["ToggleButtonBorderBrush"] = new SolidColorBrush(palette.Accent);
        resources["ToggleButtonBorderBrushPointerOver"] = new SolidColorBrush(palette.Accent);
        resources["ToggleButtonBorderBrushPressed"] = new SolidColorBrush(palette.Accent);
        resources["ToggleButtonBackgroundChecked"] = new SolidColorBrush(palette.Accent);
        resources["ToggleButtonBackgroundCheckedPointerOver"] = new SolidColorBrush(palette.Accent);
        resources["ToggleButtonBackgroundCheckedPressed"] = new SolidColorBrush(palette.Accent);
        resources["ToggleButtonForegroundChecked"] = new SolidColorBrush(palette.AccentForeground);
        resources["ToggleButtonForegroundCheckedPointerOver"] = new SolidColorBrush(palette.AccentForeground);
        resources["ToggleButtonForegroundCheckedPressed"] = new SolidColorBrush(palette.AccentForeground);
        resources["ToggleButtonBorderBrushChecked"] = new SolidColorBrush(palette.Accent);
        resources["ToggleButtonBorderBrushCheckedPointerOver"] = new SolidColorBrush(palette.Accent);
        resources["ToggleButtonBorderBrushCheckedPressed"] = new SolidColorBrush(palette.Accent);
        resources["SystemAccentColor"] = palette.Accent;
        resources["NavigationViewSelectionIndicatorForeground"] = new SolidColorBrush(palette.Accent);
        resources["NavigationViewItemBackgroundSelected"] = new SolidColorBrush(palette.Selection);
        resources["NavigationViewItemBackgroundSelectedPointerOver"] = new SolidColorBrush(palette.Selection);
        resources["SystemControlBackgroundAccentBrush"] = new SolidColorBrush(palette.Accent);
        resources["SystemControlForegroundAccentBrush"] = new SolidColorBrush(palette.Accent);
        resources["SystemControlHighlightAccentBrush"] = new SolidColorBrush(palette.Accent);
        resources["SystemControlHighlightAltAccentBrush"] = new SolidColorBrush(palette.Accent);
        resources["SystemControlHighlightListAccentLowBrush"] = new SolidColorBrush(palette.Selection);
        resources["SystemControlHighlightListAccentMediumBrush"] = new SolidColorBrush(palette.Accent);
        resources["SystemControlHighlightListAccentHighBrush"] = new SolidColorBrush(palette.Accent);
        resources["ApplicationPageBackgroundThemeBrush"] = new SolidColorBrush(palette.WindowBackground);
        resources["CardBackgroundBrush"] = new SolidColorBrush(palette.Surface);
        resources["CardBorderBrush"] = new SolidColorBrush(palette.Border);
        resources["MutedTextBrush"] = new SolidColorBrush(palette.MutedText);
        resources["SuccessBrush"] = new SolidColorBrush(palette.Success);
        resources["DangerBrush"] = new SolidColorBrush(palette.Danger);
        resources["ProgressBrush"] = new SolidColorBrush(palette.Success);
        resources["ResultsHeaderBackgroundBrush"] = new SolidColorBrush(palette.Header);
    }

    private readonly record struct ThemePalette(
        Color WindowBackground,
        Color Surface,
        Color RaisedSurface,
        Color Border,
        Color MutedText,
        Color Header,
        Color Accent,
        Color AccentForeground,
        Color Selection,
        Color Success,
        Color Danger);

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
            ViewModel.Targets = await new TargetListImporter().ImportAsync(file.Path);
            TargetsBox.Focus(FocusState.Programmatic);
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Import failed", ex.Message);
        }
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
        {
            XamlRoot = Content.XamlRoot,
            RequestedTheme = Navigation.RequestedTheme,
            Title = title,
            Content = message,
            CloseButtonText = "OK"
        };
        await dialog.ShowAsync();
    }

    private async void SettingsButton_Click(object sender, RoutedEventArgs e) =>
        await ShowSettingsAsync();

    private void ThemeToggleButton_Checked(object sender, RoutedEventArgs e) => SetTheme(AppearanceModes.DarkMode);

    private void ThemeToggleButton_Unchecked(object sender, RoutedEventArgs e) => SetTheme(AppearanceModes.LightMode);

    private void SetTheme(string themeName)
    {
        if (_state.ThemeName == themeName) return;
        _state.ThemeName = themeName;
        ApplyState();
        SaveStateSafely();
    }

    private async void Navigation_SelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer?.Tag is not string destination || destination == "Workspace") return;

        sender.SelectedItem = sender.MenuItems[0];
        switch (destination)
        {
            case "Settings":
                await ShowSettingsAsync();
                break;
            case "Documentation":
                await ShowDocumentationAsync();
                break;
            case "About":
                await ShowMessageAsync(
                    "About Ping Flud",
                    "Ping Flud 1.5.3\nNetwork reachability testing and troubleshooting.\n\nCopyright © 2026 OffByOneHuman\nLicensed under MIT.");
                break;
        }
    }

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

    private void ResultSelectionCheckBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && FindVisualAncestor<ListViewItem>(checkBox) is { } item)
            SetCheckBoxState(checkBox, item.IsSelected);
    }

    private void ResultSelectionCheckBox_Checked(object sender, RoutedEventArgs e) =>
        SetResultSelection(sender, true);

    private void ResultSelectionCheckBox_Unchecked(object sender, RoutedEventArgs e) =>
        SetResultSelection(sender, false);

    private void SetResultSelection(object sender, bool isSelected)
    {
        if (_syncingResultSelection || sender is not CheckBox checkBox) return;
        if (FindVisualAncestor<ListViewItem>(checkBox) is { } item)
            item.IsSelected = isSelected;
    }

    private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        foreach (var item in e.AddedItems.Cast<object>().Concat(e.RemovedItems))
        {
            if (ResultsList.ContainerFromItem(item) is ListViewItem container &&
                FindVisualDescendant<CheckBox>(container) is { } checkBox)
                SetCheckBoxState(checkBox, ResultsList.SelectedItems.Contains(item));
        }
    }

    private void SetCheckBoxState(CheckBox checkBox, bool isSelected)
    {
        if (_syncingResultSelection || checkBox.IsChecked == isSelected) return;
        _syncingResultSelection = true;
        checkBox.IsChecked = isSelected;
        _syncingResultSelection = false;
    }

    private static T? FindVisualAncestor<T>(DependencyObject? element) where T : DependencyObject
    {
        while (element is not null)
        {
            if (element is T match) return match;
            element = VisualTreeHelper.GetParent(element);
        }
        return null;
    }

    private static T? FindVisualDescendant<T>(DependencyObject element) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(element); index++)
        {
            var child = VisualTreeHelper.GetChild(element, index);
            if (child is T match) return match;
            if (FindVisualDescendant<T>(child) is { } descendant) return descendant;
        }
        return null;
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
        if (selected.Length == 0) selected = ViewModel.Results.ToArray();
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
        catch (Exception ex)
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

}
