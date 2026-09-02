using System.Globalization;
using System.Text;
using PingFlud.Core;

namespace PingFlud.Application;

public static class SimplePdf
{
    private const int LinesPerPage = 51;
    private const int WrapWidth = 120;

    public static void Write(
        string path,
        IEnumerable<ScanResult> rows,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var rowList = rows as IReadOnlyList<ScanResult> ?? rows.ToArray();
        var wrappedLineCount = 0L;

        foreach (var row in rowList)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var lineLength = FormatRow(row).Length;
            wrappedLineCount += Math.Max(1, (lineLength + WrapWidth - 1) / WrapWidth);
        }

        var pageCountLong = Math.Max(1L, (wrappedLineCount + LinesPerPage - 1) / LinesPerPage);
        if (pageCountLong > int.MaxValue)
            throw new InvalidOperationException("The PDF report contains too many pages.");

        var pageCount = (int)pageCountLong;
        var fontId = 3 + pageCount * 2;
        var objectCount = fontId;
        var offsets = new long[objectCount + 1];

        using var stream = File.Create(path);
        WriteAscii(stream, "%PDF-1.4\n");

        void WriteObject(int id, string body)
        {
            offsets[id] = stream.Position;
            WriteAscii(stream, $"{id} 0 obj\n{body}\nendobj\n");
        }

        WriteObject(1, "<< /Type /Catalog /Pages 2 0 R >>");
        WriteObject(
            2,
            "<< /Type /Pages /Kids [" +
            string.Join(' ', Enumerable.Range(0, pageCount).Select(i => $"{3 + i * 2} 0 R")) +
            $"] /Count {pageCount} >>");

        using var wrappedLines = EnumerateWrappedLines(rowList, cancellationToken).GetEnumerator();
        for (var page = 0; page < pageCount; page++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var lines = new List<string>(LinesPerPage + 4)
            {
                "Ping Flud Results",
                $"Generated {DateTime.Now:u}",
                $"Page {page + 1} of {pageCount}",
                "Target | Status | ms | loss | Address | Reverse DNS"
            };

            while (lines.Count < LinesPerPage + 4 && wrappedLines.MoveNext())
                lines.Add(wrappedLines.Current);

            var content = new StringBuilder("BT /F1 8 Tf 32 805 Td 12 TL ");
            foreach (var line in lines)
                content.Append('(').Append(Escape(line)).Append(") Tj T* ");
            content.Append("ET");

            var pageId = 3 + page * 2;
            var contentId = 4 + page * 2;
            WriteObject(
                pageId,
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 842] /Resources << /Font << /F1 {fontId} 0 R >> >> /Contents {contentId} 0 R >>");
            WriteObject(
                contentId,
                $"<< /Length {Encoding.ASCII.GetByteCount(content.ToString())} >>\nstream\n{content}\nendstream");
        }

        WriteObject(fontId, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

        var xref = stream.Position;
        WriteAscii(stream, $"xref\n0 {objectCount + 1}\n0000000000 65535 f \n");
        for (var id = 1; id <= objectCount; id++)
            WriteAscii(stream, $"{offsets[id]:0000000000} 00000 n \n");
        WriteAscii(
            stream,
            $"trailer\n<< /Size {objectCount + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        stream.Flush(flushToDisk: true);
    }

    private static IEnumerable<string> EnumerateWrappedLines(
        IReadOnlyList<ScanResult> rows,
        CancellationToken cancellationToken)
    {
        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var line in Wrap(FormatRow(row), WrapWidth))
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return line;
            }
        }
    }

    private static string FormatRow(ScanResult row) =>
        $"{row.Target} | {row.Status} | {row.RoundtripMs?.ToString(CultureInfo.InvariantCulture) ?? string.Empty} ms | " +
        $"{row.PacketLossPercent.ToString("0.#", CultureInfo.InvariantCulture)}% loss | {row.Address} | {row.HostName}";

    private static IEnumerable<string> Wrap(string value, int width)
    {
        if (value.Length == 0) { yield return string.Empty; yield break; }
        for (var offset = 0; offset < value.Length; offset += width)
            yield return value.Substring(offset, Math.Min(width, value.Length - offset));
    }

    private static string Escape(string value)
    {
        var ascii = new StringBuilder();
        foreach (var rune in value.EnumerateRunes())
            ascii.Append(rune.Value < 128 ? (char)rune.Value : $"\\u{{{rune.Value:X}}}");
        return ascii.ToString().Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }

    private static void WriteAscii(Stream stream, string value) =>
        stream.Write(Encoding.ASCII.GetBytes(value));
}
