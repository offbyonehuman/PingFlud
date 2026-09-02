using System.ComponentModel;
using System.Drawing.Imaging;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using PingFlud.Application;
using PingFlud.Core;

namespace PingFlud.App;

public sealed class AppState
{
    public ScanSettings Settings { get; set; } = new();
    public List<string> History { get; set; } = [];
    public string Title { get; set; } = "Ping Flud";
    public string Subtitle { get; set; } = PingFlud.Application.AppState.DefaultSubtitle;
    public string ThemeName { get; set; } = "Graphite";
}

public sealed class MainForm : Form
{
    private static readonly string DisplayVersion = GetDisplayVersion();

    private const string DocumentationText =
        "TARGET SYNTAX\n\n" +
        "Single address or host\n  192.168.1.20   server.example   ::1\n\n" +
        "Inclusive IPv4 range\n  192.168.1.10-192.168.1.25\n  Scans every address between the two endpoints, including both endpoints.\n\n" +
        "IPv4 CIDR\n  192.168.1.0/24\n  The prefix defines the network block; /24 expands to 256 IPv4 addresses.\n\n" +
        "Question-mark wildcard (?)\n  Matches exactly one decimal digit.\n  192.168.1.?  →  .0 through .9\n  192.168.1.1? →  .10 through .19\n\n" +
        "Asterisk wildcard (*)\n  Matches zero or more decimal digits while keeping the octet in 0–255.\n  192.168.1.* → .0 through .255\n\n" +
        "Combine targets with commas or new lines. Wildcards apply to IPv4 only. " +
        "Every expansion is limited by the safety cap in Settings.\n\n" +
        "RESULTS\n\n● Responding: at least one ICMP reply.\n○ Not responding: no reply within the configured attempts and timeout.";

    private readonly string _statePath;
    private readonly BindingList<ScanResult> _allResults = [];
    private readonly BindingSource _source = new();
    private readonly List<Button> _buttons = [];
    private readonly List<Control> _resultActions = [];
    private readonly List<Label> _labels = [];
    private readonly List<CardPanel> _cards = [];
    private readonly List<Control> _inputs = [];

