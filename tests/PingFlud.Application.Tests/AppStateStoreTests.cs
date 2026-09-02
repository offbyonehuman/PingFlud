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
                ThemeName = "Daylight",
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

    [Fact]
    public void RememberSkipsOversizedTargetLists()
    {
        var state = new AppState();

        state.Remember(new string('1', 16_385));

        Assert.Empty(state.History);
    }

    [Fact]
    public void SaveAndLoadDropsHistoryEntriesOverByteLimit()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pingflud-state-limit-{Guid.NewGuid():N}.json");
        try
        {
            var state = new AppState
            {
                History = [new string('é', 16_384)]
            };

            var store = new JsonAppStateStore(path);
            store.Save(state);

            Assert.Empty(store.Load().History);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void LoadKeepsValidStateWhenOneSettingIsInvalid()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pingflud-state-invalid-setting-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, "{\"Settings\":{\"TimeoutMs\":0,\"PingsPerNode\":2},\"Title\":\"Keep this\",\"ThemeName\":\"Daylight\",\"History\":[\"localhost\"]}");

            var state = new JsonAppStateStore(path).Load();

            Assert.Equal("Keep this", state.Title);
            Assert.Equal("Daylight", state.ThemeName);
            Assert.Equal(["localhost"], state.History);
            Assert.Equal(1000, state.Settings.TimeoutMs);
            Assert.Equal(2, state.Settings.PingsPerNode);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Theory]
    [InlineData("Midnight")]
    [InlineData("Nebula")]
    public void LoadMigratesRemovedThemesToGraphite(string removedTheme)
    {
        var path = Path.Combine(Path.GetTempPath(), $"pingflud-state-theme-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, $"{{\"ThemeName\":\"{removedTheme}\"}}");

            var state = new JsonAppStateStore(path).Load();

            Assert.Equal("Graphite", state.ThemeName);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void LoadMigratesTheOldDefaultSubtitleToNetworkDiagnosticsWording()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pingflud-state-subtitle-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, "{\"Subtitle\":\"Fast, transparent network reachability checks\"}");

            var state = new JsonAppStateStore(path).Load();

            Assert.Equal(AppState.DefaultSubtitle, state.Subtitle);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
