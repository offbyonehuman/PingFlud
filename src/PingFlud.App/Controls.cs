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

    public int CornerRadius { get; set; } = 8;

    public RoundedButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
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

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = UiGeometry.RoundedRectangle(bounds, CornerRadius);
        var fill = !Enabled
            ? BackColor
            : _pressed && FlatAppearance.MouseDownBackColor != Color.Empty
                ? FlatAppearance.MouseDownBackColor
                : _hovered && FlatAppearance.MouseOverBackColor != Color.Empty
                    ? FlatAppearance.MouseOverBackColor
                    : BackColor;
        var gradientEnd = Enabled ? ControlPaint.Light(fill, 0.08F) : fill;
        using var brush = new LinearGradientBrush(bounds, gradientEnd, fill, LinearGradientMode.Vertical);
        using var pen = new Pen(FlatAppearance.BorderColor == Color.Empty ? fill : FlatAppearance.BorderColor);
        e.Graphics.FillPath(brush, path);
        e.Graphics.DrawPath(pen, path);

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
        TextRenderer.DrawText(e.Graphics, Text, Font, textBounds, ForeColor, flags);
        if (Focused && ShowFocusCues)
            ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(bounds, -4, -4), ForeColor, fill);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        using var path = UiGeometry.RoundedRectangle(
            new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1)), CornerRadius);
        Region = new Region(path);
    }
}

internal sealed class CardPanel : Panel
{
    public Color BorderColor { get; set; } = Color.Gray;
    public Color GradientColor { get; set; } = Color.Gray;
    public Color ShadowColor { get; set; } = Color.FromArgb(70, 0, 0, 0);
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
        var shadowBounds = new Rectangle(3, 4, Math.Max(1, Width - 7), Math.Max(1, Height - 8));
        var bodyBounds = new Rectangle(0, 0, Math.Max(1, Width - 4), Math.Max(1, Height - 5));
        using var shadowPath = UiGeometry.RoundedRectangle(shadowBounds, CornerRadius);
        using var bodyPath = UiGeometry.RoundedRectangle(bodyBounds, CornerRadius);
        using var shadowBrush = new SolidBrush(ShadowColor);
        using var bodyBrush = new LinearGradientBrush(bodyBounds, GradientColor, BackColor, 105F);
        e.Graphics.FillPath(shadowBrush, shadowPath);
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
