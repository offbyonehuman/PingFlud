using Microsoft.UI.Xaml;
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
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (!SettingsDraft.TryConvertInteger(MaxOutstandingBox.Value, 1, 1024, out var maxOutstanding) ||
            !SettingsDraft.TryConvertInteger(TimeoutBox.Value, 1, 120000, out var timeout) ||
            !SettingsDraft.TryConvertInteger(PingsBox.Value, 1, 10, out var pings) ||
            !SettingsDraft.TryConvertInteger(TtlBox.Value, 1, 255, out var ttl) ||
            !SettingsDraft.TryConvertInteger(DelayBox.Value, 0, 60000, out var delay) ||
            !SettingsDraft.TryConvertInteger(ExpansionBox.Value, 1, 1000000, out var expansionCap) ||
            !SettingsDraft.TryConvertInteger(DnsTimeoutBox.Value, 1, 30000, out var dnsTimeout))
        {
            args.Cancel = true;
            ValidationError.Message = "Enter whole numbers within the displayed ranges.";
            ValidationError.IsOpen = true;
            return;
        }

        var draft = SettingsDraft.From(_state);
        draft.MaxOutstanding = maxOutstanding;
        draft.TimeoutMs = timeout;
        draft.PingsPerNode = pings;
        draft.Ttl = ttl;
        draft.DelayMs = delay;
        draft.ExpansionCap = expansionCap;
        draft.DnsTimeoutMs = dnsTimeout;
        draft.Payload = PayloadBox.Text;
        draft.DontFragment = DontFragmentBox.IsChecked == true;
        draft.ResolveRespondingOnly = ResolveRespondingOnlyBox.IsChecked == true;

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
