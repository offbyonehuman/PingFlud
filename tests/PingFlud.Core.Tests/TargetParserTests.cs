using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using PingFlud.Core;
using Xunit;
namespace PingFlud.Core.Tests;
public class TargetParserTests
{
 [Fact] public void ParsesIPv4RangeInclusive()=>Assert.Equal(new[]{"192.168.1.1","192.168.1.2","192.168.1.3"},TargetParser.Expand("192.168.1.1-192.168.1.3",100));
 [Fact] public void ExpandsCidr()=>Assert.Equal(4,TargetParser.Expand("10.0.0.0/30",100).Count);
 [Fact] public void ExpandsStarAndQuestionWildcards(){var x=TargetParser.Expand("10.0.0.?",100);Assert.Equal(10,x.Count);Assert.Contains("10.0.0.9",x);Assert.Equal(256,TargetParser.Expand("10.0.1.*",300).Count);}
 [Fact] public void ExpandsEmbeddedDecimalWildcard(){var x=TargetParser.Expand("10.0.0.1?",100);Assert.Equal(10,x.Count);Assert.Contains("10.0.0.10",x);Assert.Contains("10.0.0.19",x);}
 [Fact] public void PreservesHostnamesAndIpv6AndDeduplicates()=>Assert.Equal(new[]{"server.local","::1"},TargetParser.Expand("server.local, ::1, SERVER.local",100));
 [Fact] public void EnforcesSafetyCap()=>Assert.Throws<InvalidOperationException>(()=>TargetParser.Expand("10.0.*.*",100));
 [Fact] public void RejectsInvalidSpecs()=>Assert.Throws<FormatException>(()=>TargetParser.Expand("999.2.3.4",100));
 [Fact] public void RejectsInvalidHostnameCharacters()=>Assert.Throws<FormatException>(()=>TargetParser.Expand("host!name",100));
 [Fact] public void RejectsWildcardWithNoValidOctetMatches()=>Assert.Throws<FormatException>(()=>TargetParser.Expand("10.0.0.9??",100));
 [Fact] public void ExpansionObservesCancellation(){using var cancellation=new CancellationTokenSource();cancellation.Cancel();Assert.Throws<OperationCanceledException>(()=>TargetParser.Expand("10.*.*.*",1_000_000,cancellation.Token));}
}
public class SettingsTests
{
 [Fact] public void RejectsInvalidScanSettings()=>Assert.Throws<ArgumentOutOfRangeException>(()=>new ScanSettings{TimeoutMs=0}.Validate());
 [Fact] public void RejectsPayloadThatExceedsUtf8ByteLimit()=>Assert.Throws<ArgumentOutOfRangeException>(()=>new ScanSettings{Payload=new string('界',20001)}.Validate());
 [Fact] public void CsvEscapesSpecialCharacters()=>Assert.Equal("\"a,b\"",ExportFormatting.Csv("a,b"));
 [Theory]
 [InlineData("=1+1","'=1+1")]
 [InlineData("+cmd","'+cmd")]
 [InlineData("-2","'-2")]
 [InlineData("@SUM(A1)","'@SUM(A1)")]
 public void CsvNeutralizesSpreadsheetFormulas(string input,string expected)=>Assert.Equal(expected,ExportFormatting.Csv(input));
 [Fact] public void FiltersResults(){var data=new[]{new ScanResult("a",true,1,"h","1.1.1.1","OK",1,1,0,64),new ScanResult("b",false,null,"","","Timeout",1,0,100,null)};Assert.Single(ResultFilters.Apply(data,ResultFilter.Responding,""));Assert.Single(ResultFilters.Apply(data,ResultFilter.All,"time"));}
 [Fact] public void SortsIpv4AddressesNumerically(){var values=new[]{"192.168.1.100","192.168.1.10","192.168.1.3","192.168.1.2","192.168.1.1"};Assert.Equal(new[]{"192.168.1.1","192.168.1.2","192.168.1.3","192.168.1.10","192.168.1.100"},values.OrderBy(x=>x,NetworkAddressComparer.Instance));}
 [Fact] public void SortsNaturalTargetNames(){var values=new[]{"router10","router2","router1"};Assert.Equal(new[]{"router1","router2","router10"},values.OrderBy(x=>x,NetworkAddressComparer.Instance));}
 [Fact] public void BuildsCsvReport(){var rows=new[]{new ScanResult("192.168.1.1",true,4,"router","192.168.1.1","Responding",2,2,0,64)};var csv=CsvReport.Create(rows);Assert.Contains("Target,Responding,LatencyMs",csv);Assert.Contains("192.168.1.1,True,4",csv);Assert.EndsWith("\r\n",csv);}
 [Fact] public async Task ScannerCanPingLoopback(){var rows=new List<ScanResult>();await new PingScanner().ScanAsync(new[]{"127.0.0.1"},new ScanSettings{TimeoutMs=2000},new ImmediateProgress<ScanResult>(rows.Add),CancellationToken.None);var row=Assert.IsType<ScanResult>(rows.First());Assert.True(row.Responding);Assert.Equal(1,row.Successes);Assert.Equal(0,row.PacketLossPercent);Assert.InRange(rows.Count,1,2);}

