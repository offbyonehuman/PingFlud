using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PingFlud.Core;

public static class TargetParser
{
    private const int MaximumInputBytes = 4 * 1024 * 1024;

    public static IReadOnlyList<string> Expand(
        string input,
        int cap = 65_536,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input)) throw new FormatException("Enter at least one target.");
        if (cap < 1) throw new ArgumentOutOfRangeException(nameof(cap));
        if (Encoding.UTF8.GetByteCount(input) > MaximumInputBytes)
            throw new InvalidOperationException("Target input exceeds the 4 MiB safety limit.");

        var result = new List<string>();
        foreach (var raw in input.Split([',', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dash = raw.IndexOf('-');
            if (raw.Contains('*') || raw.Contains('?'))
            {
                var parts = raw.Split('.');
                if (parts.Length != 4) throw new FormatException($"Invalid wildcard: {raw}");
                IEnumerable<string> accumulated = [""];
                foreach (var pattern in parts)
                {
                    var values = OctetValues(pattern, raw).ToArray();
                    if (values.Length == 0) throw new FormatException($"Wildcard has no valid IPv4 matches: {raw}");
                    accumulated = accumulated.SelectMany(prefix =>
                        values.Select(value => prefix.Length == 0 ? $"{value}" : $"{prefix}.{value}"));
                }
                foreach (var value in accumulated) Add(value);
            }
            else if (raw.Contains('/'))
            {
                var parts = raw.Split('/');
                if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out var ip) ||
                    ip.AddressFamily != AddressFamily.InterNetwork || !int.TryParse(parts[1], out var prefix) ||
                    prefix is < 0 or > 32)
                    throw new FormatException($"Invalid CIDR: {raw}");

                var baseIp = ToUInt(ip);
                var mask = prefix == 0 ? 0 : uint.MaxValue << (32 - prefix);
                var start = baseIp & mask;
                var count = 1UL << (32 - prefix);
                if (count > (ulong)cap)
                    throw new InvalidOperationException($"Target expansion exceeds safety cap ({cap:N0}).");
                for (ulong offset = 0; offset < count; offset++) Add(FromUInt(start + (uint)offset).ToString());
            }
            else if (dash > 0 && IPAddress.TryParse(raw[..dash], out var first) &&
                     IPAddress.TryParse(raw[(dash + 1)..], out var last) &&
                     first.AddressFamily == AddressFamily.InterNetwork && last.AddressFamily == first.AddressFamily)
            {
                var start = ToUInt(first);
                var end = ToUInt(last);
                if (end < start) throw new FormatException($"Range end precedes start: {raw}");
                for (var address = start;; address++)
                {
                    Add(FromUInt(address).ToString());
                    if (address == end) break;
                }
            }
            else
            {
                var looksLikeDottedQuad = raw.Count(character => character == '.') == 3 &&
                                          raw.Split('.').All(part => int.TryParse(part, out _));
                if (raw.Any(char.IsWhiteSpace) ||
                    (looksLikeDottedQuad && !IPAddress.TryParse(raw, out _)) ||
                    (!IPAddress.TryParse(raw, out _) && Uri.CheckHostName(raw) == UriHostNameType.Unknown))
                    throw new FormatException($"Invalid target: {raw}");
                Add(raw);
            }
        }

        if (result.Count == 0) throw new FormatException("The target specification produced no addresses.");
        return result.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        void Add(string value)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (result.Count >= cap)
                throw new InvalidOperationException($"Target expansion exceeds safety cap ({cap:N0}).");
            result.Add(value);
        }
    }

    private static IEnumerable<int> OctetValues(string pattern, string raw)
    {
        if (pattern.Length == 0 || pattern.Any(character => !char.IsDigit(character) && character is not '*' and not '?'))
            throw new FormatException($"Invalid wildcard: {raw}");
        if (!pattern.Contains('*') && !pattern.Contains('?'))
            return int.TryParse(pattern, out var value) && value is >= 0 and <= 255
                ? [value]
                : throw new FormatException($"Invalid wildcard: {raw}");
        return Enumerable.Range(0, 256).Where(value => Glob(pattern, value.ToString()));
    }

    private static bool Glob(string pattern, string value)
    {
        var matches = new bool[pattern.Length + 1, value.Length + 1];
        matches[0, 0] = true;
        for (var patternIndex = 1; patternIndex <= pattern.Length; patternIndex++)
        for (var valueIndex = 0; valueIndex <= value.Length; valueIndex++)
            matches[patternIndex, valueIndex] = pattern[patternIndex - 1] == '*'
                ? matches[patternIndex - 1, valueIndex] || valueIndex > 0 && matches[patternIndex, valueIndex - 1]
                : valueIndex > 0 && matches[patternIndex - 1, valueIndex - 1] &&
                  (pattern[patternIndex - 1] == '?' || pattern[patternIndex - 1] == value[valueIndex - 1]);
        return matches[pattern.Length, value.Length];
    }

    private static uint ToUInt(IPAddress ip)
    {
        var bytes = ip.GetAddressBytes();
        return ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
    }

    private static IPAddress FromUInt(uint value) => new(
        [(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value]);
}
