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
            PublishFiles(stagingDirectory, destinationPath, cancellationToken);
        }
        finally
        {
            TryDeleteDirectory(stagingDirectory);
        }
    }

    private static void Write(ExportKind kind, string path, IReadOnlyList<ScanResult> rows, CancellationToken cancellationToken)
    {
        switch (kind)
        {
            case ExportKind.Csv:
                using (var writer = new StreamWriter(
                           path,
                           append: false,
                           new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)))
                    CsvReport.Write(writer, rows, cancellationToken);
                break;
            case ExportKind.Html:
                using (var writer = new StreamWriter(path, append: false, Encoding.UTF8))
                    HtmlReportBuilder.Write(writer, rows, spreadsheet: false, cancellationToken);
                break;
            case ExportKind.SpreadsheetHtml:
                using (var writer = new StreamWriter(path, append: false, Encoding.UTF8))
                    HtmlReportBuilder.Write(writer, rows, spreadsheet: true, cancellationToken);
                break;
            case ExportKind.Txt:
                using (var writer = new StreamWriter(path, append: false, Encoding.UTF8))
                {
                    foreach (var row in rows)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        writer.Write(row.Target);
                        writer.Write('\t');
                        writer.Write(row.Address);
                        writer.Write('\t');
                        writer.Write(row.HostName);
                        writer.Write('\t');
                        writer.Write(row.Status);
                        writer.Write('\t');
                        writer.WriteLine(row.RoundtripMs?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
                    }
                }
                break;
            case ExportKind.Pdf:
                SimplePdf.Write(path, rows, cancellationToken);
                break;
            case ExportKind.PngImage:
                PngReport.Write(path, rows, cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported export format.");
        }
    }

    internal static void PublishFiles(
        string stagingDirectory,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        var destinationDirectory = Path.GetDirectoryName(destinationPath)!;
        var stagedFiles = Directory.EnumerateFiles(stagingDirectory).ToArray();
        var stagedNames = stagedFiles
            .Select(static file => Path.GetFileName(file)!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var filesToReplace = stagedNames.ToList();
        if (string.Equals(Path.GetExtension(destinationPath), ".png", StringComparison.OrdinalIgnoreCase))
        {
            var stem = Path.GetFileNameWithoutExtension(destinationPath);
            filesToReplace.AddRange(GetStalePngPageNames(destinationDirectory, stem, stagedNames));
        }

        var backupDirectory = Path.Combine(
            destinationDirectory,
            $".pingflud-export-backup-{Guid.NewGuid():N}");
        var backups = new List<(string Destination, string Backup)>();
        var published = new List<string>();

        try
        {
            Directory.CreateDirectory(backupDirectory);

            foreach (var fileName in filesToReplace.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var destination = Path.Combine(destinationDirectory, fileName);
                if (!File.Exists(destination)) continue;

                var backup = Path.Combine(backupDirectory, $"{Guid.NewGuid():N}.bak");
                File.Move(destination, backup);
                backups.Add((destination, backup));
            }

            foreach (var stagedFile in stagedFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var destination = Path.Combine(destinationDirectory, Path.GetFileName(stagedFile));
                File.Move(stagedFile, destination);
                published.Add(destination);
            }

            // The backup set is no longer needed only after every new file is visible.
            Directory.Delete(backupDirectory, recursive: true);
        }
        catch
        {
            foreach (var destination in published.AsEnumerable().Reverse())
            {
                try
                {
                    if (File.Exists(destination)) File.Delete(destination);
                }
                catch { /* Preserve the original publication failure. */ }
            }

            foreach (var (destination, backup) in backups.AsEnumerable().Reverse())
            {
                try
                {
                    if (File.Exists(backup)) File.Move(backup, destination, overwrite: true);
                }
                catch { /* Preserve the original publication failure. */ }
            }

            throw;
        }
        finally
        {
            TryDeleteDirectory(backupDirectory);
        }
    }

    internal static void RemoveStalePngPages(
        string destinationDirectory,
        string stem,
        IReadOnlySet<string> stagedNames)
    {
        foreach (var fileName in GetStalePngPageNames(destinationDirectory, stem, stagedNames))
            File.Delete(Path.Combine(destinationDirectory, fileName));
    }

    private static IEnumerable<string> GetStalePngPageNames(
        string destinationDirectory,
        string stem,
        IReadOnlySet<string> stagedNames)
    {
        foreach (var previousPage in Directory.EnumerateFiles(destinationDirectory, $"{stem}-*.png"))
        {
            var fileName = Path.GetFileName(previousPage);
            if (stagedNames.Contains(fileName)) continue;
            var suffix = Path.GetFileNameWithoutExtension(previousPage)[(stem.Length + 1)..];
            if (int.TryParse(suffix, out var pageNumber) && pageNumber >= 2)
                yield return fileName;
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        }
        catch { /* Cleanup must not hide the original operation result. */ }
    }
}
