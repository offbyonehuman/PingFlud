using PingFlud.Application;
using PingFlud.Core;
using Xunit;

namespace PingFlud.Application.Tests;

public sealed class ExportServiceTests
{
    [Fact]
    public void PngCleanupRemovesContinuationPagesBeyond999()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"pingflud-png-pages-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "report-002.png"), string.Empty);
            File.WriteAllText(Path.Combine(directory, "report-1000.png"), string.Empty);
            File.WriteAllText(Path.Combine(directory, "report-note.png"), string.Empty);

            ExportService.RemoveStalePngPages(
                directory,
                "report",
                new HashSet<string>(["report.png", "report-002.png"], StringComparer.OrdinalIgnoreCase));

            Assert.True(File.Exists(Path.Combine(directory, "report-002.png")));
            Assert.False(File.Exists(Path.Combine(directory, "report-1000.png")));
            Assert.True(File.Exists(Path.Combine(directory, "report-note.png")));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private static readonly ScanResult Sample = new(
        Target: "host, with comma",
        Responding: true,
        RoundtripMs: 12,
        HostName: "host.example",
        Address: "10.0.0.1",
        Status: "Responding",
        Attempts: 1,
        Successes: 1,
        PacketLossPercent: 0,
        ReplyTtl: 64);

    [Fact]
    public async Task CsvExportProducesQuotedHeaderAndRow()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pingflud-csv-{Guid.NewGuid():N}.csv");

        await ExportService.ExecuteAsync(ExportKind.Csv, path, [Sample]);

        var content = File.ReadAllText(path);
        Assert.Contains("Target", content);
        Assert.Contains("Address", content);
        Assert.Contains("\"host, with comma\"", content);
        File.Delete(path);
    }

    [Fact]
    public async Task CsvExportNeutralizesFormulaCells()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pingflud-csv-formula-{Guid.NewGuid():N}.csv");
        var row = Sample with { Target = "=cmd|'/c calc'!A0" };

        await ExportService.ExecuteAsync(ExportKind.Csv, path, [row]);

        var content = File.ReadAllText(path);
        var targetLine = content.Split('\n').First(line => line.Contains("cmd"));
        Assert.StartsWith("'", targetLine.TrimStart().TrimStart('"'));
        File.Delete(path);
    }

    [Fact]
    public async Task HtmlExportProducesEscapedContent()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pingflud-html-{Guid.NewGuid():N}.html");
        var row = Sample with { Target = "<script>alert(1)</script>" };

        await ExportService.ExecuteAsync(ExportKind.Html, path, [row]);

        var content = File.ReadAllText(path);
        Assert.DoesNotContain("<script>alert(1)</script>", content);
        Assert.Contains("&lt;script&gt;", content);
        File.Delete(path);
    }

    [Fact]
    public async Task SpreadsheetHtmlIsStillValidHtml()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pingflud-spreadsheet-{Guid.NewGuid():N}.html");

        await ExportService.ExecuteAsync(ExportKind.SpreadsheetHtml, path, [Sample]);

        var content = File.ReadAllText(path);
        Assert.Contains("<!doctype html>", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<table", content);
        Assert.Contains(Sample.Target, content);
        File.Delete(path);
    }

    [Fact]
    public async Task TxtExportProducesTabSeparatedRow()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pingflud-txt-{Guid.NewGuid():N}.txt");

        await ExportService.ExecuteAsync(ExportKind.Txt, path, [Sample]);

        var lines = await File.ReadAllLinesAsync(path);
        Assert.Single(lines);
        Assert.Contains('\t', lines[0]);
        File.Delete(path);
    }

    [Fact]
    public async Task PdfExportProducesFileStartingWithPdfMagic()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pingflud-pdf-{Guid.NewGuid():N}.pdf");

        await ExportService.ExecuteAsync(ExportKind.Pdf, path, [Sample]);

        var firstBytes = await File.ReadAllBytesAsync(path);
        Assert.True(firstBytes.Length > 5);
        Assert.Equal((byte)'%', firstBytes[0]);
        Assert.Equal((byte)'P', firstBytes[1]);
        Assert.Equal((byte)'D', firstBytes[2]);
        Assert.Equal((byte)'F', firstBytes[3]);
        File.Delete(path);
    }

    [Fact]
    public async Task ExportHonoursCancellation()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pingflud-cancelled-{Guid.NewGuid():N}.csv");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ExportService.ExecuteAsync(ExportKind.Csv, path, [Sample], cts.Token));

        Assert.False(File.Exists(path), "Cancelled export must not leave a file behind.");
    }

    [Fact]
    public void CsvReportRejectsBareEqualsAtStart()
    {
        var rows = new[] { Sample with { Target = "=SUM(A1:A2)" } };

        var content = CsvReport.Create(rows);

        var targetLine = content.Split('\n').First(line => line.Contains("SUM"));
        Assert.StartsWith("'", targetLine.TrimStart().TrimStart('"'));
    }

    [Fact]
    public void SpreadsheetHtmlNeutralizesFormulaPrefixesInEveryTextCell()
    {
        var row = Sample with
        {
            Target = "=target-formula",
            Status = "+status-formula",
            Address = "-address-formula",
            HostName = "@host-formula"
        };

        var content = HtmlReportBuilder.Create([row], spreadsheet: true);

        Assert.Contains(">&#39;=target-formula<", content);
        Assert.Contains(">&#39;+status-formula<", content);
        Assert.Contains(">&#39;-address-formula<", content);
        Assert.Contains(">&#39;@host-formula<", content);
    }

    [Fact]
    public async Task PngReexportRemovesStaleContinuationPages()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"pingflud-png-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "report.png");
        Directory.CreateDirectory(directory);
        try
        {
            var manyRows = Enumerable.Range(1, 101)
                .Select(index => Sample with { Target = $"target-{index}" })
                .ToArray();
            await ExportService.ExecuteAsync(ExportKind.PngImage, path, manyRows);
            Assert.True(File.Exists(Path.Combine(directory, "report-002.png")));

            await ExportService.ExecuteAsync(ExportKind.PngImage, path, [Sample]);

            Assert.True(File.Exists(path));
            Assert.False(File.Exists(Path.Combine(directory, "report-002.png")));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void PngPublicationRollsBackWhenOnePageCannotBeReplaced()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"pingflud-png-rollback-{Guid.NewGuid():N}");
        var staging = Path.Combine(directory, "staging");
        var destination = Path.Combine(directory, "report.png");
        Directory.CreateDirectory(staging);
        try
        {
            File.WriteAllText(destination, "old-first-page");
            File.WriteAllText(Path.Combine(staging, "report.png"), "new-first-page");
            File.WriteAllText(Path.Combine(staging, "report-002.png"), "new-second-page");
            Directory.CreateDirectory(Path.Combine(directory, "report-002.png"));

            Assert.ThrowsAny<IOException>(() =>
                ExportService.PublishFiles(staging, destination, CancellationToken.None));

            Assert.Equal("old-first-page", File.ReadAllText(destination));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void HtmlReportRejectsScriptTags()
    {
        var rows = new[] { Sample with { HostName = "<img src=x onerror=alert(1)>" } };

        var content = HtmlReportBuilder.Create(rows, spreadsheet: false);

        Assert.DoesNotContain("<img src=x onerror=alert(1)>", content);
    }
}
