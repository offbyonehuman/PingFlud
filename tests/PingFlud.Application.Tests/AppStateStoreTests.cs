using PingFlud.Application;
using PingFlud.Core;
using Xunit;

namespace PingFlud.Application.Tests;

public sealed class AppStateStoreTests
{
    [Fact]
    public void MalformedStateFallsBackToGraphiteDefaults()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pingflud-state-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "{not-json");
        try
        {
            var state = new JsonAppStateStore(path).Load();

            Assert.Equal("Graphite", state.ThemeName);
            Assert.Equal("Ping Flud", state.Title);
            Assert.Equal(64, state.Settings.MaxOutstanding);
            Assert.Empty(state.History);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void SaveRoundTripsExistingStateShape()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pingflud-state-{Guid.NewGuid():N}.json");
        try
        {
            var store = new JsonAppStateStore(path);
            var expected = new AppState
            {
                ThemeName = "Nebula",
                Title = "Lab scanner",
                Subtitle = "Authorized segment",
                History = ["10.0.0.0/24"],
                Settings = new ScanSettings { TimeoutMs = 2500, PingsPerNode = 2 }
            };

            store.Save(expected);
            var actual = store.Load();

            Assert.Equal(expected.ThemeName, actual.ThemeName);
            Assert.Equal(expected.Title, actual.Title);
            Assert.Equal(expected.Subtitle, actual.Subtitle);
            Assert.Equal(expected.History, actual.History);
            Assert.Equal(2500, actual.Settings.TimeoutMs);
            Assert.Equal(2, actual.Settings.PingsPerNode);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
