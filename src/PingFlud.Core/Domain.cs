using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace PingFlud.Core;

public enum ResultFilter { All, Responding, NotResponding }

public sealed record ScanResult(
    string Target,
    bool Responding,
    long? RoundtripMs,
    string HostName,
    string Address,
    string Status,
    int Attempts,
    int Successes,
    double PacketLossPercent,
    int? ReplyTtl);

public sealed class ScanSettings
{
    public int MaxOutstanding { get; set; } = 64;
    public int TimeoutMs { get; set; } = 1000;
    public int PingsPerNode { get; set; } = 1;
    public int Ttl { get; set; } = 128;
    public int DelayMs { get; set; }
    public string Payload { get; set; } = "Ping Flud";
    public int ExpansionCap { get; set; } = 65_536;
    public int DnsTimeoutMs { get; set; } = 2000;
    public bool DontFragment { get; set; } = false;
    public bool ResolveRespondingOnly { get; set; } = true;

    public void Validate()
    {
        if (MaxOutstanding is < 1 or > 1024) throw new ArgumentOutOfRangeException(nameof(MaxOutstanding));
        if (TimeoutMs is < 1 or > 120_000) throw new ArgumentOutOfRangeException(nameof(TimeoutMs));
        if (PingsPerNode is < 1 or > 10) throw new ArgumentOutOfRangeException(nameof(PingsPerNode));
        if (Ttl is < 1 or > 255) throw new ArgumentOutOfRangeException(nameof(Ttl));
        if (DelayMs is < 0 or > 60_000) throw new ArgumentOutOfRangeException(nameof(DelayMs));
        if (Encoding.UTF8.GetByteCount(Payload ?? string.Empty) > 60_000)
            throw new ArgumentOutOfRangeException(nameof(Payload), "Payload exceeds 60,000 UTF-8 bytes.");
        if (ExpansionCap is < 1 or > 1_000_000) throw new ArgumentOutOfRangeException(nameof(ExpansionCap));
        if (DnsTimeoutMs is < 1 or > 30_000) throw new ArgumentOutOfRangeException(nameof(DnsTimeoutMs));
    }

    public ScanSettings Clone() => new()
    {
        MaxOutstanding = MaxOutstanding,
        TimeoutMs = TimeoutMs,
        PingsPerNode = PingsPerNode,
        Ttl = Ttl,
        DelayMs = DelayMs,
        Payload = Payload,
        ExpansionCap = ExpansionCap,
        DnsTimeoutMs = DnsTimeoutMs,
        DontFragment = DontFragment,
        ResolveRespondingOnly = ResolveRespondingOnly
    };
}

public static class ResultFilters
{
    public static IEnumerable<ScanResult> Apply(IEnumerable<ScanResult> rows, ResultFilter filter, string search) =>
        rows.Where(r =>
            (filter == ResultFilter.All || r.Responding == (filter == ResultFilter.Responding)) &&
            (string.IsNullOrWhiteSpace(search) || MatchesSearch(r, search)));

    private static bool MatchesSearch(ScanResult row, string search)
    {
        if (row.Target.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            row.HostName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            row.Address.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            row.Status.Contains(search, StringComparison.OrdinalIgnoreCase))
            return true;

        // Preserve the old cross-column search behavior for queries containing spaces
        // without allocating a joined string for every ordinary single-term search.
        return search.Any(char.IsWhiteSpace) &&
               string.Join(' ', row.Target, row.HostName, row.Address, row.Status)
                   .Contains(search, StringComparison.OrdinalIgnoreCase);
    }
}

public static class ExportFormatting
{
    public static string NeutralizeSpreadsheetFormula(string? value)
    {
        value ??= string.Empty;
        var content = value.TrimStart('\uFEFF').TrimStart();
        return content.Length > 0 && content[0] is '=' or '+' or '-' or '@'
            ? "'" + value
            : value;
    }

    public static string Csv(string? value)
    {
        // Prevent spreadsheet applications from interpreting untrusted cells as formulas.
        value = NeutralizeSpreadsheetFormula(value);
        return value.IndexOfAny([',', '"', '\r', '\n']) >= 0
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}

/// <summary>
/// Abstraction over DNS resolution so PingScanner can be tested with deterministic fakes.
/// </summary>
public interface IDnsResolver
{
    ValueTask<IPAddress[]> ResolveAddressesAsync(string host, CancellationToken ct);
    ValueTask<IPHostEntry> GetHostEntryAsync(IPAddress address, CancellationToken ct);
}

/// <summary>
/// Default DNS resolver that wraps the system <see cref="Dns"/> class.
/// </summary>
public sealed class SystemDnsResolver : IDnsResolver
{
    public static SystemDnsResolver Instance { get; } = new();

    public async ValueTask<IPAddress[]> ResolveAddressesAsync(string host, CancellationToken ct) =>
        await Dns.GetHostAddressesAsync(host, ct);

    public async ValueTask<IPHostEntry> GetHostEntryAsync(IPAddress address, CancellationToken ct) =>
        await Dns.GetHostEntryAsync(address.ToString(), ct);
}

/// <summary>
/// Abstraction over ICMP probing so PingScanner can be tested with deterministic fakes.
/// </summary>
public interface IPingProbe
{
    /// <summary>
    /// Sends a single ICMP echo request to <paramref name="address"/> and returns the reply.
    /// Returns <c>null</c> if the probe times out without a response.
    /// </summary>
    ValueTask<IPingProbe.Ipv4Reply?> SendAsync(IPAddress address, int timeoutMs, byte[] payload, PingOptions options, CancellationToken ct);

