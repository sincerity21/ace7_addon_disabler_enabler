using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DisableEnabler;

internal static class UiTheme
{
    public const int CornerRadius = 8;
    public const int ChipCornerRadius = 10;
    public const int ButtonCornerRadius = 8;

    // Softer palette: lower contrast borders, muted accents, less “HUD neon”.
    public static Color AppBg(bool dark) =>
        dark ? Color.FromArgb(22, 24, 28) : Color.FromArgb(242, 244, 247);

    public static Color PanelBg(bool dark) =>
        dark ? Color.FromArgb(32, 35, 42) : Color.FromArgb(250, 251, 253);

    public static Color PanelBgAlt(bool dark) =>
        dark ? Color.FromArgb(40, 44, 52) : Color.FromArgb(236, 240, 245);

    public static Color InputBg(bool dark) =>
        dark ? Color.FromArgb(28, 30, 36) : Color.FromArgb(255, 255, 255);

    public static Color Border(bool dark) =>
        dark ? Color.FromArgb(58, 64, 76) : Color.FromArgb(210, 216, 226);

    public static Color Cyan(bool dark) =>
        dark ? Color.FromArgb(120, 176, 210) : Color.FromArgb(56, 118, 168);

    public static Color CyanDim(bool dark) =>
        dark ? Color.FromArgb(70, 100, 122) : Color.FromArgb(150, 178, 200);

    public static Color Orange => Color.FromArgb(220, 148, 72);

    public static Color OrangeHover => Color.FromArgb(232, 164, 92);

    public static Color OrangePressed => Color.FromArgb(196, 128, 56);

    public static Color Success => Color.FromArgb(96, 178, 132);

    public static Color TextPrimary(bool dark) =>
        dark ? Color.FromArgb(228, 232, 238) : Color.FromArgb(32, 36, 44);

    public static Color TextMuted(bool dark) =>
        dark ? Color.FromArgb(148, 156, 168) : Color.FromArgb(100, 110, 124);

    public static Color GridRow(bool dark) =>
        dark ? Color.FromArgb(26, 28, 34) : Color.FromArgb(255, 255, 255);

    public static Color GridRowAlt(bool dark) =>
        dark ? Color.FromArgb(32, 35, 42) : Color.FromArgb(246, 248, 250);

    public static Color SelectionBg(bool dark) =>
        dark ? Color.FromArgb(48, 72, 96) : Color.FromArgb(200, 220, 238);

    public static Color SecondaryButtonBg(bool dark) =>
        dark ? Color.FromArgb(42, 48, 58) : Color.FromArgb(228, 234, 242);

    public static Color DisabledButtonBg(bool dark) =>
        dark ? Color.FromArgb(30, 33, 40) : Color.FromArgb(218, 222, 228);

    public static Color DisabledButtonText(bool dark) =>
        dark ? Color.FromArgb(88, 94, 106) : Color.FromArgb(156, 164, 176);

    public static Color LogBg(bool dark) =>
        dark ? Color.FromArgb(18, 20, 24) : Color.FromArgb(252, 252, 253);

    public static GraphicsPath CreateRoundRect(Rectangle bounds, int radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        if (radius <= 0 || bounds.Width < diameter || bounds.Height < diameter)
        {
            path.AddRectangle(bounds);
            return path;
        }

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

    public static void ApplyRoundRegion(Control control, int radius)
    {
        if (control.Width <= 0 || control.Height <= 0)
            return;

        var bounds = new Rectangle(0, 0, control.Width, control.Height);
        using var path = CreateRoundRect(bounds, radius);
        var old = control.Region;
        control.Region = new Region(path);
        old?.Dispose();
    }
}
