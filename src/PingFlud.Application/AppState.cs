using System.Text.Json;
using System.Text;
using PingFlud.Core;

namespace PingFlud.Application;

public sealed class AppState
{
    internal const int MaximumRememberedTargetBytes = 16_384;
    public const string DefaultSubtitle = "Network reachability testing and troubleshooting";

    public ScanSettings Settings { get; set; } = new();
    public List<string> History { get; set; } = [];
    public string Title { get; set; } = "Ping Flud";
    public string Subtitle { get; set; } = DefaultSubtitle;
    public string ThemeName { get; set; } = AppearanceModes.DarkMode;

    public void Remember(string value)
    {
        value = value.Trim();
        if (value.Length == 0 || Encoding.UTF8.GetByteCount(value) > MaximumRememberedTargetBytes) return;
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
            var json = JsonSerializer.Serialize(state, JsonOptions);
            if (Encoding.UTF8.GetByteCount(json) > MaximumStateBytes)
                throw new InvalidOperationException("Application state exceeds the maximum supported size.");
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, _path, true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private static AppState Normalize(AppState state)
    {
        var settings = state.Settings ?? new ScanSettings();
        state.Settings = new ScanSettings
        {
            MaxOutstanding = Bounded(settings.MaxOutstanding, 1, 1024, 64),
            TimeoutMs = Bounded(settings.TimeoutMs, 1, 120_000, 1000),
            PingsPerNode = Bounded(settings.PingsPerNode, 1, 10, 1),
            Ttl = Bounded(settings.Ttl, 1, 255, 128),
            DelayMs = Bounded(settings.DelayMs, 0, 60_000, 0),
            Payload = ValidPayload(settings.Payload),
            ExpansionCap = Bounded(settings.ExpansionCap, 1, 1_000_000, 65_536),
            DnsTimeoutMs = Bounded(settings.DnsTimeoutMs, 1, 30_000, 2000),
            DontFragment = settings.DontFragment,
            ResolveRespondingOnly = settings.ResolveRespondingOnly
        };
        state.Title = string.IsNullOrWhiteSpace(state.Title) ? "Ping Flud" : state.Title.Trim()[..Math.Min(120, state.Title.Trim().Length)];
        state.Subtitle = (state.Subtitle ?? string.Empty).Trim();
        if (state.Subtitle.Length > 240) state.Subtitle = state.Subtitle[..240];
        if (state.Subtitle.Equals("Fast, transparent network reachability checks", StringComparison.OrdinalIgnoreCase))
            state.Subtitle = AppState.DefaultSubtitle;
        state.ThemeName = AppearanceModes.NormalizeName(state.ThemeName);
        state.History = (state.History ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Where(value => Encoding.UTF8.GetByteCount(value) <= AppState.MaximumRememberedTargetBytes)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToList();
        return state;
    }

    private static int Bounded(int value, int minimum, int maximum, int fallback) =>
        value >= minimum && value <= maximum ? value : fallback;

    private static string ValidPayload(string? value)
    {
        value ??= "Ping Flud";
        return Encoding.UTF8.GetByteCount(value) <= 60_000 ? value : "Ping Flud";
    }
}
