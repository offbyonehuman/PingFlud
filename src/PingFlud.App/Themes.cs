namespace PingFlud.App;

using System.Drawing;
using PingFlud.Application;

/// <summary>
/// Windows 11 Fluent Design theme palettes.
/// All control-background colors are fully opaque (alpha=255) to comply with
/// WinForms Control.BackColor requirements. Custom controls also use opaque
/// fills to avoid repaint artifacts during hover and theme changes.
/// </summary>
public sealed record ThemePalette(
    string Name,
    Color WindowBackground,
    Color Surface,           // Layer 1 — base elevated surface
    Color SurfaceRaised,     // Layer 2 — cards, elevated surfaces
    Color Header,            // App header bar
    Color Foreground,
    Color MutedForeground,
    Color Accent,
    Color AccentForeground,
    Color Border,
    Color GridAlternate,
    Color Selection,
    Color Success,
    Color Danger,
    bool IsDark);

public static class ThemeCatalog
{
    public static IReadOnlyList<ThemePalette> All { get; } =
        AppearanceModes.All.Select(mode => new ThemePalette(
            mode.Name,
            ToColor(mode.WindowBackground),
            ToColor(mode.Surface),
            ToColor(mode.SurfaceRaised),
            ToColor(mode.Header),
            ToColor(mode.Foreground),
            ToColor(mode.MutedForeground),
            ToColor(mode.Accent),
            ToColor(mode.AccentForeground),
            ToColor(mode.Border),
            ToColor(mode.GridAlternate),
            ToColor(mode.Selection),
            ToColor(mode.Success),
            ToColor(mode.Danger),
            mode.IsDark)).ToArray();

    private static Color ToColor(uint argb) => Color.FromArgb(unchecked((int)argb));

    public static ThemePalette Get(string? name) =>
        All.FirstOrDefault(theme => theme.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? All[0];
}
