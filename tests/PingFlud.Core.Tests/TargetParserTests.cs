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
 private sealed class ImmediateProgress<T>(Action<T> action):IProgress<T>{public void Report(T value)=>action(value);}
}
