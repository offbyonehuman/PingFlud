using System.Text.Json;
using PingFlud.Core;

namespace PingFlud.Application;

public sealed class AppState
{
    public ScanSettings Settings { get; set; } = new();
    public List<string> History { get; set; } = [];
    public string Title { get; set; } = "Ping Flud";
    public string Subtitle { get; set; } = "Fast, transparent network reachability checks";
    public string ThemeName { get; set; } = "Graphite";

    public void Remember(string value)
    {
        value = value.Trim();
        if (value.Length == 0) return;
        History.RemoveAll(existing => existing.Equals(value, StringComparison.OrdinalIgnoreCase));
        History.Insert(0, value);
        if (History.Count > 20) History.RemoveRange(20, History.Count - 20);
    }
}

public interface IAppStateStore
{
    AppState Load();
    void Save(AppState state);
}

public sealed class JsonAppStateStore : IAppStateStore
{
    private const long MaximumStateBytes = 1024 * 1024;
    private static readonly HashSet<string> SupportedThemes =
        new(["Graphite", "Midnight", "Nebula", "Daylight"], StringComparer.OrdinalIgnoreCase);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string _path;

    public JsonAppStateStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PingFlud",
            "settings.json");
    }

    public AppState Load()
    {
        try
        {
            var file = new FileInfo(_path);
            if (!file.Exists || file.Length > MaximumStateBytes) return new AppState();

            var state = JsonSerializer.Deserialize<AppState>(File.ReadAllText(_path), JsonOptions);
            return state is null ? new AppState() : Normalize(state);
        }
        catch
        {
            return new AppState();
        }
    }

    public void Save(AppState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        state = Normalize(state);

        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        var temporaryPath = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(state, JsonOptions));
            File.Move(temporaryPath, _path, true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private static AppState Normalize(AppState state)
    {
        state.Settings ??= new ScanSettings();
        state.Settings.Validate();
        state.Title = string.IsNullOrWhiteSpace(state.Title) ? "Ping Flud" : state.Title.Trim()[..Math.Min(120, state.Title.Trim().Length)];
        state.Subtitle = (state.Subtitle ?? string.Empty).Trim();
        if (state.Subtitle.Length > 240) state.Subtitle = state.Subtitle[..240];
        state.ThemeName = SupportedThemes.Contains(state.ThemeName ?? string.Empty)
            ? SupportedThemes.First(theme => theme.Equals(state.ThemeName, StringComparison.OrdinalIgnoreCase))
            : "Graphite";
        state.History = (state.History ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToList();
        return state;
    }
}
