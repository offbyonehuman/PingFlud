using System.Net;
using System.Net.NetworkInformation;
using PingFlud.Core;
using Xunit;

namespace PingFlud.Core.Tests;

public sealed class PingScannerTests
{
    [Fact]
    public async Task FailedProbePreservesResolvedAddressForReverseDns()
    {
        var resolver = new FakeDnsResolver();
        var scanner = new PingScanner(resolver, new ThrowingPingProbe());
        var results = new List<ScanResult>();
        var settings = new ScanSettings
        {
            ResolveRespondingOnly = false,
            MaxOutstanding = 1,
            DnsTimeoutMs = 100
        };

        await scanner.ScanAsync(
            ["device.example"],
            settings,
            new Progress<ScanResult>(results.Add),
            CancellationToken.None);

        Assert.Equal(2, results.Count);
        var result = Assert.Single(results, row => row.HostName == "reverse.example");
        Assert.Equal("192.0.2.10", result.Address);
        Assert.Equal("reverse.example", result.HostName);
        Assert.Equal(1, resolver.ReverseLookupCount);
    }

    private sealed class FakeDnsResolver : IDnsResolver
    {
        public int ReverseLookupCount { get; private set; }

        public ValueTask<IPAddress[]> ResolveAddressesAsync(string host, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IPAddress[]>([IPAddress.Parse("192.0.2.10")]);

        public ValueTask<IPHostEntry> GetHostEntryAsync(IPAddress address, CancellationToken cancellationToken)
        {
            ReverseLookupCount++;
            return ValueTask.FromResult(new IPHostEntry { HostName = "reverse.example" });
        }
    }

    private sealed class ThrowingPingProbe : IPingProbe
    {
        public ValueTask<IPingProbe.Ipv4Reply?> SendAsync(
            IPAddress address,
            int timeoutMs,
            byte[] payload,
            PingOptions options,
            CancellationToken cancellationToken) =>
            ValueTask.FromException<IPingProbe.Ipv4Reply?>(new PingException("synthetic failure"));
    }
}
