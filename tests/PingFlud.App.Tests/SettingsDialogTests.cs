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
    public void ExportMenuIsReusableAndNotDisposedWhenClosed()
    {
        using var form = CreateMainForm();
        var first = form.GetOrCreateExportMenu();
        first.Close();
        var second = form.GetOrCreateExportMenu();

        Assert.Same(first, second);
        Assert.False(second.IsDisposed);
        form.Dispose();
        Assert.True(second.IsDisposed);
    }

    [Fact]
    public void ProvidesThreePersistentThemes()
    {
        Assert.Equal("Midnight", new AppState().ThemeName);
        Assert.True(ThemeCatalog.All.Count >= 7);
        Assert.Equal(ThemeCatalog.All.Count, ThemeCatalog.All.Select(theme => theme.Name).Distinct().Count());
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
    public void MainWindowShowsUsageLegendAndCsvReportButton()
    {
        using var form = CreateMainForm();
        var controls = Descendants(form).ToList();

        var labels = controls.OfType<Label>().ToList();
        Assert.Contains(labels, label => label.Text.Contains("Examples:") && label.Text.Contains("/24"));
        Assert.Contains(labels, label => label.Text.Contains("?") && label.Text.Contains("exactly one decimal digit"));
        Assert.Contains(labels, label => label.Text.Contains("*") && label.Text.Contains("any valid octet digits"));
        Assert.Contains(labels, label => label.Text.Contains("Range / CIDR"));
        Assert.Contains(labels, label => label.Text.Contains("WORKSPACE"));
        Assert.Contains(labels, label => label.Text.Contains("PING FLUD"));
        Assert.Contains(labels, label => label.Text.Contains("Import .txt/.csv") && label.Text.Contains("one target"));
        var buttons = controls.OfType<Button>().ToList();
        var cards = controls.OfType<CardPanel>().ToList();
        Assert.NotEmpty(cards);
        Assert.All(cards, card => { Assert.True(card.CornerRadius >= 10); Assert.NotEqual(card.BackColor, card.GradientColor); });
        Assert.All(buttons, button => Assert.IsType<RoundedButton>(button));
        Assert.Contains(buttons, button => button.Text.Contains("Scan workspace"));
        Assert.Contains(buttons, button => button.Text.Contains("Import list"));
        Assert.DoesNotContain(buttons, button => button.Text.Trim().EndsWith("Results"));
        Assert.DoesNotContain(buttons, button => button.Text.Trim().EndsWith("Reports"));
        Assert.Contains(buttons, button => button.Text.Contains("Export CSV"));
        var grid = Assert.Single(controls.OfType<DataGridView>());
        Assert.IsType<CardPanel>(grid.Parent);
        Assert.Equal(SortOrder.Ascending, grid.Columns["Address"].HeaderCell.SortGlyphDirection);
        var stop = Assert.Single(buttons, button => button.Text.Contains("Stop"));
        Assert.False(stop.Enabled);
        Assert.Equal(ThemeCatalog.Get("Midnight").SurfaceRaised, stop.BackColor);
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
}