    private readonly ComboBox _targets = new() { DropDownStyle = ComboBoxStyle.DropDown, AccessibleName = "Targets" };
    private readonly ComboBox _filter = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 146 };
    private readonly TextBox _search = new() { PlaceholderText = "Search target, IP, DNS, or status…" };
    private readonly ToolTip _toolTip = new() { AutoPopDelay = 12000, InitialDelay = 350, ReshowDelay = 150, ShowAlways = true };
    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = true,
        AutoGenerateColumns = false,
        BorderStyle = BorderStyle.None,
        RowHeadersVisible = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
    };
    private readonly ToolStripStatusLabel _status = new("Ready");
    private readonly ToolStripStatusLabel _summary = new("0 targets");
    private readonly ToolStripProgressBar _progress = new() { Minimum = 0, Maximum = 100, Width = 170 };
    private readonly Label _titleLabel = new() { AutoSize = true };
    private readonly Label _subtitleLabel = new() { AutoSize = true };

    private AppState _state;
    private ThemePalette _theme;
    private TableLayoutPanel _shell = null!;
    private TableLayoutPanel _root = null!;
    private Panel _sidebar = null!;
    private MenuStrip _menu = null!;
    private ToolStripMenuItem? _appearanceToggle;
    private Panel _header = null!;
    private Panel _resultsToolbar = null!;
    private CardPanel _scanCard = null!;
    private CardPanel _resultsCard = null!;
    private Panel _emptyState = null!;
    private Label _emptyStateTitle = null!;
    private Label _emptyStateDetail = null!;
    private Label _scanSummaryLabel = null!;
    private StatusStrip _statusStrip = null!;
    private Button _startButton = null!;
    private Button _stopButton = null!;
    private CancellationTokenSource? _scanCancellation;
    private CancellationTokenSource? _exportCancellation;
    private string? _sortProperty = nameof(ScanResult.Address);
    private bool _sortAscending = true;
    private int _completed;
    private int _total;

    public MainForm() : this(null) { }

    internal MainForm(string? statePath)
    {
        _statePath = statePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PingFlud", "settings.json");
        _state = LoadState();
        _theme = ThemeCatalog.Get(_state.ThemeName);
        Text = _state.Title;
        MinimumSize = new Size(1180, 720);
        Size = new Size(1420, 900);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI Variable", 9.5F);
        AutoScaleMode = AutoScaleMode.Dpi;

        BuildUi();
        ApplyBrand();
        RefreshHistory();
        ApplyFilter();
        ApplyTheme();
        FormClosing += (_, _) =>
        {
            _scanCancellation?.Cancel();
            _exportCancellation?.Cancel();
            SaveState();
        };
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        // Windows 11: rounded corners + Mica backdrop + dark title bar.
        DwmInterop.ApplyWindowStyling(this, _theme.IsDark);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _toolTip.Dispose();
            _scanCancellation?.Dispose();
            _exportCancellation?.Dispose();
            _scanCancellation = null;
            _exportCancellation = null;
        }
        base.Dispose(disposing);
    }

    private void BuildUi()
    {
        _menu = BuildMenu();
        _root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(16, 0, 16, 0)
        };
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 156));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

        _header = BuildHeader();
        _scanCard = BuildScanCard();
        _resultsToolbar = BuildResultsToolbar();
        ConfigureGrid();
        _resultsCard = CreateCard();
        _resultsCard.Margin = new Padding(0, 0, 0, 8);
        _resultsCard.Padding = new Padding(10);
        _resultsCard.Controls.Add(_grid);
        _emptyState = BuildEmptyState();
        _resultsCard.Controls.Add(_emptyState);
        _statusStrip = new StatusStrip { Dock = DockStyle.Fill, SizingGrip = false };
        _statusStrip.Items.AddRange([_status, new ToolStripStatusLabel { Spring = true }, _summary, _progress]);

        _root.Controls.Add(_header, 0, 0);
        _root.Controls.Add(_scanCard, 0, 1);
        _root.Controls.Add(_resultsToolbar, 0, 2);
        _root.Controls.Add(_resultsCard, 0, 3);
        _root.Controls.Add(_statusStrip, 0, 4);

        _shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = Padding.Empty
        };
        _shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 205));
        _shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _sidebar = BuildSidebar();
        _shell.Controls.Add(_sidebar, 0, 0);
        _shell.Controls.Add(_root, 1, 0);

        Controls.Add(_shell);
        Controls.Add(_menu);
        MainMenuStrip = _menu;
    }

    private MenuStrip BuildMenu()
    {
        var menu = new MenuStrip { Dock = DockStyle.Top };
        var file = new ToolStripMenuItem("File");
        file.DropDownItems.Add("Import targets…", null, (_, _) => ImportTargets());
        var export = new ToolStripMenuItem("Export report");
        foreach (var format in new[] { "CSV", "XML", "HTML", "PDF", "PNG image", "TXT", "XLS-compatible HTML" })
            export.DropDownItems.Add(format, null, (_, _) => Export(format));
        file.DropDownItems.Add(export);
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add("Exit", null, (_, _) => Close());

        var edit = new ToolStripMenuItem("Edit");
        edit.DropDownItems.Add("Copy selected rows", null, (_, _) => CopySelected());
        edit.DropDownItems.Add("Clear results", null, (_, _) => ClearResults());
        edit.DropDownItems.Add(new ToolStripSeparator());
        edit.DropDownItems.Add("Scan settings…", null, (_, _) => ShowSettings());

        var view = new ToolStripMenuItem("Appearance");
        var appearanceToggle = new ToolStripMenuItem("Dark mode")
        {
            CheckOnClick = true,
            Checked = _theme.IsDark,
            ToolTipText = "Switch between Light and Dark modes"
        };
        appearanceToggle.Click += (_, _) => SelectTheme(appearanceToggle.Checked ? "Graphite" : "Daylight");
        _appearanceToggle = appearanceToggle;
        view.DropDownItems.Add(appearanceToggle);

        var help = new ToolStripMenuItem("Help");
        help.DropDownItems.Add("Target syntax and legend", null, (_, _) => ShowDocumentation());
        help.DropDownItems.Add("About", null, (_, _) => MessageBox.Show(this,
            $"Ping Flud\nVersion {DisplayVersion}\nDeveloper: OffByOneHuman\n\nAn MIT-licensed independent network reachability tool.\nUse only on networks you own or are authorized to test.",
            "About Ping Flud", MessageBoxButtons.OK, MessageBoxIcon.Information));

        menu.Items.AddRange([file, edit, view, help]);
        return menu;
    }

    private Panel BuildSidebar()
    {
        var sidebar = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 16, 14, 14) };
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = false,
            BackColor = Color.Transparent
        };

        var brand = TrackLabel("◈  PING FLUD", new Font("Segoe UI Semibold Variable", 13F));
        brand.Margin = new Padding(4, 2, 0, 24);
        flow.Controls.Add(brand);
        AddSidebarSection(flow, "WORKSPACE");
        flow.Controls.Add(CreateSidebarButton("▣   Scan workspace", "primary", (_, _) => _targets.Focus()));
        AddSidebarSection(flow, "GENERAL");
        flow.Controls.Add(CreateSidebarButton("⚙   Scan settings", "nav", (_, _) => ShowSettings()));
        flow.Controls.Add(CreateSidebarButton("?   Documentation", "nav", (_, _) => ShowDocumentation()));
        AddSidebarSection(flow, "SUPPORT");
        flow.Controls.Add(CreateSidebarButton("ⓘ   About", "nav", (_, _) => MessageBox.Show(this,
            $"Ping Flud {DisplayVersion}\nDeveloper: OffByOneHuman\n\nOpen-source network reachability scanner.",
            "About Ping Flud", MessageBoxButtons.OK, MessageBoxIcon.Information)));

        var authorization = TrackLabel("AUTHORIZED NETWORKS ONLY", new Font("Segoe UI Semibold Variable", 7.5F), true);
        authorization.Margin = new Padding(4, 28, 0, 0);
        flow.Controls.Add(authorization);
        sidebar.Controls.Add(flow);
        return sidebar;
    }

    private void AddSidebarSection(FlowLayoutPanel flow, string text)
    {
        var label = TrackLabel(text, new Font("Segoe UI Semibold Variable", 7.8F), true);
        label.Margin = new Padding(4, 12, 0, 6);
        flow.Controls.Add(label);
    }

    private Button CreateSidebarButton(string text, string role, EventHandler click)
    {
        var button = CreateButton(text, role, click);
        button.AutoSize = false;
        button.Size = new Size(170, 36);
        button.TextAlign = ContentAlignment.MiddleLeft;
        button.Margin = new Padding(0, 0, 0, 6);
        return button;
    }

    private Panel BuildHeader()
    {
        var header = new Panel { Dock = DockStyle.Fill, Margin = new Padding(-16, 0, -16, 10), Padding = new Padding(30, 14, 30, 10) };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));

        var branding = new Panel { Dock = DockStyle.Fill };
        _titleLabel.Font = new Font("Segoe UI Semibold Variable", 24F);
        _titleLabel.Location = new Point(0, 0);
        _subtitleLabel.Font = new Font("Segoe UI Variable", 9.5F);
        _subtitleLabel.Location = new Point(2, 42);
        branding.Controls.AddRange([_titleLabel, _subtitleLabel]);

        // Application.ProductVersion can include a long source-control suffix.
        // Product metadata belongs in About; the header must remain a readable
        // one-line identity at every supported window size.
        var buildLabel = TrackLabel($"Version {DisplayVersion}  •  OffByOneHuman", new Font("Segoe UI Variable", 8.5F), true);
        buildLabel.AutoSize = true;
        buildLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        buildLabel.Margin = new Padding(0, 27, 0, 0);
        buildLabel.TextAlign = ContentAlignment.MiddleRight;

        layout.Controls.Add(branding, 0, 0);
        layout.Controls.Add(buildLabel, 1, 0);
        header.Controls.Add(layout);
        return header;
    }

    private CardPanel BuildScanCard()
    {
        var card = CreateCard();
        card.Margin = new Padding(0, 0, 0, 12);
        card.Padding = new Padding(18, 12, 18, 12);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
        var scanHeading = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent };
        scanHeading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        scanHeading.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var scanTitle = TrackLabel("New scan", new Font("Segoe UI Semibold Variable", 12F));
        scanTitle.Anchor = AnchorStyles.Left;
        var importHint = TrackLabel("Test hosts, IPs, ranges, or CIDR blocks", new Font("Segoe UI Variable", 8.5F), true);
        scanHeading.Controls.Add(scanTitle, 0, 0);
        scanHeading.Controls.Add(importHint, 1, 0);
        layout.Controls.Add(scanHeading, 0, 0);

        var primary = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1 };
        primary.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76));
        primary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        primary.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        primary.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        primary.Controls.Add(TrackLabel("Targets", new Font("Segoe UI Semibold Variable", 9F), true), 0, 0);
        _targets.Dock = DockStyle.Fill;
        _targets.Margin = new Padding(0, 4, 10, 5);
        _targets.AccessibleDescription = "Enter a host, IP address, inclusive range, CIDR block, or IPv4 wildcard.";
        _inputs.Add(_targets);
        _targets.KeyDown += (_, e) =>
        {
            if (!IsStartKey(e.KeyCode)) return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            if (_scanCancellation is null) StartScan(_targets, EventArgs.Empty);
        };
        primary.Controls.Add(_targets, 1, 0);
        _startButton = CreateButton("▶  Start scan", "primary", StartScan);
        _stopButton = CreateButton("■  Stop", "danger", (_, _) => _scanCancellation?.Cancel());
        _stopButton.Enabled = false;
        primary.Controls.Add(_startButton, 2, 0);
        primary.Controls.Add(_stopButton, 3, 0);

        var secondary = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent
        };
        var importButton = CreateButton("Import list…", "secondary", (_, _) => ImportTargets());
        _toolTip.SetToolTip(importButton,
            "Import a .txt or .csv file. Put one address, host, range, CIDR, or wildcard specification on each line. Lines beginning with # are ignored.");
        secondary.Controls.Add(importButton);
        secondary.Controls.Add(CreateButton("Scan settings", "secondary", (_, _) => ShowSettings()));
        secondary.Controls.Add(CreateButton("Syntax help", "secondary", (_, _) => ShowDocumentation()));

        layout.Controls.Add(primary, 0, 1);
        layout.Controls.Add(secondary, 0, 2);
        card.Controls.Add(layout);
        return card;
    }

    private Panel BuildResultsToolbar()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(2, 10, 2, 6) };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 8, RowCount = 1, BackColor = Color.Transparent };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 146));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
        for (var i = 4; i < 8; i++) layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var title = TrackLabel("Scan results", new Font("Segoe UI Semibold Variable", 12F));
        title.Margin = new Padding(0, 7, 16, 0);
        _scanSummaryLabel = TrackLabel(string.Empty, new Font("Segoe UI Variable", 8.5F), true);
        _scanSummaryLabel.Margin = new Padding(0, 9, 12, 0);
        layout.Controls.Add(title, 0, 0);
        layout.Controls.Add(_scanSummaryLabel, 1, 0);

        _filter.Items.AddRange(["All results", "Responding", "Not responding"]);
        _filter.SelectedIndex = 0;
        _filter.Margin = new Padding(0, 5, 10, 5);
        _filter.SelectedIndexChanged += (_, _) => ApplyFilter();
        _inputs.Add(_filter);
        layout.Controls.Add(_filter, 2, 0);
        _search.Dock = DockStyle.Fill;
        _search.Margin = new Padding(0, 5, 10, 5);
        _search.TextChanged += (_, _) => ApplyFilter();
        _inputs.Add(_search);
        layout.Controls.Add(_search, 3, 0);
        var copyButton = CreateButton("Copy", "secondary", (_, _) => CopySelected());
        var clearButton = CreateButton("Clear", "secondary", (_, _) => ClearResults());
        var exportButton = CreateButton("Export CSV", "primary", (_, _) => Export("CSV"));
        _resultActions.AddRange([copyButton, clearButton, exportButton]);
        layout.Controls.Add(copyButton, 4, 0);
        layout.Controls.Add(clearButton, 5, 0);
        layout.Controls.Add(exportButton, 6, 0);
        var moreFormats = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            AccessibleName = "More export formats",
            Dock = DockStyle.Fill,
            Margin = new Padding(4, 5, 0, 5)
        };
        moreFormats.Items.AddRange(["More formats…", "XML", "HTML", "PDF", "PNG image", "TXT", "XLS-compatible HTML"]);
        moreFormats.SelectedIndex = 0;
        moreFormats.SelectedIndexChanged += (_, _) =>
        {
            if (moreFormats.SelectedIndex <= 0) return;
            var format = moreFormats.SelectedItem!.ToString()!;
            BeginInvoke(() => moreFormats.SelectedIndex = 0);
            Export(format);
        };
        _inputs.Add(moreFormats);
        _resultActions.Add(moreFormats);
        layout.Controls.Add(moreFormats, 7, 0);
        panel.Controls.Add(layout);
        return panel;
    }

    private Panel BuildEmptyState()
    {
        var panel = new Panel { Dock = DockStyle.Fill, AccessibleName = "Empty scan results" };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        _emptyStateTitle = TrackLabel("No reachability results yet", new Font("Segoe UI Semibold Variable", 14F));
        _emptyStateTitle.Anchor = AnchorStyles.None;
        _emptyStateDetail = TrackLabel("Enter targets above to test latency, packet loss, TTL, and reverse DNS.", new Font("Segoe UI Variable", 9.5F), true);
        _emptyStateDetail.Anchor = AnchorStyles.None;
        _emptyStateDetail.Margin = new Padding(0, 8, 0, 0);
        layout.Controls.Add(_emptyStateTitle, 0, 1);
        layout.Controls.Add(_emptyStateDetail, 0, 2);
        panel.Controls.Add(layout);
        return panel;
    }

    private void ConfigureGrid()
    {
        void AddTextColumn(string name, string header, string property, float weight) => _grid.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                Name = name, HeaderText = header, DataPropertyName = property, FillWeight = weight,
                SortMode = DataGridViewColumnSortMode.Programmatic
            });

        AddTextColumn("State", "STATE", nameof(ScanResult.Status), 72);
        AddTextColumn("Target", "TARGET", nameof(ScanResult.Target), 120);
        AddTextColumn("Up", "UP", nameof(ScanResult.Responding), 36);
        AddTextColumn("Latency", "LATENCY", nameof(ScanResult.RoundtripMs), 58);
        AddTextColumn("Loss", "LOSS %", nameof(ScanResult.PacketLossPercent), 48);
        AddTextColumn("Replies", "REPLIES", nameof(ScanResult.Successes), 46);
        AddTextColumn("TTL", "TTL", nameof(ScanResult.ReplyTtl), 38);
        AddTextColumn("Address", "IP ADDRESS", nameof(ScanResult.Address), 105);
        AddTextColumn("HostName", "REVERSE DNS", nameof(ScanResult.HostName), 140);

        _source.DataSource = _allResults;
        _grid.DataSource = _source;
        _grid.Columns["Address"].HeaderCell.SortGlyphDirection = SortOrder.Ascending;
        _grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex >= 0 && _grid.Rows[e.RowIndex].DataBoundItem is ScanResult result)
            {
                if (e.ColumnIndex == 0)
                {
                    e.Value = result.Responding ? "● Responding" : "○ " + result.Status;
                    e.CellStyle!.ForeColor = result.Responding ? _theme.Success : _theme.Danger;
                }
                else if (e.ColumnIndex == _grid.Columns["Up"].Index)
                {
                    e.Value = result.Responding ? "True" : "False";
                    e.CellStyle!.ForeColor = result.Responding ? _theme.Success : _theme.Danger;
                }
            }
        };
        _grid.ColumnHeaderMouseClick += (_, e) =>
        {
            var property = _grid.Columns[e.ColumnIndex].DataPropertyName;
            if (string.IsNullOrEmpty(property)) return;
            if (_sortProperty == property) _sortAscending = !_sortAscending;
            else { _sortProperty = property; _sortAscending = true; }
            foreach (DataGridViewColumn column in _grid.Columns)
                column.HeaderCell.SortGlyphDirection = SortOrder.None;
            _grid.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection =
                _sortAscending ? SortOrder.Ascending : SortOrder.Descending;
            ApplyFilter();
        };
    }

    internal static bool IsStartKey(Keys key) => key == Keys.Enter;

    private async void StartScan(object? sender, EventArgs e)
    {
        if (_scanCancellation is not null) return;
        var cancellation = new CancellationTokenSource();
        _scanCancellation = cancellation;
        SetScanningState(true);
        try
        {
            _status.Text = "Expanding targets…";
            var input = _targets.Text;
            var cap = _state.Settings.ExpansionCap;
            var expanded = await Task.Run(() => TargetParser.Expand(input, cap, cancellation.Token), cancellation.Token);
            if (expanded.Count == 0) return;

            Remember(input);
            _allResults.Clear();
            _scanSummaryLabel.Text = string.Empty;
            ApplyFilter();
            _total = expanded.Count;
            _completed = 0;
            _progress.Value = 0;
            _status.Text = "Scanning…";
            _summary.Text = $"0 / {_total}";
            _source.RaiseListChangedEvents = false;
            try
            {
                var indexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var responding = 0;
                var nextFilterRefresh = DateTime.MinValue;
                var report = new Progress<ScanResult>(result =>
                {
                    var key = result.Target + "\0" + result.Address;
                    if (indexes.TryGetValue(key, out var index)) _allResults[index] = result;
                    else
                    {
                        indexes[key] = _allResults.Count;
                        _allResults.Add(result);
                        _completed++;
                        if (result.Responding) responding++;
                    }
                    _progress.Value = Math.Min(100, _completed * 100 / _total);
                    _summary.Text = $"{_completed:N0} / {_total:N0}   •   {responding:N0} responding";
                    if (DateTime.UtcNow >= nextFilterRefresh)
                    {
                        // Rebuild the filtered/sorted BindingList at an adaptive cadence instead
                        // of on every result, to keep the UI responsive for large scans.
                        ApplyFilter();
                        nextFilterRefresh = DateTime.UtcNow.AddMilliseconds(250);
                    }
                });
                await new PingScanner().ScanAsync(expanded, _state.Settings, report, cancellation.Token);
            }
            finally
            {
                _source.RaiseListChangedEvents = true;
                _source.ResetBindings(false);
            }
            ApplyFilter();
            _status.Text = "Scan complete";
            UpdateResultsPresentation(completed: true);
        }
        catch (OperationCanceledException) { _status.Text = "Scan stopped"; }
        catch (Exception ex)
        {
            _status.Text = "Cannot start scan";
            MessageBox.Show(this, ex.Message, "Cannot start scan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            cancellation.Dispose();
            if (ReferenceEquals(_scanCancellation, cancellation)) _scanCancellation = null;
            SetScanningState(false);
        }
    }

    private void ApplyFilter()
    {
        var selectedFilter = _filter.SelectedIndex switch
        {
            1 => ResultFilter.Responding,
            2 => ResultFilter.NotResponding,
            _ => ResultFilter.All
        };

        IEnumerable<ScanResult> rows = ResultFilters.Apply(_allResults, selectedFilter, _search.Text);
        rows = _sortProperty switch
        {
            nameof(ScanResult.Status) => Sort(rows, row => row.Status),
            nameof(ScanResult.Target) => Sort(rows, row => row.Target, NetworkAddressComparer.Instance),
            nameof(ScanResult.Responding) => Sort(rows, row => row.Responding),
            nameof(ScanResult.RoundtripMs) => Sort(rows, row => row.RoundtripMs),
            nameof(ScanResult.PacketLossPercent) => Sort(rows, row => row.PacketLossPercent),
            nameof(ScanResult.Successes) => Sort(rows, row => row.Successes),
            nameof(ScanResult.ReplyTtl) => Sort(rows, row => row.ReplyTtl),
            nameof(ScanResult.Address) => Sort(rows, row => row.Address, NetworkAddressComparer.Instance),
            nameof(ScanResult.HostName) => Sort(rows, row => row.HostName),
            _ => rows
        };

        // Reuse the same BindingList when possible to avoid full grid rebuilds.
        if (_source.DataSource is BindingList<ScanResult> existing && existing.Count > 0)
        {
            existing.Clear();
            foreach (var row in rows) existing.Add(row);
        }
        else
        {
            _source.DataSource = new BindingList<ScanResult>(rows.ToList());
        }

        // Update summary with match count.
        var matchCount = ((BindingList<ScanResult>)_source.DataSource!).Count;
        var totalCount = _allResults.Count;
        _summary.Text = _search.Text.Length > 0
            ? $"{matchCount:N0} of {totalCount:N0} shown"
            : $"{totalCount:N0} targets";
        UpdateResultsPresentation();
    }

    private void UpdateResultsPresentation(bool completed = false)
    {
        var visibleCount = _source.List.Cast<object>().OfType<ScanResult>().Count();
        var hasResults = _allResults.Count > 0;
        _emptyState.Visible = visibleCount == 0;
        _grid.Visible = visibleCount > 0;
        _emptyStateTitle.Text = hasResults ? "No matching results" : "No reachability results yet";
        _emptyStateDetail.Text = hasResults
            ? "Adjust the filter or search text to show matching scan results."
            : "Enter targets above to test latency, packet loss, TTL, and reverse DNS.";
        foreach (var action in _resultActions) action.Enabled = hasResults;
        ApplyButtonStyles();

        if (!completed) return;
        var responding = _allResults.Where(result => result.Responding).ToList();
        var unavailable = _allResults.Count - responding.Count;
        var latencySamples = responding.Where(result => result.RoundtripMs.HasValue)
            .Select(result => result.RoundtripMs!.Value).Order().ToList();
        long? median = latencySamples.Count == 0 ? null : latencySamples[latencySamples.Count / 2];
        _scanSummaryLabel.Text = $"Completed · {_allResults.Count:N0} targets · {responding.Count:N0} responding · {unavailable:N0} unavailable" +
            (median.HasValue ? $" · median {median.Value:N0} ms" : string.Empty);
    }

    private IEnumerable<ScanResult> Sort<TKey>(IEnumerable<ScanResult> rows, Func<ScanResult, TKey> key,
        IComparer<TKey>? comparer = null) => _sortAscending
        ? rows.OrderBy(key, comparer)
        : rows.OrderByDescending(key, comparer);

    private void ImportTargets()
    {
        using var dialog = new OpenFileDialog { Filter = "Target lists|*.txt;*.csv|All files|*.*" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var info = new FileInfo(dialog.FileName);
            if (!info.Exists) throw new FileNotFoundException("The selected target list no longer exists.", dialog.FileName);
            if (info.Length > 5 * 1024 * 1024)
            {
                MessageBox.Show(this, "Target files are limited to 5 MB.", "File too large", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _targets.Text = string.Join(", ", File.ReadLines(dialog.FileName)
                .Select(line => line.Trim()).Where(line => line.Length > 0 && !line.StartsWith('#')));
            _status.Text = $"Imported targets from {info.Name}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            MessageBox.Show(this, ex.Message, "Import failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Remember(string value)
    {
        _state.History.RemoveAll(item => item.Equals(value, StringComparison.OrdinalIgnoreCase));
        _state.History.Insert(0, value);
        if (_state.History.Count > 20) _state.History.RemoveRange(20, _state.History.Count - 20);
        RefreshHistory();
        SaveState();
    }

    private void RefreshHistory()
    {
        var text = _targets.Text;
        _targets.Items.Clear();
        _targets.Items.AddRange(_state.History.Cast<object>().ToArray());
        _targets.Text = text;
    }

    private void ClearResults()
    {
        if (_scanCancellation is not null)
        {
            MessageBox.Show(this, "Stop the active scan before clearing results.", "Scan in progress",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        _allResults.Clear();
        ApplyFilter();
        _summary.Text = "0 targets";
        _progress.Value = 0;
        _status.Text = "Ready";
        _scanSummaryLabel.Text = string.Empty;
        UpdateResultsPresentation();
    }

    private void CopySelected()
    {
        var rows = _grid.SelectedRows.Cast<DataGridViewRow>().Select(row => row.DataBoundItem)
            .OfType<ScanResult>().ToList();
        if (rows.Count == 0) return;
        Clipboard.SetText(string.Join(Environment.NewLine,
            rows.Select(row => $"{row.Target}\t{row.Address}\t{row.HostName}\t{row.Status}\t{row.RoundtripMs}")));
        _status.Text = $"Copied {rows.Count:N0} row(s)";
    }

    private void ShowSettings()
    {
        using var dialog = new SettingsDialog(_state, _theme);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _theme = ThemeCatalog.Get(_state.ThemeName);
        ApplyTheme();
        ApplyBrand();
        SaveState();
    }

    private void ShowDocumentation() => MessageBox.Show(this, DocumentationText, "Ping Flud target syntax",
        MessageBoxButtons.OK, MessageBoxIcon.Information);

    internal void SelectTheme(string name)
    {
        _state.ThemeName = ThemeCatalog.Get(name).Name;
        _theme = ThemeCatalog.Get(_state.ThemeName);
        ApplyTheme();
        SaveState();
    }

    private void ApplyBrand()
    {
        _titleLabel.Text = _state.Title;
        _subtitleLabel.Text = _state.Subtitle;
        Text = _state.Title;
    }

    private AppState LoadState()
    {
        try
        {
            if (!File.Exists(_statePath)) return new AppState();
            if (new FileInfo(_statePath).Length > 1024 * 1024) return new AppState();
            var loaded = JsonSerializer.Deserialize<AppState>(File.ReadAllText(_statePath)) ?? new AppState();
            loaded.Settings.Validate();
            loaded.ThemeName = ThemeCatalog.Get(loaded.ThemeName).Name;
            loaded.Title = NormalizeLabel(loaded.Title, "Ping Flud", 120);
            loaded.Subtitle = NormalizeLabel(loaded.Subtitle, string.Empty, 240);
            if (loaded.Subtitle.Equals("Fast, transparent network reachability checks", StringComparison.OrdinalIgnoreCase))
                loaded.Subtitle = PingFlud.Application.AppState.DefaultSubtitle;
            loaded.History = loaded.History.Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim()).Where(item => item.Length <= 16_384)
                .Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToList();
            return loaded;
        }
        catch { return new AppState(); }
    }

    private void SaveState()
    {
        string? tempPath = null;
        try
        {
            var directory = Path.GetDirectoryName(_statePath)!;
            Directory.CreateDirectory(directory);
            tempPath = Path.Combine(directory, $".{Path.GetFileName(_statePath)}.{Guid.NewGuid():N}.tmp");
            File.WriteAllText(tempPath, JsonSerializer.Serialize(_state, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(tempPath, _statePath, overwrite: true);
        }
        catch { /* The application remains usable if settings cannot be persisted. */ }
        finally
        {
            try
            {
                if (tempPath is not null && File.Exists(tempPath)) File.Delete(tempPath);
            }
            catch { /* A stale temp file is harmless and can be removed later. */ }
        }
    }

    private static string NormalizeLabel(string? value, string fallback, int maximumLength)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return normalized.Length <= maximumLength ? normalized : normalized[..maximumLength];
    }

    private static string GetDisplayVersion()
    {
        var version = typeof(MainForm).Assembly.GetName().Version;
        return version is null ? "unknown" : $"{version.Major}.{version.Minor}.{version.Build}";
    }

    private List<ScanResult> CurrentRows() => _source.List.Cast<object>().OfType<ScanResult>().ToList();

    private async void Export(string kind)
    {
        var rows = CurrentRows();
        if (rows.Count == 0)
        {
            MessageBox.Show(this, "There are no visible results to export.");
            return;
        }
        var extension = kind switch
        {
            "CSV" => "csv", "XML" => "xml", "HTML" => "html", "PDF" => "pdf",
            "PNG image" => "png", "TXT" => "txt", "XLS-compatible HTML" => "xls", _ => "dat"
        };
        using var dialog = new SaveFileDialog
        {
            Filter = $"{kind}|*.{extension}", FileName = $"ping-flud-{DateTime.Now:yyyyMMdd-HHmmss}.{extension}"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        // Render to a staging directory beside the destination, then atomically
        // publish the completed file(s). This prevents readers from observing a
        // partially written report while export data is still being generated.
        var fileName = dialog.FileName;
        if (_exportCancellation is not null) return;
        var cancellation = new CancellationTokenSource();
        _exportCancellation = cancellation;
        var token = cancellation.Token;
        try
        {
            _status.Text = "Exporting…";
            var destinationDirectory = Path.GetDirectoryName(fileName)!;
            var stagingDirectory = Path.Combine(destinationDirectory, $".pingflud-export-{Guid.NewGuid():N}");
            Directory.CreateDirectory(stagingDirectory);
            try
            {
                var stagedPath = Path.Combine(stagingDirectory, Path.GetFileName(fileName));
                await Task.Run(() => WriteExport(kind, stagedPath, rows), token);
                PublishExportFiles(stagingDirectory, fileName);
                _status.Text = $"Exported {rows.Count:N0} result(s)";
            }
            finally
            {
                if (Directory.Exists(stagingDirectory)) Directory.Delete(stagingDirectory, recursive: true);
            }
        }
        catch (OperationCanceledException) { _status.Text = "Export cancelled"; }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Export failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _status.Text = "Export failed";
        }
        finally
        {
            cancellation.Dispose();
            if (ReferenceEquals(_exportCancellation, cancellation)) _exportCancellation = null;
        }
    }

    private static void PublishExportFiles(string stagingDirectory, string destinationPath)
    {
        var destinationDirectory = Path.GetDirectoryName(destinationPath)!;
        var stagedFiles = Directory.EnumerateFiles(stagingDirectory).ToArray();
        var stagedNames = stagedFiles
            .Select(Path.GetFileName)
            .OfType<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Move every staged file into the destination directory. If any single
        // move fails the caller's finally block will clean the staging directory,
        // and previously moved files remain valid because each destination file
        // is overwritten in place only after the corresponding source fully exists.
        foreach (var stagedFile in stagedFiles)
        {
            var destination = Path.Combine(destinationDirectory, Path.GetFileName(stagedFile));
            File.Move(stagedFile, destination, overwrite: true);
        }

        // For multi-page PNG reports, remove continuation pages from prior exports
        // that are no longer part of the report set so stale data cannot persist.
        if (!string.Equals(Path.GetExtension(destinationPath), ".png", StringComparison.OrdinalIgnoreCase)) return;

        var stem = Path.GetFileNameWithoutExtension(destinationPath);
        foreach (var previousPage in Directory.EnumerateFiles(destinationDirectory, $"{stem}-*.png"))
        {
            var fileName = Path.GetFileName(previousPage);
            if (stagedNames.Contains(fileName)) continue;
            var pageStem = Path.GetFileNameWithoutExtension(previousPage);
            var suffix = pageStem.Length > stem.Length + 1 ? pageStem[(stem.Length + 1)..] : string.Empty;
            if (suffix.Length == 3 && int.TryParse(suffix, out var pageNumber) && pageNumber >= 2)
                File.Delete(previousPage);
        }
    }

    private void WriteExport(string kind, string path, List<ScanResult> rows)
    {
        switch (kind)
        {
            case "CSV": File.WriteAllText(path, CsvReport.Create(rows), new UTF8Encoding(true)); break;
            case "XML":
                new XDocument(new XElement("PingFludResults", rows.Select(row => new XElement("Result",
                    new XAttribute("target", row.Target), new XElement("Responding", row.Responding),
                    new XElement("LatencyMs", row.RoundtripMs), new XElement("PacketLossPercent", row.PacketLossPercent),
                    new XElement("Address", row.Address), new XElement("HostName", row.HostName),
                    new XElement("Status", row.Status))))).Save(path);
                break;
            case "HTML": File.WriteAllText(path, Html(rows, false), Encoding.UTF8); break;
            case "XLS-compatible HTML": File.WriteAllText(path, Html(rows, true), Encoding.UTF8); break;
            case "TXT": File.WriteAllLines(path,
                rows.Select(row => $"{row.Target}\t{row.Address}\t{row.HostName}\t{row.Status}\t{row.RoundtripMs}")); break;
            case "PDF": SimplePdf.Write(path, rows); break;
            case "PNG image": ExportImages(path, rows); break;
        }
    }

    private string Html(IEnumerable<ScanResult> rows, bool spreadsheet)
    {
        string Encode(string value)
        {
            if (spreadsheet && value.Length > 0 && value[0] is '=' or '+' or '-' or '@') value = "'" + value;
            return WebUtility.HtmlEncode(value);
        }
        string Hex(Color color) => ColorTranslator.ToHtml(color);
        return $"<!doctype html><meta charset=utf-8><title>Ping Flud Results</title>" +
               $"<style>body{{font:14px Segoe UI;background:{Hex(_theme.WindowBackground)};color:{Hex(_theme.Foreground)}}}" +
               $"table{{border-collapse:collapse;width:100%}}th,td{{padding:8px;border:1px solid {Hex(_theme.Border)}}}" +
               $"th{{background:{Hex(_theme.SurfaceRaised)}}}tr:nth-child(even){{background:{Hex(_theme.GridAlternate)}}}" +
               $".up{{color:{Hex(_theme.Success)}}}.down{{color:{Hex(_theme.Danger)}}}</style>" +
               "<h1>Ping Flud Results</h1><table><tr><th>Target</th><th>Status</th><th>Latency</th><th>Loss %</th>" +
               "<th>Replies</th><th>TTL</th><th>Address</th><th>Reverse DNS</th></tr>" +
               string.Join("", rows.Select(row => $"<tr><td>{Encode(row.Target)}</td><td class='{(row.Responding ? "up" : "down")}'>{Encode(row.Status)}</td>" +
                                                   $"<td>{row.RoundtripMs}</td><td>{row.PacketLossPercent:0.##}</td><td>{row.Successes}/{row.Attempts}</td>" +
                                                   $"<td>{row.ReplyTtl}</td><td>{Encode(row.Address)}</td><td>{Encode(row.HostName)}</td></tr>")) +
               "</table>";
    }

    private void ExportImages(string path, IReadOnlyList<ScanResult> rows)
    {
        // Cap rows per page so each bitmap stays under ~64 MiB (1600x2600 at 32bpp).
        // 100 rows × 24px = 2400px height + 34px header = 2434px → ~15.6 MiB per page.
        const int rowsPerImage = 100, width = 1600, rowHeight = 24, headerHeight = 34;
        var pageCount = (rows.Count + rowsPerImage - 1) / rowsPerImage;
        var directory = Path.GetDirectoryName(path)!;
        var stem = Path.GetFileNameWithoutExtension(path);
        for (var page = 0; page < pageCount; page++)
        {
            var chunk = rows.Skip(page * rowsPerImage).Take(rowsPerImage).ToList();
            var output = page == 0 ? path : Path.Combine(directory, $"{stem}-{page + 1:000}.png");
            using var bitmap = new Bitmap(width, headerHeight + chunk.Count * rowHeight);
            using var graphics = Graphics.FromImage(bitmap);
            using var font = new Font("Segoe UI Variable", 9);
            using var bold = new Font(font, FontStyle.Bold);
            using var gridPen = new Pen(_theme.Border);
            using var textBrush = new SolidBrush(_theme.Foreground);
            using var headerBrush = new SolidBrush(_theme.SurfaceRaised);
            using var alternateBrush = new SolidBrush(_theme.GridAlternate);
            graphics.Clear(_theme.WindowBackground);
            var columns = new[]
            {
                (0,220,"Target"),(220,210,"Status"),(430,90,"Latency"),(520,90,"Loss %"),
                (610,90,"Replies"),(700,70,"TTL"),(770,260,"IP address"),(1030,570,"Reverse DNS")
            };
            graphics.FillRectangle(headerBrush, 0, 0, width, headerHeight);
            foreach (var (x, columnWidth, label) in columns)
            {
                graphics.DrawString(label, bold, textBrush, new RectangleF(x + 5, 8, columnWidth - 10, 20));
                graphics.DrawLine(gridPen, x, 0, x, bitmap.Height);
            }
            graphics.DrawLine(gridPen, 0, headerHeight, width, headerHeight);
            for (var i = 0; i < chunk.Count; i++)
            {
                var row = chunk[i];
                var y = headerHeight + i * rowHeight;
                if (i % 2 == 1) graphics.FillRectangle(alternateBrush, 0, y, width, rowHeight);
                var values = new[]
                {
                    row.Target,row.Status,row.RoundtripMs?.ToString() ?? "",row.PacketLossPercent.ToString("0.##"),
                    $"{row.Successes}/{row.Attempts}",row.ReplyTtl?.ToString() ?? "",row.Address,row.HostName
                };
                for (var column = 0; column < columns.Length; column++)
                {
                    var (x, columnWidth, _) = columns[column];
                    graphics.DrawString(values[column], font, textBrush,
                        new RectangleF(x + 5, y + 4, columnWidth - 10, rowHeight - 4));
                }
                graphics.DrawLine(gridPen, 0, y + rowHeight, width, y + rowHeight);
            }
            bitmap.Save(output, ImageFormat.Png);
        }
    }

    private CardPanel CreateCard()
    {
        var card = new CardPanel { Dock = DockStyle.Fill };
        _cards.Add(card);
        return card;
    }

    private Label TrackLabel(string text, Font font, bool muted = false)
    {
        var label = new Label { Text = text, Font = font, AutoSize = true, Tag = muted ? "muted" : "normal" };
        _labels.Add(label);
        return label;
    }

    private Button CreateButton(string text, string role, EventHandler click)
    {
        var button = new RoundedButton
        {
            Text = text,
            Tag = role,
            AutoSize = false,
            Height = 34,
            Padding = new Padding(10, 0, 10, 0),
            Margin = new Padding(4, 4, 0, 4),
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleCenter,
            IsPrimary = role == "primary"
        };
        button.Size = button.GetPreferredSize(Size.Empty);
        button.Click += click;
        _buttons.Add(button);

        if (role == "primary")
        {
            button.BackColor = _theme.Accent;
            button.ForeColor = _theme.AccentForeground;
        }
        else if (role == "danger")
        {
            button.BackColor = _theme.SurfaceRaised;
            button.ForeColor = _theme.Danger;
        }
        else
        {
            button.BackColor = _theme.SurfaceRaised;
            button.ForeColor = _theme.Foreground;
        }

        return button;
    }


    private void ApplyButtonStyles()
    {
        foreach (var button in _buttons)
        {
            if (!button.Enabled)
            {
                button.BackColor = _theme.SurfaceRaised;
                button.ForeColor = _theme.MutedForeground;
                continue;
            }
            var role = button.Tag?.ToString();
            button.BackColor = role switch { "primary" => _theme.Accent, "danger" => _theme.SurfaceRaised, _ => _theme.SurfaceRaised };
            button.ForeColor = role switch { "primary" => _theme.AccentForeground, "danger" => _theme.Danger, _ => _theme.Foreground };
        }
    }

    private void SetScanningState(bool scanning)
    {
        _startButton.Enabled = !scanning;
        _stopButton.Enabled = scanning;
        _targets.Enabled = !scanning;
        ApplyButtonStyles();
    }

    private void ApplyToolStripColors(ToolStripItemCollection items)
    {
        foreach (ToolStripItem item in items)
        {
            item.ForeColor = _theme.Foreground;
            item.BackColor = ReferenceEquals(item.Owner, _menu) ? _theme.Header : _theme.Surface;
            if (item is ToolStripDropDownItem dropDown) ApplyToolStripColors(dropDown.DropDownItems);
        }
    }

    private void ApplyTheme()
    {
        _theme = ThemeCatalog.Get(_state.ThemeName);
        Tag = _theme; // Expose theme to child controls (buttons need accent via FindForm().Tag)
        BackColor = _theme.WindowBackground;
        ForeColor = _theme.Foreground;

        // Re-apply DWM styling when the theme changes at runtime (dark/light title bar).
        if (IsHandleCreated)
            DwmInterop.SetDarkMode(this, _theme.IsDark);

        _shell.BackColor = _theme.WindowBackground;
        _root.BackColor = _theme.WindowBackground;
        _sidebar.BackColor = _theme.SurfaceRaised;
        _header.BackColor = _theme.Header;
        _resultsToolbar.BackColor = _theme.WindowBackground;
        _emptyState.BackColor = _theme.Surface;

        foreach (var card in _cards)
        {
            card.BackColor = _theme.Surface;
            card.BorderColor = _theme.Border;
            card.Invalidate();
        }
        foreach (var label in _labels)
            label.ForeColor = Equals(label.Tag, "muted") ? _theme.MutedForeground : _theme.Foreground;
        _titleLabel.ForeColor = _theme.Foreground;
        _subtitleLabel.ForeColor = _theme.MutedForeground;

        foreach (var input in _inputs)
        {
            input.BackColor = _theme.SurfaceRaised;
            input.ForeColor = _theme.Foreground;
        }
        ApplyButtonStyles();

        _menu.BackColor = _theme.Header;
        _menu.ForeColor = _theme.Foreground;
        _menu.Renderer = new ToolStripProfessionalRenderer(new ThemeColorTable(_theme));
        if (_appearanceToggle is not null)
        {
            _appearanceToggle.Checked = _theme.IsDark;
            _appearanceToggle.Text = _theme.IsDark ? "Dark mode" : "Light mode";
        }
        ApplyToolStripColors(_menu.Items);
        _statusStrip.BackColor = _theme.Header;
        _statusStrip.ForeColor = _theme.Foreground;
        _status.ForeColor = _theme.Foreground;
        _summary.ForeColor = _theme.MutedForeground;
        _progress.ForeColor = _theme.Success;

        _grid.BackgroundColor = _theme.Surface;
        _grid.GridColor = _theme.Border;
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersHeight = 36;
        _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = _theme.SurfaceRaised, ForeColor = _theme.Foreground,
            Font = new Font("Segoe UI Semibold Variable", 8.5F, FontStyle.Bold), SelectionBackColor = _theme.SurfaceRaised,
            SelectionForeColor = _theme.Foreground, Alignment = DataGridViewContentAlignment.MiddleCenter
        };
        _grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = _theme.Surface, ForeColor = _theme.Foreground,
            SelectionBackColor = _theme.Selection, SelectionForeColor = _theme.Foreground,
            Font = new Font("Segoe UI Variable", 9.5F, FontStyle.Bold),
            Padding = new Padding(3, 2, 3, 6), Alignment = DataGridViewContentAlignment.MiddleCenter
        };
        _grid.AlternatingRowsDefaultCellStyle.BackColor = _theme.GridAlternate;
        _grid.RowTemplate.Height = 32; // Windows 11 touch-friendly height
        _grid.RowTemplate.DefaultCellStyle.SelectionBackColor = _theme.Selection;
        _grid.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
        _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = true;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AutoGenerateColumns = false;
        _grid.BorderStyle = BorderStyle.None;
        _grid.RowHeadersVisible = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.EnableHeadersVisualStyles = false;
        _grid.Invalidate();
    }
}

internal sealed class ThemeColorTable(ThemePalette theme) : ProfessionalColorTable
{
    public override Color MenuStripGradientBegin => theme.Header;
    public override Color MenuStripGradientEnd => theme.Header;
    public override Color ToolStripDropDownBackground => theme.Surface;
    public override Color MenuItemSelected => theme.Selection;
    public override Color MenuItemSelectedGradientBegin => theme.Selection;
    public override Color MenuItemSelectedGradientEnd => theme.Selection;
    public override Color MenuItemPressedGradientBegin => theme.SurfaceRaised;
    public override Color MenuItemPressedGradientEnd => theme.SurfaceRaised;
    public override Color MenuBorder => theme.Border;
    public override Color MenuItemBorder => theme.Border;
    public override Color ImageMarginGradientBegin => theme.Surface;
    public override Color ImageMarginGradientMiddle => theme.Surface;
    public override Color ImageMarginGradientEnd => theme.Surface;
}
