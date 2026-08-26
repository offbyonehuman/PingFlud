namespace PingFlud.App;

using System.Drawing;

/// <summary>
/// Windows 11 Fluent Design theme palettes.
/// All control-background colors are fully opaque (alpha=255) to comply with
/// WinForms Control.BackColor requirements. Semi-transparency is used only
/// in custom-drawn surfaces (CardPanel gradients, button hover overlays) 
/// and alpha-channel-based selection colors.
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
    /// <summary>
    /// Helper: converts an ARGB color to fully opaque for use as BackColor.
    /// </summary>
    private static Color Solid(int r, int g, int b) => Color.FromArgb(255, r, g, b);

    public static IReadOnlyList<ThemePalette> All { get; } =
    [
        // Graphite — neutral Windows 11 black-and-grey default
        new(
            "Graphite",
            Solid(18, 18, 18),       // WindowBackground  #121212
            Solid(30, 30, 30),       // Surface           #1E1E1E
            Solid(42, 42, 42),       // SurfaceRaised     #2A2A2A
            Solid(23, 23, 23),       // Header            #171717
            Solid(245, 245, 245),    // Foreground        #F5F5F5
            Solid(180, 180, 180),    // MutedForeground   #B4B4B4
            Solid(200, 200, 200),    // Accent            #C8C8C8
            Solid(18, 18, 18),       // AccentForeground
            Solid(70, 70, 70),       // Border            #464646
            Solid(24, 24, 24),       // GridAlternate     #181818
            Solid(68, 68, 68),       // Selection         #444444
            Solid(86, 205, 137),     // Success
            Solid(255, 145, 155),    // Danger
            true),

        // Midnight — dark navy-slate dashboard (reference-matched)
        new(
            "Midnight",
            Solid(26, 32, 48),       // WindowBackground  #1A2030
            Solid(35, 39, 51),       // Surface           #232733
            Solid(46, 49, 55),       // SurfaceRaised     #2E3137
            Solid(30, 36, 56),       // Header            #1E2438
            Solid(240, 240, 240),    // Foreground        #F0F0F0
            Solid(192, 192, 192),    // MutedForeground   #C0C0C0
            Solid(64, 128, 192),     // Accent            #4080C0 azure blue
            Color.White,             // AccentForeground
            Solid(72, 78, 96),       // Border
            Solid(31, 34, 44),       // GridAlternate     #1F222C
            Solid(64, 110, 180),     // Selection         #406EB4
            Solid(75, 211, 139),     // Success
            Solid(248, 105, 115),    // Danger
            true),

        // Nebula — violet-shifted variant of the reference
        new(
            "Nebula",
            Solid(28, 27, 43),
            Solid(38, 37, 56),
            Solid(50, 48, 68),
            Solid(33, 32, 52),
            Solid(242, 240, 250),
            Solid(190, 186, 205),
            Solid(128, 96, 192),     // Accent #8060C0 violet
            Color.White,
            Solid(80, 76, 104),
            Solid(33, 32, 45),
            Solid(120, 100, 190),
            Solid(90, 220, 160),
            Solid(248, 105, 135),
            true),

        // Daylight — Windows 11 light mode
        new(
            "Daylight",
            Solid(244, 248, 252),
            Solid(249, 250, 252),
            Color.White,
            Solid(238, 245, 252),
            Solid(32, 32, 32),
            Solid(97, 97, 102),
            Solid(0, 120, 212),
            Color.White,
            Solid(200, 200, 205),
            Solid(244, 244, 245),
            Solid(215, 235, 255),
            Solid(35, 150, 90),
            Solid(205, 55, 55),
            false)
    ];

    public static ThemePalette Get(string? name) =>
        All.FirstOrDefault(theme => theme.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? All[0];
}
