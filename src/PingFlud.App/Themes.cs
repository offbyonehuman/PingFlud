namespace PingFlud.App;

public sealed record ThemePalette(
    string Name,
    Color WindowBackground,
    Color Surface,
    Color SurfaceRaised,
    Color Header,
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
    [
        new(
            "Midnight",
            Color.FromArgb(15, 28, 35), Color.FromArgb(22, 42, 52), Color.FromArgb(36, 58, 72),
            Color.FromArgb(34, 52, 64), Color.FromArgb(245, 250, 252), Color.FromArgb(160, 180, 195),
            Color.FromArgb(20, 175, 200), Color.White, Color.FromArgb(65, 85, 98),
            Color.FromArgb(18, 38, 47), Color.FromArgb(52, 88, 100), Color.FromArgb(75, 211, 139),
            Color.FromArgb(248, 105, 115), true),
        new(
            "Graphite",
            Color.FromArgb(25, 29, 34), Color.FromArgb(42, 46, 52), Color.FromArgb(56, 62, 70),
            Color.FromArgb(35, 39, 45), Color.FromArgb(245, 247, 250), Color.FromArgb(175, 182, 192),
            Color.FromArgb(80, 190, 255), Color.FromArgb(15, 35, 55), Color.FromArgb(85, 92, 102),
            Color.FromArgb(38, 44, 52), Color.FromArgb(65, 85, 100), Color.FromArgb(85, 215, 150),
            Color.FromArgb(255, 115, 125), true),
        new(
            "Oceanic",
            Color.FromArgb(8, 28, 45), Color.FromArgb(16, 48, 72), Color.FromArgb(28, 65, 98),
            Color.FromArgb(14, 40, 58), Color.FromArgb(238, 250, 255), Color.FromArgb(150, 185, 210),
            Color.FromArgb(55, 195, 235), Color.FromArgb(10, 35, 50), Color.FromArgb(52, 90, 120),
            Color.FromArgb(15, 35, 55), Color.FromArgb(38, 95, 125), Color.FromArgb(85, 220, 165),
            Color.FromArgb(250, 110, 120), true),
        new(
            "Forest",
            Color.FromArgb(14, 35, 28), Color.FromArgb(24, 52, 42), Color.FromArgb(36, 70, 58),
            Color.FromArgb(20, 48, 38), Color.FromArgb(240, 252, 248), Color.FromArgb(155, 190, 175),
            Color.FromArgb(70, 205, 145), Color.FromArgb(8, 42, 24), Color.FromArgb(58, 95, 78),
            Color.FromArgb(18, 42, 34), Color.FromArgb(48, 100, 78), Color.FromArgb(90, 220, 155),
            Color.FromArgb(248, 115, 120), true),
        new(
            "Amethyst",
            Color.FromArgb(28, 20, 42), Color.FromArgb(44, 32, 60), Color.FromArgb(62, 48, 85),
            Color.FromArgb(34, 24, 50), Color.FromArgb(248, 245, 255), Color.FromArgb(185, 170, 205),
            Color.FromArgb(180, 130, 240), Color.FromArgb(15, 32, 48), Color.FromArgb(88, 68, 110),
            Color.FromArgb(38, 26, 50), Color.FromArgb(95, 70, 120), Color.FromArgb(90, 220, 160),
            Color.FromArgb(248, 115, 135), true),
        new(
            "Ember",
            Color.FromArgb(40, 25, 18), Color.FromArgb(58, 36, 26), Color.FromArgb(78, 50, 36),
            Color.FromArgb(50, 32, 22), Color.FromArgb(252, 245, 240), Color.FromArgb(195, 170, 150),
            Color.FromArgb(245, 155, 85), Color.FromArgb(48, 24, 8), Color.FromArgb(100, 70, 50),
            Color.FromArgb(50, 30, 20), Color.FromArgb(115, 75, 50), Color.FromArgb(95, 215, 150),
            Color.FromArgb(248, 105, 110), true),
        new(
            "Daylight",
            Color.FromArgb(244, 248, 252), Color.White, Color.FromArgb(250, 253, 255),
            Color.FromArgb(238, 245, 252), Color.FromArgb(25, 42, 60), Color.FromArgb(95, 112, 130),
            Color.FromArgb(0, 130, 225), Color.White, Color.FromArgb(210, 225, 240),
            Color.FromArgb(248, 250, 253), Color.FromArgb(215, 235, 255), Color.FromArgb(35, 150, 90),
            Color.FromArgb(205, 55, 55), false)
    ];

    public static ThemePalette Get(string? name) =>
        All.FirstOrDefault(theme => theme.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? All[0];
}
