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
            Color.FromArgb(12, 24, 30), Color.FromArgb(18, 35, 43), Color.FromArgb(31, 50, 59),
            Color.FromArgb(29, 44, 51), Color.FromArgb(240, 245, 247), Color.FromArgb(148, 165, 173),
            Color.FromArgb(20, 157, 178), Color.White, Color.FromArgb(49, 71, 80),
            Color.FromArgb(15, 30, 37), Color.FromArgb(42, 77, 88), Color.FromArgb(69, 211, 139),
            Color.FromArgb(238, 92, 103), true),
        new(
            "Graphite",
            Color.FromArgb(27, 29, 33), Color.FromArgb(38, 41, 46), Color.FromArgb(49, 53, 59),
            Color.FromArgb(31, 34, 39), Color.FromArgb(242, 243, 245), Color.FromArgb(174, 180, 188),
            Color.FromArgb(71, 184, 255), Color.FromArgb(9, 31, 45), Color.FromArgb(75, 80, 88),
            Color.FromArgb(32, 35, 40), Color.FromArgb(55, 76, 91), Color.FromArgb(73, 209, 147),
            Color.FromArgb(255, 112, 122), true),
        new(
            "Oceanic",
            Color.FromArgb(7, 24, 39), Color.FromArgb(13, 39, 58), Color.FromArgb(24, 57, 79),
            Color.FromArgb(10, 31, 47), Color.FromArgb(235, 247, 255), Color.FromArgb(143, 174, 195),
            Color.FromArgb(45, 181, 222), Color.FromArgb(4, 30, 42), Color.FromArgb(43, 76, 98),
            Color.FromArgb(10, 31, 47), Color.FromArgb(31, 83, 112), Color.FromArgb(75, 213, 153),
            Color.FromArgb(248, 104, 113), true),
        new(
            "Forest",
            Color.FromArgb(12, 29, 24), Color.FromArgb(20, 45, 37), Color.FromArgb(31, 62, 51),
            Color.FromArgb(17, 39, 32), Color.FromArgb(237, 248, 243), Color.FromArgb(147, 177, 163),
            Color.FromArgb(61, 193, 128), Color.FromArgb(5, 40, 24), Color.FromArgb(48, 82, 68),
            Color.FromArgb(16, 38, 31), Color.FromArgb(39, 92, 69), Color.FromArgb(77, 215, 145),
            Color.FromArgb(240, 104, 110), true),
        new(
            "Amethyst",
            Color.FromArgb(24, 18, 36), Color.FromArgb(38, 29, 54), Color.FromArgb(57, 43, 76),
            Color.FromArgb(31, 23, 45), Color.FromArgb(246, 241, 252), Color.FromArgb(179, 160, 196),
            Color.FromArgb(169, 116, 229), Color.White, Color.FromArgb(77, 59, 98),
            Color.FromArgb(31, 24, 44), Color.FromArgb(84, 61, 112), Color.FromArgb(78, 211, 151),
            Color.FromArgb(247, 104, 129), true),
        new(
            "Ember",
            Color.FromArgb(35, 23, 18), Color.FromArgb(52, 34, 27), Color.FromArgb(72, 47, 36),
            Color.FromArgb(45, 29, 23), Color.FromArgb(250, 243, 237), Color.FromArgb(190, 164, 145),
            Color.FromArgb(239, 145, 75), Color.FromArgb(43, 22, 8), Color.FromArgb(92, 64, 48),
            Color.FromArgb(43, 28, 22), Color.FromArgb(105, 67, 45), Color.FromArgb(86, 205, 141),
            Color.FromArgb(238, 92, 98), true),
        new(
            "Daylight",
            Color.FromArgb(242, 246, 250), Color.White, Color.FromArgb(249, 251, 253),
            Color.FromArgb(232, 239, 246), Color.FromArgb(27, 39, 51), Color.FromArgb(91, 108, 124),
            Color.FromArgb(0, 120, 212), Color.White, Color.FromArgb(202, 213, 224),
            Color.FromArgb(245, 248, 251), Color.FromArgb(207, 231, 250), Color.FromArgb(25, 135, 84),
            Color.FromArgb(197, 48, 48), false)
    ];

    public static ThemePalette Get(string? name) =>
        All.FirstOrDefault(theme => theme.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? All[0];
}
