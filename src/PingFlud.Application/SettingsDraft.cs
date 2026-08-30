using PingFlud.Core;

namespace PingFlud.Application;

public sealed class SettingsDraft
{
    public int MaxOutstanding { get; set; }
    public int TimeoutMs { get; set; }
    public int PingsPerNode { get; set; }
    public int Ttl { get; set; }
    public int DelayMs { get; set; }
    public string Payload { get; set; } = string.Empty;
    public int ExpansionCap { get; set; }
    public int DnsTimeoutMs { get; set; }
    public bool DontFragment { get; set; }
    public bool ResolveRespondingOnly { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string ThemeName { get; set; } = "Graphite";

    public static SettingsDraft From(AppState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var settings = state.Settings;
        return new SettingsDraft
        {
            MaxOutstanding = settings.MaxOutstanding,
            TimeoutMs = settings.TimeoutMs,
            PingsPerNode = settings.PingsPerNode,
            Ttl = settings.Ttl,
            DelayMs = settings.DelayMs,
            Payload = settings.Payload,
            ExpansionCap = settings.ExpansionCap,
            DnsTimeoutMs = settings.DnsTimeoutMs,
            DontFragment = settings.DontFragment,
            ResolveRespondingOnly = settings.ResolveRespondingOnly,
            Title = state.Title,
            Subtitle = state.Subtitle,
            ThemeName = state.ThemeName
        };
    }

    public bool TryApply(AppState state, out string error)
    {
        ArgumentNullException.ThrowIfNull(state);
        try
        {
            var title = string.IsNullOrWhiteSpace(Title) ? "Ping Flud" : Title.Trim();
            var subtitle = (Subtitle ?? string.Empty).Trim();
            if (title.Length > 120) throw new ArgumentOutOfRangeException(nameof(Title), "Window title exceeds 120 characters.");
            if (subtitle.Length > 240) throw new ArgumentOutOfRangeException(nameof(Subtitle), "Subtitle exceeds 240 characters.");
            if (ThemeName is not ("Graphite" or "Midnight" or "Nebula" or "Daylight"))
                throw new ArgumentOutOfRangeException(nameof(ThemeName), "Unsupported theme.");

            var candidate = new ScanSettings
            {
                MaxOutstanding = MaxOutstanding,
                TimeoutMs = TimeoutMs,
                PingsPerNode = PingsPerNode,
                Ttl = Ttl,
                DelayMs = DelayMs,
                Payload = Payload ?? string.Empty,
                ExpansionCap = ExpansionCap,
                DnsTimeoutMs = DnsTimeoutMs,
                DontFragment = DontFragment,
                ResolveRespondingOnly = ResolveRespondingOnly
            };
            candidate.Validate();

            state.Settings = candidate;
            state.Title = title;
            state.Subtitle = subtitle;
            state.ThemeName = ThemeName;
            error = string.Empty;
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            error = ex.Message;
            return false;
        }
    }
}