    public sealed record Ipv4Reply(IPStatus Status, long RoundtripMs, int? Ttl);
}

/// <summary>
/// Default ICMP probe that wraps <see cref="Ping"/>.
/// </summary>
public sealed class SystemPingProbe : IPingProbe
{
    public static SystemPingProbe Instance { get; } = new();

    public async ValueTask<IPingProbe.Ipv4Reply?> SendAsync(IPAddress address, int timeoutMs, byte[] payload, PingOptions options, CancellationToken ct)
    {
        using var ping = new Ping();
        var reply = await ping.SendPingAsync(address, TimeSpan.FromMilliseconds(timeoutMs), payload, options, ct);
        return reply.Status == IPStatus.TimedOut
            ? null
            : new IPingProbe.Ipv4Reply(reply.Status, reply.RoundtripTime, reply.Options?.Ttl);
    }
}

public sealed class PingScanner
{
    private readonly IDnsResolver _resolver;
    private readonly IPingProbe _probe;

    public PingScanner(IDnsResolver? resolver = null, IPingProbe? probe = null)
    {
        _resolver = resolver ?? SystemDnsResolver.Instance;
        _probe = probe ?? SystemPingProbe.Instance;
    }

    public async Task ScanAsync(
        IEnumerable<string> targets,
        ScanSettings settings,
        IProgress<ScanResult>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(targets);
        ArgumentNullException.ThrowIfNull(settings);
        var scanSettings = settings.Clone();
        scanSettings.Validate();
        var scanOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = scanSettings.MaxOutstanding,
            CancellationToken = cancellationToken
        };
        var results = new ConcurrentBag<ScanResult>();

        await Parallel.ForEachAsync(targets, scanOptions, async (target, ct) =>
        {
            var result = await ProbeAsync(target, scanSettings, ct);
            results.Add(result);
            progress?.Report(result);
        });

        // Phase 2: Reverse DNS
        var dnsTargets = scanSettings.ResolveRespondingOnly
            ? results.Where(r => r.Responding && r.Address.Length > 0)
            : results.Where(r => r.Address.Length > 0 || IPAddress.TryParse(r.Target, out _));

        if (!dnsTargets.Any()) return;

        var dnsOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Min(32, scanSettings.MaxOutstanding),
            CancellationToken = cancellationToken
        };
        await Parallel.ForEachAsync(dnsTargets, dnsOptions, async (result, ct) =>
        {
            try
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
                linked.CancelAfter(scanSettings.DnsTimeoutMs);
                var entry = await _resolver.GetHostEntryAsync(IPAddress.Parse(result.Address), linked.Token);
                if (!string.IsNullOrWhiteSpace(entry.HostName))
                    progress?.Report(result with { HostName = entry.HostName });
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch { /* Reverse DNS is optional; the reachability result was already delivered. */ }
        });
    }

    private async ValueTask<ScanResult> ProbeAsync(string target, ScanSettings settings, CancellationToken ct)
    {
        IPAddress? resolvedAddress = null;
        try
        {
            var addresses = await _resolver.ResolveAddressesAsync(target, ct);
            if (addresses.Length == 0) throw new SocketException();
            resolvedAddress = addresses[0];

            long? best = null;
            IPStatus lastStatus = IPStatus.Unknown;
            var successes = 0;
            int? replyTtl = null;
            var payload = Encoding.UTF8.GetBytes(settings.Payload ?? string.Empty);
            var pingOptions = new PingOptions(settings.Ttl, settings.DontFragment);
            var reachedAddress = string.Empty;

            for (var attempt = 0; attempt < settings.PingsPerNode; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                foreach (var candidate in addresses)
                {
                    var reply = await _probe.SendAsync(candidate, settings.TimeoutMs, payload, pingOptions, ct);
                    lastStatus = reply?.Status ?? IPStatus.TimedOut;
                    if (reply is not null && reply.Status == IPStatus.Success)
                    {
                        successes++;
                        best = Math.Min(best ?? long.MaxValue, reply.RoundtripMs);
                        replyTtl = reply.Ttl;
                        reachedAddress = candidate.ToString();
                        break; // One successful address completes this attempt.
                    }
                }

                if (settings.DelayMs > 0 && attempt + 1 < settings.PingsPerNode)
                    await Task.Delay(settings.DelayMs, ct);
            }

            return new ScanResult(
                target,
                successes > 0,
                best,
                string.Empty,
                reachedAddress.Length > 0 ? reachedAddress : addresses.First().ToString(),
                successes > 0 ? "Responding" : lastStatus.ToString(),
                settings.PingsPerNode,
                successes,
                100d * (settings.PingsPerNode - successes) / settings.PingsPerNode,
                replyTtl);
        }
        catch (OperationCanceledException) { throw; }
        catch (PingException ex)
        {
            return Failed(target, "Ping error: " + (ex.InnerException?.Message ?? ex.Message), settings.PingsPerNode, resolvedAddress);
        }
        catch (Exception ex) when (ex is SocketException or ArgumentException)
        {
            return Failed(target, "Resolution failed", settings.PingsPerNode, resolvedAddress);
        }
    }

    private static ScanResult Failed(string target, string status, int attempts, IPAddress? address = null) =>
        new(target, false, null, string.Empty, address?.ToString() ?? string.Empty, status, attempts, 0, 100, null);
}
