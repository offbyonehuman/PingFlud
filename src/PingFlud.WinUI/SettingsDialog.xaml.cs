using Microsoft.UI.Xaml.Controls;
using PingFlud.Application;

namespace PingFlud.WinUI;

public sealed partial class SettingsDialog : ContentDialog
{
    private readonly AppState _state;

    public bool Saved { get; private set; }

    public SettingsDialog(AppState state)
    {
        _state = state;
        InitializeComponent();

        var draft = SettingsDraft.From(state);
        MaxOutstandingBox.Value = draft.MaxOutstanding;
        TimeoutBox.Value = draft.TimeoutMs;
        PingsBox.Value = draft.PingsPerNode;
        TtlBox.Value = draft.Ttl;
        DelayBox.Value = draft.DelayMs;
        ExpansionBox.Value = draft.ExpansionCap;
        DnsTimeoutBox.Value = draft.DnsTimeoutMs;
        PayloadBox.Text = draft.Payload;
        DontFragmentBox.IsChecked = draft.DontFragment;
        ResolveRespondingOnlyBox.IsChecked = draft.ResolveRespondingOnly;
        ThemeBox.ItemsSource = new[] { "Graphite", "Midnight", "Nebula", "Daylight" };
        ThemeBox.SelectedItem = draft.ThemeName;
        TitleBox.Text = draft.Title;
        SubtitleBox.Text = draft.Subtitle;
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var draft = new SettingsDraft
        {
            MaxOutstanding = (int)MaxOutstandingBox.Value,
            TimeoutMs = (int)TimeoutBox.Value,
            PingsPerNode = (int)PingsBox.Value,
            Ttl = (int)TtlBox.Value,
            DelayMs = (int)DelayBox.Value,
            ExpansionCap = (int)ExpansionBox.Value,
            DnsTimeoutMs = (int)DnsTimeoutBox.Value,
            Payload = PayloadBox.Text,
            DontFragment = DontFragmentBox.IsChecked == true,
            ResolveRespondingOnly = ResolveRespondingOnlyBox.IsChecked == true,
            ThemeName = ThemeBox.SelectedItem as string ?? "Graphite",
            Title = TitleBox.Text,
            Subtitle = SubtitleBox.Text
        };

        if (draft.TryApply(_state, out var error))
        {
            Saved = true;
            return;
        }

        args.Cancel = true;
        ValidationError.Message = error;
        ValidationError.IsOpen = true;
    }
}
