using System.Text;
using PingFlud.Core;

namespace PingFlud.Application;

public static class SimplePdf
{
    public static void Write(string path, IEnumerable<ScanResult> rows)
    {
        var data = rows.Select(row =>
            $"{row.Target} | {row.Status} | {row.RoundtripMs} ms | {row.PacketLossPercent:0.#}% loss | {row.Address} | {row.HostName}").ToList();
        var wrapped = data.SelectMany(line => Wrap(line, 120)).ToList();
        var pages = wrapped.Chunk(51).Select(chunk => chunk.ToList()).ToList();
        if (pages.Count == 0) pages.Add([]);

        var fontId = 3 + pages.Count * 2;
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [" + string.Join(' ', Enumerable.Range(0, pages.Count).Select(i => $"{3 + i * 2} 0 R")) + $"] /Count {pages.Count} >>"
        };

        for (var page = 0; page < pages.Count; page++)
        {
            var lines = new List<string>
            {
                "Ping Flud Results",
                $"Generated {DateTime.Now:u}",
                $"Page {page + 1} of {pages.Count}",
                "Target | Status | ms | loss | Address | Reverse DNS"
            };
            lines.AddRange(pages[page]);
            var content = new StringBuilder("BT /F1 8 Tf 32 805 Td 12 TL ");
            foreach (var line in lines)
                content.Append('(').Append(Escape(line)).Append(") Tj T* ");
            content.Append("ET");
            var contentId = 4 + page * 2;
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 842] /Resources << /Font << /F1 {fontId} 0 R >> >> /Contents {contentId} 0 R >>");
            objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(content.ToString())} >>\nstream\n{content}\nendstream");
        }

        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        using var stream = File.Create(path);
        using var writer = new StreamWriter(stream, Encoding.ASCII, 1024, true) { NewLine = "\n" };
        writer.Write("%PDF-1.4\n");
        writer.Flush();
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(stream.Position);
            writer.Write($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
            writer.Flush();
        }
        var xref = stream.Position;
        writer.Write($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) writer.Write($"{offset:0000000000} 00000 n \n");
        writer.Write($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
    }

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
}
