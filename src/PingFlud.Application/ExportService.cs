using System.Globalization;
using System.Text;
using PingFlud.Core;

namespace PingFlud.Application;

public enum ExportKind
{
    Csv,
    Html,
    SpreadsheetHtml,
    Txt,
    Pdf,
    PngImage
}

public static class ExportService
{
    public static Task ExecuteAsync(ExportKind kind, string path, IReadOnlyList<ScanResult> rows, CancellationToken cancellationToken = default) =>
        Task.Run(() => WriteAndPublish(kind, path, rows, cancellationToken), cancellationToken);

    private static void WriteAndPublish(ExportKind kind, string path, IReadOnlyList<ScanResult> rows, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);
        cancellationToken.ThrowIfCancellationRequested();

        var destinationPath = Path.GetFullPath(path);
        var destinationDirectory = Path.GetDirectoryName(destinationPath)
            ?? throw new InvalidOperationException("Export target must be a file path.");
        var fileName = Path.GetFileName(destinationPath);
        if (string.IsNullOrWhiteSpace(fileName)) throw new InvalidOperationException("Export target must be a file path.");

        var stagingDirectory = Path.Combine(destinationDirectory, $".pingflud-export-{Guid.NewGuid():N}");
        Directory.CreateDirectory(stagingDirectory);
        try
        {
            Write(kind, Path.Combine(stagingDirectory, fileName), rows, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            PublishFiles(stagingDirectory, destinationPath);
        }
        finally
        {
            if (Directory.Exists(stagingDirectory)) Directory.Delete(stagingDirectory, recursive: true);
        }
    }

    private static void Write(ExportKind kind, string path, IReadOnlyList<ScanResult> rows, CancellationToken cancellationToken)
    {
        switch (kind)
        {
            case ExportKind.Csv:
                File.WriteAllText(path, CsvReport.Create(rows), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
                break;
            case ExportKind.Html:
                File.WriteAllText(path, HtmlReportBuilder.Create(rows, spreadsheet: false), Encoding.UTF8);
                break;
            case ExportKind.SpreadsheetHtml:
                File.WriteAllText(path, HtmlReportBuilder.Create(rows, spreadsheet: true), Encoding.UTF8);
                break;
            case ExportKind.Txt:
                File.WriteAllLines(path, rows.Select(row =>
                    string.Join('\t', row.Target, row.Address, row.HostName, row.Status,
                        row.RoundtripMs?.ToString(CultureInfo.InvariantCulture) ?? string.Empty)),
                    Encoding.UTF8);
                break;
            case ExportKind.Pdf:
                SimplePdf.Write(path, rows);
                break;
            case ExportKind.PngImage:
                PngReport.Write(path, rows, cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported export format.");
        }
    }

    private static void PublishFiles(string stagingDirectory, string destinationPath)
    {
        var destinationDirectory = Path.GetDirectoryName(destinationPath)!;
        var stagedFiles = Directory.EnumerateFiles(stagingDirectory).ToArray();
        var stagedNames = stagedFiles
            .Select(Path.GetFileName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var stagedFile in stagedFiles)
            File.Move(stagedFile, Path.Combine(destinationDirectory, Path.GetFileName(stagedFile)), overwrite: true);

        if (!string.Equals(Path.GetExtension(destinationPath), ".png", StringComparison.OrdinalIgnoreCase)) return;

        var stem = Path.GetFileNameWithoutExtension(destinationPath);
        foreach (var previousPage in Directory.EnumerateFiles(destinationDirectory, $"{stem}-*.png"))
        {
            var fileName = Path.GetFileName(previousPage);
            if (stagedNames.Contains(fileName)) continue;
            var suffix = Path.GetFileNameWithoutExtension(previousPage)[(stem.Length + 1)..];
            if (suffix.Length == 3 && int.TryParse(suffix, out var pageNumber) && pageNumber >= 2)
                File.Delete(previousPage);
        }
    }
}
