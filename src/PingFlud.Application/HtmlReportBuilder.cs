using System.Globalization;
using System.Text;
using System.Web;
using PingFlud.Core;

namespace PingFlud.Application;

internal static class HtmlReportBuilder
{
    public static string Create(IEnumerable<ScanResult> rows, bool spreadsheet)
    {
        var output = new StringBuilder();
        using var writer = new StringWriter(output, CultureInfo.InvariantCulture);
        Write(writer, rows, spreadsheet);
        return output.ToString();
    }

    public static void Write(
        TextWriter writer,
        IEnumerable<ScanResult> rows,
        bool spreadsheet,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(rows);

        writer.Write("<!doctype html><meta charset=utf-8><title>Ping Flud Results</title>");
        writer.Write("<style>body{font:14px Segoe UI;background:#121212;color:#b4b4b4}" +
                     "table{border-collapse:collapse;width:100%}th,td{padding:8px;border:1px solid #464646}" +
                     "th{background:#1e1e1e}tr:nth-child(even){background:#2a2a2a}" +
                     ".up{color:#00ff00}.down{color:#ff4444}</style>");
        writer.Write("<h1>Ping Flud Results</h1><table><tr><th>Target</th><th>Status</th><th>Latency</th><th>Loss %</th>" +
                     "<th>Replies</th><th>TTL</th><th>Address</th><th>Reverse DNS</th></tr>");
        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var statusClass = row.Responding ? "up" : "down";
            writer.Write("<tr><td>");
            writer.Write(Escape(row.Target, spreadsheet));
            writer.Write("</td><td class='");
            writer.Write(statusClass);
            writer.Write("'>");
            writer.Write(Escape(row.Status, spreadsheet));
            writer.Write("</td><td>");
            writer.Write(row.RoundtripMs?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
            writer.Write("</td><td>");
            writer.Write(row.PacketLossPercent.ToString("0.##", CultureInfo.InvariantCulture));
            writer.Write("</td><td>");
            writer.Write(row.Successes.ToString(CultureInfo.InvariantCulture));
            writer.Write('/');
            writer.Write(row.Attempts.ToString(CultureInfo.InvariantCulture));
            writer.Write("</td><td>");
            writer.Write(row.ReplyTtl?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
            writer.Write("</td><td>");
            writer.Write(Escape(row.Address, spreadsheet));
            writer.Write("</td><td>");
            writer.Write(Escape(row.HostName, spreadsheet));
            writer.Write("</td></tr>");
        }
        writer.Write("</table>");
    }

    private static string Escape(string value, bool spreadsheet)
    {
        if (spreadsheet)
            value = ExportFormatting.NeutralizeSpreadsheetFormula(value);
        return HttpUtility.HtmlEncode(value);
    }
}
