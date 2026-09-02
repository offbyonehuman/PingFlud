namespace PingFlud.Application;

/// <summary>
/// The two supported appearance modes and their shared color definitions.
/// UI shells convert these ARGB values to their native color types.
/// </summary>
public sealed record AppearancePalette(
    string Name,
    uint WindowBackground,
    uint Surface,
    uint SurfaceRaised,
    uint Header,
    uint Foreground,
    uint MutedForeground,
    uint Accent,
    uint AccentForeground,
    uint Border,
    uint GridAlternate,
    uint Selection,
    uint Success,
    uint Danger,
    bool IsDark);

public static class AppearanceModes
{
    public const string DarkMode = "Graphite";
    public const string LightMode = "Daylight";

    public static IReadOnlyList<AppearancePalette> All { get; } =
    [
        new(
            DarkMode,
            0xFF121212, // WindowBackground
            0xFF1E1E1E, // Surface
            0xFF2A2A2A, // SurfaceRaised
            0xFF171717, // Header
            0xFFF5F5F5, // Foreground
            0xFFB4B4B4, // MutedForeground
            0xFFF59E0B, // Accent
            0xFF121212, // AccentForeground
            0xFF464646, // Border
            0xFF181818, // GridAlternate
            0xFF5A3A14, // Selection
            0xFF56CD89, // Success
            0xFFFF919B, // Danger
            true),
        new(
            LightMode,
            0xFFF4F8FC, // WindowBackground
            0xFFF9FAFC, // Surface
            0xFFFFFFFF, // SurfaceRaised
            0xFFEEF5FC, // Header
            0xFF202020, // Foreground
            0xFF616166, // MutedForeground
            0xFFC45F00, // Accent
            0xFFFFFFFF, // AccentForeground
            0xFFC8C8CD, // Border
            0xFFF4F4F5, // GridAlternate
            0xFFFFE3BD, // Selection
            0xFF23965A, // Success
            0xFFCD3737, // Danger
            false)
    ];

    public static AppearancePalette Get(string? name) =>
        All.FirstOrDefault(mode => mode.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? All[0];

    public static string NormalizeName(string? name) => Get(name).Name;
}
