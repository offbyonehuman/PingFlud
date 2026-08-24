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
    }
}

public static class ResultFilters
{
    public static IEnumerable<ScanResult> Apply(IEnumerable<ScanResult> rows, ResultFilter filter, string search) =>
        rows.Where(r =>
            (filter == ResultFilter.All || r.Responding == (filter == ResultFilter.Responding)) &&
            (string.IsNullOrWhiteSpace(search) ||
             string.Join(' ', r.Target, r.HostName, r.Address, r.Status)
                 .Contains(search, StringComparison.OrdinalIgnoreCase)));
}

public static class ExportFormatting
{
    public static string Csv(string? value)
    {
        value ??= string.Empty;
        // Prevent spreadsheet applications from interpreting untrusted cells as formulas.
        if (value.Length > 0 && value[0] is '=' or '+' or '-' or '@') value = "'" + value;
        return value.IndexOfAny([',', '"', '\r', '\n']) >= 0
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}

public sealed class PingScanner
{
    public async Task ScanAsync(
        IEnumerable<string> targets,
        ScanSettings settings,
        IProgress<ScanResult>? progress,
        CancellationToken cancellationToken)
    {
        settings.Validate();
        var pingOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = settings.MaxOutstanding,
            CancellationToken = cancellationToken
        };
        var pingResults = new ConcurrentBag<ScanResult>();

        await Parallel.ForEachAsync(targets, pingOptions, async (target, ct) =>
        {
            var result = await ProbeAsync(target, settings, ct);
            pingResults.Add(result);
            progress?.Report(result); // Reachability arrives before reverse DNS.
        });

        var dnsOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Min(32, settings.MaxOutstanding),
            CancellationToken = cancellationToken
        };
        await Parallel.ForEachAsync(pingResults.Where(r => r.Address.Length > 0), dnsOptions, async (result, ct) =>
        {
            try
            {
                var entry = await Dns.GetHostEntryAsync(IPAddress.Parse(result.Address)).WaitAsync(ct);
                if (!string.IsNullOrWhiteSpace(entry.HostName)) progress?.Report(result with { HostName = entry.HostName });
            }
            catch (OperationCanceledException) { throw; }
            catch { /* Reverse DNS is optional; the reachability result was already delivered. */ }
        });
    }

    private static async ValueTask<ScanResult> ProbeAsync(string target, ScanSettings settings, CancellationToken ct)
    {
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(target, ct);
            var address = addresses.FirstOrDefault() ?? throw new SocketException();
            long? best = null;
            IPStatus lastStatus = IPStatus.Unknown;
            var successes = 0;
            int? replyTtl = null;
            var payload = Encoding.UTF8.GetBytes(settings.Payload ?? string.Empty);

            for (var attempt = 0; attempt < settings.PingsPerNode; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(
                        address,
                        settings.TimeoutMs,
                        payload,
                        new PingOptions(settings.Ttl, true))
                    .WaitAsync(ct);
                lastStatus = reply.Status;
                if (reply.Status == IPStatus.Success)
                {
                    successes++;
                    best = Math.Min(best ?? long.MaxValue, reply.RoundtripTime);
                    replyTtl = reply.Options?.Ttl;
                }

                if (settings.DelayMs > 0 && attempt + 1 < settings.PingsPerNode)
                    await Task.Delay(settings.DelayMs, ct);
            }

            return new ScanResult(
                target,
                successes > 0,
                best,
                string.Empty,
                address.ToString(),
                successes > 0 ? "Responding" : lastStatus.ToString(),
                settings.PingsPerNode,
                successes,
                100d * (settings.PingsPerNode - successes) / settings.PingsPerNode,
                replyTtl);
        }
        catch (OperationCanceledException) { throw; }
        catch (PingException ex)
        {
            return Failed(target, "Ping error: " + (ex.InnerException?.Message ?? ex.Message), settings.PingsPerNode);
        }
        catch (Exception ex) when (ex is SocketException or ArgumentException)
        {
            return Failed(target, "Resolution failed", settings.PingsPerNode);
        }
    }

    private static ScanResult Failed(string target, string status, int attempts) =>
        new(target, false, null, string.Empty, string.Empty, status, attempts, 0, 100, null);
}
