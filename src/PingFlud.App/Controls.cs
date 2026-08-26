using System.Drawing;
using System.Drawing.Drawing2D;

namespace PingFlud.App;

internal static class UiGeometry
{
    public static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        if (radius <= 1)
        {
            path.AddRectangle(bounds);
            path.CloseFigure();
            return path;
        }
        var diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class RoundedButton : Button
{
    private bool _hovered;
    private bool _pressed;

    public int CornerRadius { get; set; } = 6; // Windows 11 control corner radius
    public bool IsPrimary { get; set; } = false; // Accent-filled button

    public RoundedButton()
    {
        FlatStyle = FlatStyle.Flat;
        TextAlign = ContentAlignment.MiddleCenter;
        UseVisualStyleBackColor = false;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected override bool ShowFocusCues => true;

    /// <summary>
    /// Exposes whether focus cues are shown, for accessibility verification and testing.
    /// </summary>
    public bool FocusCuesVisible => ShowFocusCues;

    public override Size GetPreferredSize(Size proposedSize)
    {
        var measured = TextRenderer.MeasureText(Text, Font);
        return new Size(Math.Max(76, measured.Width + Padding.Horizontal + 22), Math.Max(34, measured.Height + 14));
    }

    protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hovered = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) _pressed = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

    // Modern Windows 11 style button with gradient fill, layered shadow, and accent support
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = UiGeometry.RoundedRectangle(bounds, CornerRadius);

        // Determine fill, text color, and border based on state and role
        Color fill, textColor, borderColor;

        if (!Enabled)
        {
            fill = BackColor;
            textColor = ForeColor;
            borderColor = ControlPaint.Light(ForeColor, 0.1F);
        }
        else if (IsPrimary)
        {
            // Primary button: solid accent fill with hover/press states
            var accent = FindAccentColor();
            fill = _pressed
                ? ControlPaint.Dark(accent, 0.1F)
                : _hovered
                    ? ControlPaint.Light(accent, 0.05F)
                    : accent;
            textColor = GetContrastColor(accent);
            borderColor = fill;

            // Draw Fluent shadow for elevated (accent) buttons
            FluentHelpers.DrawFluentShadow(e.Graphics, bounds, CornerRadius);
        }
        else
        {
            // Secondary button: subtle hover overlay over its own BackColor.
            // NOTE: never use Color.Transparent here — WinForms fake transparency
            // re-paints the parent's background (a gradient on CardPanel) at the
            // wrong offset, producing ghost/duplicated text artifacts.
            fill = _hovered && _pressed
                ? Color.FromArgb(45, ForeColor.R, ForeColor.G, ForeColor.B)
                : _hovered
                    ? Color.FromArgb(25, ForeColor.R, ForeColor.G, ForeColor.B)
                    : BackColor;
            textColor = ForeColor;
            borderColor = _hovered
                ? Color.FromArgb(100, ForeColor.R, ForeColor.G, ForeColor.B)
                : Color.FromArgb(50, ForeColor.R, ForeColor.G, ForeColor.B);
        }

        // Draw button surface with subtle gradient
        var gradientEnd = fill == BackColor ? fill : ControlPaint.Light(fill, 0.04F);
        using var brush = new LinearGradientBrush(bounds, gradientEnd, fill, LinearGradientMode.Vertical);
        e.Graphics.FillPath(brush, path);

        // Draw border (1px)
        using var pen = new Pen(borderColor, 1f);
        e.Graphics.DrawPath(pen, path);

        // Draw text
        var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
        flags |= TextAlign switch
        {
            ContentAlignment.MiddleLeft => TextFormatFlags.Left,
            ContentAlignment.MiddleRight => TextFormatFlags.Right,
            _ => TextFormatFlags.HorizontalCenter
        };
        var textBounds = TextAlign == ContentAlignment.MiddleLeft
            ? new Rectangle(Padding.Left + 8, 0, Math.Max(1, Width - Padding.Left - Padding.Right - 12), Height)
            : Rectangle.Inflate(ClientRectangle, -4, 0);
        TextRenderer.DrawText(e.Graphics, Text, Font, textBounds, textColor, flags);

        // Draw focus rectangle if focused
        if (Focused && ShowFocusCues)
            ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(bounds, -4, -4), textColor, fill);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        using var path = UiGeometry.RoundedRectangle(
            new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1)), CornerRadius);
        Region = new Region(path);
    }

    private ThemePalette? FindThemePalette()
    {
        var form = FindForm();
        return form?.Tag as ThemePalette;
    }

    private Color FindAccentColor()
    {
        var palette = FindThemePalette();
        return palette?.Accent ?? Color.FromArgb(0, 120, 212);
    }

    private static Color GetContrastColor(Color bg)
    {
        var luminance = 0.299 * bg.R + 0.587 * bg.G + 0.114 * bg.B;
        return luminance > 128 ? Color.Black : Color.White;
    }
}

