using System.Text;
using System.Web;
using PingFlud.Core;

namespace PingFlud.Application;

internal static class HtmlReportBuilder
{
    public static string Create(IEnumerable<ScanResult> rows, bool spreadsheet)
    {
        var writer = new StringBuilder();
        writer.Append("<!doctype html><meta charset=utf-8><title>Ping Flud Results</title>");
        writer.Append("<style>body{font:14px Segoe UI;background:#121212;color:#b4b4b4}" +
                      "table{border-collapse:collapse;width:100%}th,td{padding:8px;border:1px solid #464646}" +
                      "th{background:#1e1e1e}tr:nth-child(even){background:#2a2a2a}" +
                      ".up{color:#00ff00}.down{color:#ff4444}</style>");
        writer.Append("<h1>Ping Flud Results</h1><table><tr><th>Target</th><th>Status</th><th>Latency</th><th>Loss %</th>" +
                      "<th>Replies</th><th>TTL</th><th>Address</th><th>Reverse DNS</th></tr>");
        foreach (var row in rows)
        {
            var statusClass = row.Responding ? "up" : "down";
            writer.Append("<tr><td>").Append(Escape(row.Target, spreadsheet)).Append("</td>")
                  .Append("<td class='").Append(statusClass).Append("'>").Append(Escape(row.Status, spreadsheet)).Append("</td>")
                  .Append("<td>").Append(row.RoundtripMs).Append("</td>")
                  .Append("<td>").Append(row.PacketLossPercent.ToString("0.##")).Append("</td>")
                  .Append("<td>").Append($"{row.Successes}/{row.Attempts}").Append("</td>")
                  .Append("<td>").Append(row.ReplyTtl).Append("</td>")
                  .Append("<td>").Append(Escape(row.Address, spreadsheet)).Append("</td>")
                  .Append("<td>").Append(Escape(row.HostName, spreadsheet)).Append("</td></tr>");
        }
        writer.Append("</table>");
        return writer.ToString();
    }

    private static string Escape(string value, bool spreadsheet)
    {
        if (spreadsheet && value.Length > 0 && value[0] is '=' or '+' or '-' or '@')
            value = "'" + value;
        return HttpUtility.HtmlEncode(value);
    }
}
