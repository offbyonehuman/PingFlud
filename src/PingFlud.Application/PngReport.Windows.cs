#if NET8_0_OR_GREATER && WINDOWS

using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Text;
using PingFlud.Core;

namespace PingFlud.Application;

internal static partial class PngReport
{
    private static void WriteImpl(string path, IReadOnlyList<ScanResult> rows, CancellationToken cancellationToken)
    {
        var pageCount = (rows.Count + RowsPerImage - 1) / RowsPerImage;
        if (pageCount == 0) pageCount = 1;
        var directory = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Export target must be a file path.");
        var stem = Path.GetFileNameWithoutExtension(path);

        var columns = new[]
        {
            (0, 220, "Target"), (220, 210, "Status"), (430, 90, "Latency"), (520, 90, "Loss %"),
            (610, 90, "Replies"), (700, 70, "TTL"), (770, 260, "IP address"), (1030, 570, "Reverse DNS")
        };

        using var font = new Font("Segoe UI Variable", 9);
        using var bold = new Font(font, FontStyle.Bold);
        using var gridPen = new Pen(ColorTranslator.FromHtml("#464646"));
        using var textBrush = new SolidBrush(ColorTranslator.FromHtml("#b4b4b4"));
        using var headerBrush = new SolidBrush(ColorTranslator.FromHtml("#1e1e1e"));
        using var alternateBrush = new SolidBrush(ColorTranslator.FromHtml("#2a2a2a"));
        using var backgroundBrush = new SolidBrush(ColorTranslator.FromHtml("#121212"));

        for (var page = 0; page < pageCount; page++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var chunk = rows.Skip(page * RowsPerImage).Take(RowsPerImage).ToList();
            var output = page == 0 ? path : Path.Combine(directory, $"{stem}-{page + 1:000}.png");

            using var bitmap = new Bitmap(Width, HeaderHeight + chunk.Count * RowHeight);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.Clear(ColorTranslator.FromHtml("#121212"));
            graphics.FillRectangle(headerBrush, 0, 0, Width, HeaderHeight);

            foreach (var (x, columnWidth, label) in columns)
            {
                graphics.DrawString(label, bold, textBrush, new RectangleF(x + 5, 8, columnWidth - 10, 20));
                graphics.DrawLine(gridPen, x, 0, x, HeaderHeight + chunk.Count * RowHeight);
            }
            graphics.DrawLine(gridPen, 0, HeaderHeight, Width, HeaderHeight);

            for (var i = 0; i < chunk.Count; i++)
            {
                var row = chunk[i];
                var y = HeaderHeight + i * RowHeight;
                if (i % 2 == 1) graphics.FillRectangle(alternateBrush, 0, y, Width, RowHeight);
                var values = new[]
                {
                    row.Target, row.Status, row.RoundtripMs?.ToString(CultureInfo.InvariantCulture) ?? "",
                    row.PacketLossPercent.ToString("0.##", CultureInfo.InvariantCulture),
                    $"{row.Successes}/{row.Attempts}", row.ReplyTtl?.ToString(CultureInfo.InvariantCulture) ?? "",
                    row.Address, row.HostName
                };
                for (var column = 0; column < columns.Length; column++)
                {
                    var (x, columnWidth, _) = columns[column];
                    graphics.DrawString(values[column], font, textBrush,
                        new RectangleF(x + 5, y + 4, columnWidth - 10, RowHeight - 4));
                }
                graphics.DrawLine(gridPen, 0, y + RowHeight, Width, y + RowHeight);
            }

            bitmap.Save(output, ImageFormat.Png);
        }
    }
}

#endif
