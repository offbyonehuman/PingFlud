using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PingFlud.Core;

public sealed class NetworkAddressComparer : IComparer<string?>
{
    public static NetworkAddressComparer Instance { get; } = new();

    private NetworkAddressComparer() { }

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (string.IsNullOrWhiteSpace(x)) return string.IsNullOrWhiteSpace(y) ? 0 : 1;
        if (string.IsNullOrWhiteSpace(y)) return -1;

        var xIsIp = IPAddress.TryParse(x, out var xAddress);
        var yIsIp = IPAddress.TryParse(y, out var yAddress);
        if (xIsIp && yIsIp) return CompareAddresses(xAddress!, yAddress!);
        if (xIsIp != yIsIp) return xIsIp ? -1 : 1;
        return CompareNatural(x, y);
    }

    private static int CompareAddresses(IPAddress x, IPAddress y)
    {
        if (x.AddressFamily != y.AddressFamily)
            return x.AddressFamily == AddressFamily.InterNetwork ? -1 : 1;

        var xb = x.GetAddressBytes();
        var yb = y.GetAddressBytes();
        for (var i = 0; i < xb.Length; i++)
        {
            var comparison = xb[i].CompareTo(yb[i]);
            if (comparison != 0) return comparison;
        }
        return 0;
    }

    private static int CompareNatural(string x, string y)
    {
        var xi = 0;
        var yi = 0;
        while (xi < x.Length && yi < y.Length)
        {
            if (char.IsDigit(x[xi]) && char.IsDigit(y[yi]))
            {
                var xStart = xi;
                var yStart = yi;
                while (xi < x.Length && char.IsDigit(x[xi])) xi++;
                while (yi < y.Length && char.IsDigit(y[yi])) yi++;

                var xDigits = x.AsSpan(xStart, xi - xStart);
                var yDigits = y.AsSpan(yStart, yi - yStart);
                var xTrimmed = xDigits.TrimStart('0');
                var yTrimmed = yDigits.TrimStart('0');
                if (xTrimmed.Length == 0) xTrimmed = "0";
                if (yTrimmed.Length == 0) yTrimmed = "0";
                var lengthComparison = xTrimmed.Length.CompareTo(yTrimmed.Length);
                if (lengthComparison != 0) return lengthComparison;
                var numberComparison = xTrimmed.CompareTo(yTrimmed, StringComparison.Ordinal);
                if (numberComparison != 0) return numberComparison;
                var zeroComparison = xDigits.Length.CompareTo(yDigits.Length);
                if (zeroComparison != 0) return zeroComparison;
                continue;
            }

            var characterComparison = char.ToUpperInvariant(x[xi]).CompareTo(char.ToUpperInvariant(y[yi]));
            if (characterComparison != 0) return characterComparison;
            xi++;
            yi++;
        }
        return x.Length.CompareTo(y.Length);
    }
}

public static class CsvReport
{
    public static string Create(IEnumerable<ScanResult> rows)
    {
        var output = new StringBuilder();
        using var writer = new StringWriter(output, CultureInfo.InvariantCulture);
        Write(writer, rows);
        return output.ToString();
    }

    public static void Write(
        TextWriter writer,
        IEnumerable<ScanResult> rows,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(rows);

        writer.Write("Target,Responding,LatencyMs,PacketLossPercent,Successes,Attempts,ReplyTtl,Address,HostName,Status\r\n");
        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var cells = new[]
            {
                row.Target,
                row.Responding.ToString(),
                row.RoundtripMs?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                row.PacketLossPercent.ToString("0.##", CultureInfo.InvariantCulture),
                row.Successes.ToString(CultureInfo.InvariantCulture),
                row.Attempts.ToString(CultureInfo.InvariantCulture),
                row.ReplyTtl?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                row.Address,
                row.HostName,
                row.Status
            };
            writer.Write(string.Join(',', cells.Select(ExportFormatting.Csv)));
            writer.Write("\r\n");
        }
    }
}