/// <summary>
/// Fluent Design shadow and theme helpers for Windows 11 styling.
/// </summary>
internal static class FluentHelpers
{
    public static void DrawFluentShadow(Graphics g, Rectangle bounds, int cornerRadius)
    {
        // Layer 1: soft outer shadow (12% opacity, offset 4px down)
        using var path1 = UiGeometry.RoundedRectangle(
            new Rectangle(bounds.X, bounds.Y + 4, bounds.Width + 2, bounds.Height + 2), cornerRadius);
        using var brush1 = new SolidBrush(Color.FromArgb(12, 0, 0, 0));
        g.FillPath(brush1, path1);

        // Layer 2: medium shadow (8% opacity, offset 3px down)
        using var path2 = UiGeometry.RoundedRectangle(
            new Rectangle(bounds.X + 1, bounds.Y + 3, bounds.Width, bounds.Height), cornerRadius);
        using var brush2 = new SolidBrush(Color.FromArgb(8, 0, 0, 0));
        g.FillPath(brush2, path2);

        // Layer 3: inner shadow (4% opacity, offset 2px down)
        using var path3 = UiGeometry.RoundedRectangle(
            new Rectangle(bounds.X + 2, bounds.Y + 2, bounds.Width - 2, bounds.Height - 2), cornerRadius);
        using var brush3 = new SolidBrush(Color.FromArgb(4, 0, 0, 0));
        g.FillPath(brush3, path3);
    }
}

internal sealed class RoundedNumericUpDown : NumericUpDown
{
    // NumericUpDown is a native composite control; keep native painting so its
    // hosted edit and spin-button windows receive the active palette correctly.
    public RoundedNumericUpDown()
    {
        BorderStyle = BorderStyle.FixedSingle;
    }

    protected override bool ShowFocusCues => true;

    public bool FocusCuesVisible => ShowFocusCues;

    protected override void OnCreateControl()
    {
        base.OnCreateControl();
        ApplyChildColors();
    }

    protected override void OnBackColorChanged(EventArgs e)
    {
        base.OnBackColorChanged(e);
        ApplyChildColors();
    }

    protected override void OnForeColorChanged(EventArgs e)
    {
        base.OnForeColorChanged(e);
        ApplyChildColors();
    }

    private void ApplyChildColors()
    {
        foreach (Control child in Controls)
        {
            child.BackColor = BackColor;
            child.ForeColor = ForeColor;
        }
    }
}

internal sealed class CardPanel : Panel
{
    public Color BorderColor { get; set; } = Color.Gray;
    public Color GradientColor { get; set; } = Color.Gray;
    public int CornerRadius { get; set; } = 12;

    public CardPanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? BackColor);

        // Layered shadow for depth (two shadow layers for softness)
        var shadowOffset = new Point(0, 6);
        var shadowBounds1 = new Rectangle(shadowOffset.X, shadowOffset.Y,
            Math.Max(1, Width - 7), Math.Max(1, Height - 8));
        var shadowBounds2 = new Rectangle(shadowOffset.X + 1, shadowOffset.Y + 2,
            Math.Max(1, Width - 9), Math.Max(1, Height - 10));
        using var shadowPath1 = UiGeometry.RoundedRectangle(shadowBounds1, CornerRadius);
        using var shadowPath2 = UiGeometry.RoundedRectangle(shadowBounds2, CornerRadius);
        using var softShadowBrush = new SolidBrush(Color.FromArgb(15, 0, 0, 0));
        using var hardShadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0));
        e.Graphics.FillPath(softShadowBrush, shadowPath2);
        e.Graphics.FillPath(hardShadowBrush, shadowPath1);

        // Draw the card body with gradient
        var bodyBounds = new Rectangle(0, 0, Math.Max(1, Width - 4), Math.Max(1, Height - 5));
        using var bodyPath = UiGeometry.RoundedRectangle(bodyBounds, CornerRadius);
        using var bodyBrush = new LinearGradientBrush(bodyBounds, GradientColor, BackColor, 105F);
        e.Graphics.FillPath(bodyBrush, bodyPath);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = UiGeometry.RoundedRectangle(
            new Rectangle(0, 0, Math.Max(1, Width - 4), Math.Max(1, Height - 5)), CornerRadius);
        using var pen = new Pen(BorderColor);
        e.Graphics.DrawPath(pen, path);
    }
}