 [Fact]
 public async Task ScannerTriesAllResolvedAddressesUntilOneResponds()
 {
     var fakeResolver = new FakeDnsResolver(["10.0.0.1", "127.0.0.1"]);
     var fakeProbe = new FakePingProbe(ip => ip.ToString() == "10.0.0.1" ? null : new IPingProbe.Ipv4Reply(IPStatus.Success, 5, 128));
     var scanner = new PingScanner(fakeResolver, fakeProbe);
     var results = new Dictionary<string, ScanResult>();
     var progress = new Progress<ScanResult>(r => results[r.Target] = r);
     await scanner.ScanAsync(new[] { "host.example" }, new ScanSettings { TimeoutMs = 2000 }, progress, CancellationToken.None);
     var row = Assert.Single(results.Values);
     Assert.True(row.Responding);
     Assert.Equal("127.0.0.1", row.Address);
 }

 [Fact]
 public async Task ScannerRunsEveryConfiguredAttemptAndCalculatesPacketLoss()
 {
     var fakeResolver = new FakeDnsResolver(["127.0.0.1"]);
     var fakeProbe = new SequencePingProbe(
         new IPingProbe.Ipv4Reply(IPStatus.Success, 8, 64),
         null,
         new IPingProbe.Ipv4Reply(IPStatus.Success, 3, 63));
     var results = new Dictionary<string, ScanResult>();

     await new PingScanner(fakeResolver, fakeProbe).ScanAsync(
         ["host.example"],
         new ScanSettings { PingsPerNode = 3, DelayMs = 0 },
         new ImmediateProgress<ScanResult>(result => results[result.Target] = result),
         CancellationToken.None);

     var row = Assert.Single(results.Values);
     Assert.Equal(3, fakeProbe.CallCount);
     Assert.Equal(3, row.Attempts);
     Assert.Equal(2, row.Successes);
     Assert.Equal(3, row.RoundtripMs);
     Assert.Equal(100d / 3d, row.PacketLossPercent, precision: 10);
 }

 [Fact]
 public async Task ScannerPassesDontFragmentSettingToPingOptions()
 {
     var fakeResolver = new FakeDnsResolver(["127.0.0.1"]);
     var fakeProbe = new SequencePingProbe(new IPingProbe.Ipv4Reply(IPStatus.Success, 1, 64));

     await new PingScanner(fakeResolver, fakeProbe).ScanAsync(
         ["host.example"],
         new ScanSettings { DontFragment = true },
         progress: null,
         CancellationToken.None);

     Assert.True(fakeProbe.LastOptions!.DontFragment);
 }

 [Fact]
 public async Task ScannerSkipsReverseDnsForNonRespondingHosts()
 {
     var fakeResolver = new FakeDnsResolver(["192.168.1.1"]);
     var fakeProbe = new FakePingProbe(ip => null);
     var scanner = new PingScanner(fakeResolver, fakeProbe);
     var rows = new List<ScanResult>();
     await scanner.ScanAsync(new[] { "non-responding.example" }, new ScanSettings { TimeoutMs = 2000, ResolveRespondingOnly = true }, new Progress<ScanResult>(rows.Add), CancellationToken.None);
     var row = Assert.Single(rows);
     Assert.False(row.Responding);
     Assert.False(fakeResolver.DnsCalled, "Reverse DNS should not be called for non-responding hosts");
 }

 [Fact]
 public async Task ScannerEnforcesDnsTimeout()
 {
     var fakeResolver = new FakeDnsResolver(["127.0.0.1"]);
     var fakeProbe = new FakePingProbe(ip => new IPingProbe.Ipv4Reply(IPStatus.Success, 1, 64));
     fakeResolver.HangOnReverseDns = true;
     var scanner = new PingScanner(fakeResolver, fakeProbe);
     var rows = new List<ScanResult>();
     var settings = new ScanSettings { TimeoutMs = 2000, DnsTimeoutMs = 100 };
     await scanner.ScanAsync(new[] { "host.example" }, settings, new Progress<ScanResult>(rows.Add), CancellationToken.None);
     var row = Assert.Single(rows);
     // DNS timed out, so HostName should remain empty.
     Assert.True(row.Responding);
     Assert.Equal(string.Empty, row.HostName);
     Assert.True(fakeResolver.DnsCalled, "Reverse DNS should have been attempted");
 }

