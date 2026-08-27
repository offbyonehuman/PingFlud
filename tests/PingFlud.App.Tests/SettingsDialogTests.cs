using PingFlud.App;
using Xunit;

namespace PingFlud.App.Tests;

public sealed class SettingsDialogTests
{
    [Fact]
    public void EnterIsTheTargetStartShortcut()
    {
        Assert.True(MainForm.IsStartKey(Keys.Enter));
        Assert.False(MainForm.IsStartKey(Keys.Escape));
    }

    [Fact]
    public async Task EnterInTargetsStartsScan()
    {
        using var form = CreateMainForm();
        var controls = Descendants(form).ToList();
        var targets = controls.OfType<ComboBox>().Single(control => control.AccessibleName == "Targets");
        var start = controls.OfType<Button>().Single(button => button.Text.Contains("Start scan"));
        targets.Text = "127.0.0.1";
        var args = new KeyEventArgs(Keys.Enter);
        typeof(ComboBox).GetMethod("OnKeyDown", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(targets, new object[] { args });

        Assert.True(args.SuppressKeyPress);
        Assert.False(start.Enabled);
        for (var attempt = 0; attempt < 50 && !start.Enabled; attempt++) await Task.Delay(50);
        Assert.True(start.Enabled);
    }

    [Fact]
    public void ProvidesOnlySupportedThemes()
    {
        Assert.Equal("Graphite", new AppState().ThemeName);
        Assert.Equal(["Graphite", "Midnight", "Nebula", "Daylight"], ThemeCatalog.All.Select(theme => theme.Name));
        var graphite = ThemeCatalog.Get("Graphite");
        Assert.True(graphite.IsDark);
        Assert.True(graphite.WindowBackground.R < 24 && graphite.WindowBackground.G < 24 && graphite.WindowBackground.B < 24);
        Assert.True(graphite.Surface.R > graphite.WindowBackground.R);
        Assert.All(ThemeCatalog.All, theme => Assert.NotEqual(theme.WindowBackground, theme.Foreground));
    }

    [Fact]
    public void ThemeMenuSelectionAppliesPaletteImmediately()
    {
        using var form = CreateMainForm();
        Assert.DoesNotContain(Descendants(form).OfType<ComboBox>(), control => control.AccessibleName == "Theme");

        form.SelectTheme("Daylight");
        Assert.Equal(ThemeCatalog.Get("Daylight").WindowBackground, form.BackColor);

        form.SelectTheme("Midnight");
        Assert.Equal(ThemeCatalog.Get("Midnight").WindowBackground, form.BackColor);
    }

    [Fact]
    public void MainWindowUsesTaskLedEmptyStateAndResultsToolbar()
    {
        using var form = CreateMainForm();
        var controls = Descendants(form).ToList();

        var labels = controls.OfType<Label>().ToList();
        Assert.Contains(labels, label => label.Text == "No scan results yet");
        Assert.Contains(labels, label => label.Text.Contains("host, IP address, range, or CIDR"));
        Assert.DoesNotContain(labels, label => label.Text.Contains("Examples:"));
        Assert.DoesNotContain(labels, label => label.Text.Contains("exactly one decimal digit"));
        Assert.Contains(labels, label => label.Text.Contains("WORKSPACE"));
        Assert.Contains(labels, label => label.Text.Contains("PING FLUD"));
        var buttons = controls.OfType<Button>().ToList();
        Assert.Contains(buttons, button => button.Text.Contains("Syntax help"));
        var cards = controls.OfType<CardPanel>().ToList();
        Assert.NotEmpty(cards);
        Assert.All(cards, card => { Assert.True(card.CornerRadius >= 10); Assert.NotEqual(card.BackColor, card.BorderColor); });
        Assert.All(buttons, button => Assert.IsType<RoundedButton>(button));
        Assert.All(buttons, button => Assert.True(((RoundedButton)button).FocusCuesVisible, "RoundedButton must show focus cues for accessibility"));
        Assert.Contains(buttons, button => button.Text.Contains("Scan workspace"));
        Assert.Contains(buttons, button => button.Text.Contains("Import list"));
        Assert.Contains(buttons, button => button.Text.Contains("Scan settings"));
        Assert.DoesNotContain(buttons, button => button.Text.Trim().EndsWith("Results"));
        Assert.DoesNotContain(buttons, button => button.Text.Trim().EndsWith("Reports"));
        Assert.Contains(buttons, button => button.Text.Contains("Export CSV"));
        Assert.All(buttons.Where(button => button.Text is "Copy" or "Clear" or "Export CSV"), button => Assert.False(button.Enabled));
        var extraFormats = Assert.Single(controls.OfType<ComboBox>(), control => control.AccessibleName == "More export formats");
        Assert.Equal("More formats…", extraFormats.Text);
        Assert.Contains("PDF", extraFormats.Items.Cast<string>());
        var grid = Assert.Single(controls.OfType<DataGridView>());
        Assert.IsType<CardPanel>(grid.Parent);
        Assert.Equal(SortOrder.Ascending, grid.Columns["Address"].HeaderCell.SortGlyphDirection);
        var stop = Assert.Single(buttons, button => button.Text.Contains("Stop"));
        Assert.False(stop.Enabled);
        Assert.Equal(ThemeCatalog.Get("Graphite").SurfaceRaised, stop.BackColor);
    }

    private static MainForm CreateMainForm() => new(
        Path.Combine(Path.GetTempPath(), $"ping-flud-test-{Guid.NewGuid():N}", "settings.json"));

    private static IEnumerable<Control> Descendants(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }

    [Fact]
    public void PdfExportPreservesLongRowContent()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ping-flud-pdf-{Guid.NewGuid():N}.pdf");
        try
        {
            var suffix = "UNIQUE-PDF-TAIL";
            var row = new PingFlud.Core.ScanResult("target", true, 1, new string('h', 150) + suffix, "192.168.1.1", "Responding", 1, 1, 0, 64);
            SimplePdf.Write(path, new[] { row });
            var content = System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(path));
            Assert.Contains(suffix, content);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void MultiPagePngExportCreatesTheSelectedFileAndNumberedContinuations()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"ping-flud-png-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "report.png");
        Directory.CreateDirectory(directory);
        try
        {
            using var form = CreateMainForm();
            var rows = Enumerable.Range(1, 101)
                .Select(index => new PingFlud.Core.ScanResult(
                    $"target-{index}", true, index, string.Empty, $"192.0.2.{index % 255}",
                    "Responding", 1, 1, 0, 64))
                .ToList();

            typeof(MainForm).GetMethod("ExportImages", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(form, [path, rows]);

            Assert.True(File.Exists(path));
            Assert.True(new FileInfo(path).Length > 0);
            Assert.True(File.Exists(Path.Combine(directory, "report-002.png")));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void PublishingPngRemovesStaleContinuationFiles()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"ping-flud-publish-{Guid.NewGuid():N}");
        var staging = Path.Combine(directory, "staging");
        var destination = Path.Combine(directory, "report.png");
        Directory.CreateDirectory(staging);
        try
        {
            File.WriteAllText(Path.Combine(staging, "report.png"), "current");
            File.WriteAllText(Path.Combine(directory, "report-002.png"), "stale");

            typeof(MainForm).GetMethod("PublishExportFiles", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(null, [staging, destination]);

            Assert.Equal("current", File.ReadAllText(destination));
            Assert.False(File.Exists(Path.Combine(directory, "report-002.png")));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void SettingsDialogConstructsWithDefaultSettings()
    {
        using var dialog = new SettingsDialog(new AppState());
        dialog.CreateControl();
        dialog.PerformLayout();
        var subtitle = Descendants(dialog).OfType<TextBox>().Single(control => control.Text == "Fast, transparent network reachability checks");
        Assert.Equal("Scan settings", dialog.Text);
        Assert.True(dialog.ClientSize.Width >= 640);
        Assert.True(dialog.ClientSize.Height >= 660);
        Assert.True(subtitle.Width >= 300);
    }

    [Theory]
    [InlineData(1000, 1, 120000)]
    [InlineData(128, 1, 255)]
    [InlineData(65536, 1, 1000000)]
    public void NumericFactoryAppliesBoundsBeforeValue(decimal value, decimal minimum, decimal maximum)
    {
        using var control = SettingsDialog.CreateNumeric(value, minimum, maximum);

        Assert.Equal(minimum, control.Minimum);
        Assert.Equal(maximum, control.Maximum);
        Assert.Equal(value, control.Value);
    }

    [Fact]
    public void RoundedNumericUpDownShowsFocusCues()
    {
        using var numeric = SettingsDialog.CreateNumeric(1000, 1, 120000);
        var rounded = Assert.IsType<RoundedNumericUpDown>(numeric);
        Assert.True(rounded.FocusCuesVisible, "NumericUpDown must show focus cues for accessibility");
    }

    [Fact]
    public void SettingsDialogUsesTheActiveThemeForEveryEditableControl()
    {
        var theme = ThemeCatalog.Get("Midnight");
        using var dialog = new SettingsDialog(new AppState(), theme);
        dialog.CreateControl();

        var editable = Descendants(dialog)
            .Where(control => control is TextBox or NumericUpDown)
            .ToList();

        Assert.NotEmpty(editable);
        Assert.All(editable, control =>
        {
            Assert.Equal(theme.SurfaceRaised, control.BackColor);
            Assert.Equal(theme.Foreground, control.ForeColor);
        });
    }

    [Fact]
    public void SettingsDialogFitsAllSettingsWithoutRequiringItsOwnScrollBar()
    {
        using var dialog = new SettingsDialog(new AppState());
        dialog.CreateControl();
        dialog.PerformLayout();

        Assert.DoesNotContain(Descendants(dialog), control => control is VScrollBar);
    }

    [Fact]
    public void HeaderUsesAConciseSingleLineVersionLabel()
    {
        using var form = CreateMainForm();
        var version = Descendants(form).OfType<Label>()
            .Single(label => label.Text.StartsWith("Version ", StringComparison.Ordinal));

        Assert.DoesNotContain('\n', version.Text);
        Assert.DoesNotContain('\r', version.Text);
        Assert.DoesNotContain('+', version.Text);
        Assert.Equal(ContentAlignment.MiddleRight, version.TextAlign);
    }
}
