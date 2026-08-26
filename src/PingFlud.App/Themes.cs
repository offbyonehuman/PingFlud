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

        // Graphite — Windows 11 dark mode
        new(
            "Graphite",
            Solid(25, 29, 34),
            Solid(28, 32, 36),
            Solid(42, 47, 52),
            Solid(42, 47, 52),
            Solid(247, 248, 250),
            Solid(173, 181, 192),
            Solid(55, 159, 226),
            Color.White,
            Solid(70, 78, 88),
            Solid(38, 44, 52),
            Solid(65, 85, 100),
            Solid(85, 215, 150),
            Solid(255, 115, 125),
            true),

        // Oceanic — Windows 11 dark blue
        new(
            "Oceanic",
            Solid(8, 28, 45),
            Solid(20, 34, 48),
            Solid(32, 50, 68),
            Solid(32, 50, 68),
            Solid(247, 248, 250),
            Solid(173, 181, 192),
            Solid(55, 195, 235),
            Solid(10, 35, 50),
            Solid(80, 90, 100),
            Solid(26, 40, 54),
            Solid(38, 95, 125),
            Solid(85, 220, 165),
            Solid(250, 110, 120),
            true),

        // Forest — Windows 11 green accent
        new(
            "Forest",
            Solid(14, 35, 28),
            Solid(22, 38, 32),
            Solid(34, 52, 44),
            Solid(34, 52, 44),
            Solid(247, 248, 250),
            Solid(173, 181, 192),
            Solid(70, 205, 145),
            Solid(8, 42, 24),
            Solid(85, 100, 88),
            Solid(26, 42, 34),
            Solid(48, 100, 78),
            Solid(90, 220, 155),
            Solid(248, 115, 120),
            true),

        // Amethyst — Windows 11 purple
        new(
            "Amethyst",
            Solid(28, 20, 42),
            Solid(36, 28, 40),
            Solid(48, 40, 54),
            Solid(48, 40, 54),
            Solid(247, 248, 250),
            Solid(173, 181, 192),
            Solid(180, 130, 240),
            Color.White,
            Solid(90, 70, 110),
            Solid(38, 26, 50),
            Solid(100, 80, 130),
            Solid(90, 220, 160),
            Solid(248, 115, 135),
            true),

        // Ember — Windows 11 warm
        new(
            "Ember",
            Solid(40, 25, 18),
            Solid(50, 32, 24),
            Solid(66, 46, 32),
            Solid(66, 46, 32),
            Solid(252, 245, 240),
            Solid(195, 170, 150),
            Solid(245, 155, 85),
            Solid(48, 24, 8),
            Solid(100, 70, 50),
            Solid(42, 28, 21),
            Solid(115, 75, 50),
            Solid(95, 215, 150),
            Solid(248, 105, 110),
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
