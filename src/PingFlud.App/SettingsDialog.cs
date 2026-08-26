using PingFlud.Core;

namespace PingFlud.App;

internal sealed class SettingsDialog : Form
{
    private readonly AppState _state;
    private readonly ThemePalette _theme;

    public SettingsDialog(AppState state, ThemePalette? theme = null)
    {
        _state = state;
        _theme = theme ?? ThemeCatalog.Get(state.ThemeName);

        Text = "Scan settings";
        Size = new Size(680, 720);
        MinimumSize = new Size(640, 680);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        Font = new Font("Segoe UI", 9.5F);
        BackColor = _theme.WindowBackground;
        ForeColor = _theme.Foreground;

        BuildContent();
    }

    private void BuildContent()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22),
            ColumnCount = 1,
            RowCount = 3,
            BackColor = _theme.WindowBackground
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));

        var heading = new Panel { Dock = DockStyle.Fill, BackColor = _theme.WindowBackground };
        heading.Controls.Add(new Label
        {
            Text = "Scan settings",
            Font = new Font("Segoe UI Semibold", 18F),
            ForeColor = _theme.Foreground,
            AutoSize = true,
            Location = new Point(0, 0)
        });
        heading.Controls.Add(new Label
        {
            Text = "Tune ICMP behavior, safety limits, and window labels.",
            ForeColor = _theme.MutedForeground,
            AutoSize = true,
            Location = new Point(2, 36)
        });

        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = _theme.Surface,
            Padding = new Padding(18, 14, 18, 14),
            ColumnCount = 2,
            RowCount = 12,
            AutoScroll = true,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56));

        var maxOutstanding = CreateNumeric(_state.Settings.MaxOutstanding, 1, 1024);
        var timeout = CreateNumeric(_state.Settings.TimeoutMs, 1, 120000);
        var pings = CreateNumeric(_state.Settings.PingsPerNode, 1, 10);
        var ttl = CreateNumeric(_state.Settings.Ttl, 1, 255);
        var delay = CreateNumeric(_state.Settings.DelayMs, 0, 60000);
        var cap = CreateNumeric(_state.Settings.ExpansionCap, 1, 1000000);
        var payload = CreateTextBox(_state.Settings.Payload);
        var title = CreateTextBox(_state.Title);
        var subtitle = CreateTextBox(_state.Subtitle);
        var dnsTimeout = CreateNumeric(_state.Settings.DnsTimeoutMs, 1, 30000);
        var dontFragment = new CheckBox
        {
            Text = "Don't Fragment (MTU testing)",
            Checked = _state.Settings.DontFragment,
            AutoSize = true,
            ForeColor = _theme.Foreground,
            BackColor = _theme.Surface
        };
        var resolveResponding = new CheckBox
        {
            Text = "Reverse DNS for responding hosts only",
            Checked = _state.Settings.ResolveRespondingOnly,
            AutoSize = true,
            ForeColor = _theme.Foreground,
            BackColor = _theme.Surface
        };

        AddField(fields, 0, "Max outstanding packets", "Concurrent probes", maxOutstanding);
        AddField(fields, 1, "Timeout", "Milliseconds per ping", timeout);
        AddField(fields, 2, "Pings per target", "Attempts before completion", pings);
        AddField(fields, 3, "Packet TTL", "Maximum router hops", ttl);
        AddField(fields, 4, "Delay between pings", "Milliseconds", delay);
        AddField(fields, 5, "Expansion safety cap", "Maximum generated targets", cap);
        AddField(fields, 6, "ICMP payload", "UTF-8 text, up to 60 KB", payload);
        AddField(fields, 7, "Window title", "Shown in the title bar", title);
        AddField(fields, 8, "Subtitle", "Shown below the product name", subtitle);
        AddField(fields, 9, "DNS timeout", "Milliseconds per reverse DNS lookup", dnsTimeout);

        // CheckBoxes span both columns
        fields.Controls.Add(dontFragment, 0, 10);
        fields.SetColumnSpan(dontFragment, 2);
        fields.Controls.Add(resolveResponding, 0, 11);
        fields.SetColumnSpan(resolveResponding, 2);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 10, 0, 0),
            BackColor = _theme.WindowBackground
        };
        var save = CreateButton("Save settings", true);
        var cancel = CreateButton("Cancel", false);
        save.DialogResult = DialogResult.OK;
        cancel.DialogResult = DialogResult.Cancel;
        actions.Controls.Add(save);
        actions.Controls.Add(cancel);
        AcceptButton = save;
        CancelButton = cancel;

        save.Click += (_, _) =>
        {
            try
            {
                var candidate = new ScanSettings
                {
                    MaxOutstanding = (int)maxOutstanding.Value,
                    TimeoutMs = (int)timeout.Value,
                    PingsPerNode = (int)pings.Value,
                    Ttl = (int)ttl.Value,
                    DelayMs = (int)delay.Value,
                    ExpansionCap = (int)cap.Value,
                    Payload = payload.Text,
                    DnsTimeoutMs = (int)dnsTimeout.Value,
                    DontFragment = dontFragment.Checked,
                    ResolveRespondingOnly = resolveResponding.Checked
                };
                candidate.Validate();
                _state.Settings = candidate;
                _state.Title = string.IsNullOrWhiteSpace(title.Text) ? "Ping Flud" : title.Text.Trim();
                _state.Subtitle = subtitle.Text.Trim();
            }
            catch (Exception ex)
            {
                DialogResult = DialogResult.None;
                MessageBox.Show(this, ex.Message, "Invalid settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };

        root.Controls.Add(heading, 0, 0);
        root.Controls.Add(fields, 0, 1);
        root.Controls.Add(actions, 0, 2);
        Controls.Add(root);
    }

    private void AddField(TableLayoutPanel fields, int row, string title, string hint, Control input)
    {
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        var labelPanel = new Panel { Dock = DockStyle.Fill, BackColor = _theme.Surface };
        labelPanel.Controls.Add(new Label
        {
            Text = title,
            ForeColor = _theme.Foreground,
            Font = new Font("Segoe UI Semibold", 9.5F),
            AutoSize = true,
            Location = new Point(0, 3)
        });
        labelPanel.Controls.Add(new Label
        {
            Text = hint,
            ForeColor = _theme.MutedForeground,
            Font = new Font("Segoe UI", 8F),
            AutoSize = true,
            Location = new Point(0, 23)
        });
        fields.Controls.Add(labelPanel, 0, row);
        fields.Controls.Add(input, 1, row);
    }

    private TextBox CreateTextBox(string value) => new()
    {
        Text = value,
        Dock = DockStyle.Fill,
        Margin = new Padding(4, 7, 0, 7),
        BorderStyle = BorderStyle.FixedSingle,
        BackColor = _theme.SurfaceRaised,
        ForeColor = _theme.Foreground
    };

    private Button CreateButton(string text, bool primary)
    {
        var button = new RoundedButton
        {
            Text = text,
            AutoSize = false,
            Height = 36,
            Padding = new Padding(12, 0, 12, 0),
            BackColor = primary ? _theme.Accent : _theme.SurfaceRaised,
            ForeColor = primary ? _theme.AccentForeground : _theme.Foreground,
            Margin = new Padding(8, 0, 0, 0),
            TextAlign = ContentAlignment.MiddleCenter
        };
        button.Size = button.GetPreferredSize(Size.Empty);
        button.FlatAppearance.BorderColor = primary ? _theme.Accent : _theme.Border;
        return button;
    }

    internal static RoundedNumericUpDown CreateNumeric(decimal value, decimal min, decimal max)
    {
        var control = new RoundedNumericUpDown
        {
            Minimum = min,
            Maximum = max,
            ThousandsSeparator = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(4, 7, 0, 7),
            BorderStyle = BorderStyle.FixedSingle
        };
        control.Value = Math.Clamp(value, min, max);
        return control;
    }
}