 [Fact]
 public async Task ScannerRespectsResolveRespondingOnlyFalse()
 {
     var fakeResolver = new FakeDnsResolver(["192.168.1.1"]);
     var fakeProbe = new FakePingProbe(ip => null);
     var scanner = new PingScanner(fakeResolver, fakeProbe);
     var results = new Dictionary<string, ScanResult>();
     var progress = new Progress<ScanResult>(r => results[r.Target] = r);
     await scanner.ScanAsync(new[] { "non-responding.example" }, new ScanSettings { TimeoutMs = 2000, ResolveRespondingOnly = false }, progress, CancellationToken.None);
     var row = Assert.Single(results.Values);
     Assert.False(row.Responding);
     // When ResolveRespondingOnly is false, even non-responding hosts get reverse DNS.
     Assert.Equal("host-192.168.1.1", row.HostName);
 }

 [Fact]
 public async Task ScannerPropagatesCancellationDuringDnsPhase()
 {
     var fakeResolver = new FakeDnsResolver(["127.0.0.1"]);
     var fakeProbe = new FakePingProbe(ip => new IPingProbe.Ipv4Reply(IPStatus.Success, 1, 64));
     fakeResolver.HangOnReverseDns = true;
     var scanner = new PingScanner(fakeResolver, fakeProbe);
     using var cts = new CancellationTokenSource();
     var task = scanner.ScanAsync(new[] { "host.example" }, new ScanSettings { TimeoutMs = 2000, DnsTimeoutMs = 5000 }, new Progress<ScanResult>(r => { }), cts.Token);
     await Task.Delay(50);
     cts.Cancel();
     await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await task);
 }

 [Fact]
 public void ScanSettingsValidatesDnsTimeoutBounds()
 {
     Assert.Throws<ArgumentOutOfRangeException>(() => new ScanSettings { DnsTimeoutMs = 0 }.Validate());
     Assert.Throws<ArgumentOutOfRangeException>(() => new ScanSettings { DnsTimeoutMs = 31000 }.Validate());
     new ScanSettings { DnsTimeoutMs = 1 }.Validate();
     new ScanSettings { DnsTimeoutMs = 30000 }.Validate();
 }

 private sealed class ImmediateProgress<T>(Action<T> action) : IProgress<T> { public void Report(T value) => action(value); }
 }

 /// <summary>
 /// Simple DNS resolver fake for testing PingScanner without network access.
 /// </summary>
 internal sealed class FakeDnsResolver(params string[] addresses) : IDnsResolver
 {
 public bool DnsCalled { get; set; }
 public bool HangOnReverseDns { get; set; }
 private readonly IPAddress[] _addresses = addresses.Select(IPAddress.Parse).ToArray();

 public ValueTask<IPAddress[]> ResolveAddressesAsync(string host, CancellationToken ct) =>
     new(_addresses);

 public async ValueTask<IPHostEntry> GetHostEntryAsync(IPAddress address, CancellationToken ct)
 {
     DnsCalled = true;
     if (HangOnReverseDns)
     {
         // Simulate a hanging DNS lookup that will be cancelled by the timeout.
         await Task.Delay(30000, ct);
         throw new OperationCanceledException(ct);
     }
     var entry = new IPHostEntry { HostName = $"host-{address}" };
     return entry;
 }
 }

 /// <summary>
 /// Simple ICMP probe fake for testing PingScanner deterministically.
 /// </summary>
 internal sealed class FakePingProbe(Func<IPAddress, IPingProbe.Ipv4Reply?> respond) : IPingProbe
 {
 public ValueTask<IPingProbe.Ipv4Reply?> SendAsync(IPAddress address, int timeoutMs, byte[] payload, PingOptions options, CancellationToken ct) =>
     new(respond(address));
 }

 internal sealed class SequencePingProbe(params IPingProbe.Ipv4Reply?[] replies) : IPingProbe
 {
 public int CallCount { get; private set; }
 public PingOptions? LastOptions { get; private set; }

 public ValueTask<IPingProbe.Ipv4Reply?> SendAsync(IPAddress address, int timeoutMs, byte[] payload, PingOptions options, CancellationToken ct)
 {
     LastOptions = options;
     var reply = replies[Math.Min(CallCount, replies.Length - 1)];
     CallCount++;
     return new(reply);
 }
 }
