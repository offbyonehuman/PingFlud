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
        var draft = SettingsDraft.From(_state);
        draft.MaxOutstanding = (int)MaxOutstandingBox.Value;
        draft.TimeoutMs = (int)TimeoutBox.Value;
        draft.PingsPerNode = (int)PingsBox.Value;
        draft.Ttl = (int)TtlBox.Value;
        draft.DelayMs = (int)DelayBox.Value;
        draft.ExpansionCap = (int)ExpansionBox.Value;
        draft.DnsTimeoutMs = (int)DnsTimeoutBox.Value;
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
