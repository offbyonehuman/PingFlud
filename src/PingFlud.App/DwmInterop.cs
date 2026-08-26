using System.Runtime.InteropServices;

namespace PingFlud.App;

/// <summary>
/// Windows 11 DWM interop: rounded window corners, Mica/Acrylic backdrop,
/// and dark-mode title bar. All calls degrade gracefully on Windows 10
/// (unsupported attributes simply return an error code that we ignore).
/// </summary>
internal static class DwmInterop
{
    // DWMWINDOWATTRIBUTE values
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;       // Win10 1809+/Win11
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_PRE_20H1 = 19;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;      // Win11+
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;           // Win11 22H2+

    // DWM_WINDOW_CORNER_PREFERENCE values
    private const int DWMWCP_ROUND = 2;          // ~8px rounded corners

    // DWMSBT (system backdrop) values
    private const int DWMSBT_MICA = 2;           // Mica material

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);


    /// <summary>
    /// Applies Windows 11 styling to a window: rounded corners + optional
    /// Mica backdrop + dark/light title bar. Safe to call on any Windows version.
    /// </summary>
    public static void ApplyWindowStyling(IWin32Window window, bool isDark)
    {
        ArgumentNullException.ThrowIfNull(window);
        var hwnd = window.Handle;

        // 1. Rounded corners (Windows 11+). Ignored on older systems.
        var preference = DWMWCP_ROUND;
        _ = DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));

        // 2. Dark or light title bar (Windows 10 1809+).
        SetDarkMode(window, isDark);

        // 3. Mica backdrop (Windows 11 22H2+). Requires the form to paint
        //    its background with a transparent-ish brush over extended frame.
        var backdrop = DWMSBT_MICA;
        _ = DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));
    }


    public static void SetDarkMode(IWin32Window window, bool isDark)
    {
        var hwnd = window.Handle;
        // Try the modern attribute first (20), fall back to pre-20H1 (19).
        var dark = isDark ? 1 : 0;
        if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int)) != 0)
        {
            _ = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_PRE_20H1, ref dark, sizeof(int));
        }
    }

}
