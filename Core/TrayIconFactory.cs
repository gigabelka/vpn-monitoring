using System.Drawing;
using System.Drawing.Drawing2D;

namespace VpnMonitor.Core;

/// <summary>
/// Creates a small tray icon at runtime without requiring an embedded .ico file.
/// Green shield = connected, grey shield = disconnected.
/// </summary>
internal static class TrayIconFactory
{
    private const int Size = 32;

    public static System.Drawing.Icon Create(bool connected)
    {
        using var bmp = new Bitmap(Size, Size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g   = Graphics.FromImage(bmp);

        g.SmoothingMode     = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.Clear(Color.Transparent);

        // ── Shield outline ────────────────────────────────────────────────────
        var shieldColor = connected
            ? Color.FromArgb(0x22, 0xC5, 0x5E)   // emerald green
            : Color.FromArgb(0x6B, 0x72, 0x80);   // slate grey

        var shieldPath = BuildShieldPath(Size);

        using var fillBrush = new SolidBrush(shieldColor);
        g.FillPath(fillBrush, shieldPath);

        // ── Lock / signal icon inside shield ─────────────────────────────────
        using var wPen = new Pen(Color.White, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        if (connected)
        {
            // Draw a small tick (✔) in white
            var pts = new PointF[]
            {
                new(10, 18),
                new(14, 22),
                new(22, 12)
            };
            g.DrawLines(wPen, pts);
        }
        else
        {
            // Draw an X
            g.DrawLine(wPen, 11, 11, 21, 21);
            g.DrawLine(wPen, 21, 11, 11, 21);
        }

        var hIcon = bmp.GetHicon();
        // FromHandle creates a copy, so we must destroy the GDI handle afterwards.
        var icon = System.Drawing.Icon.FromHandle(hIcon);
        return icon;
    }

    // ── Shield path ───────────────────────────────────────────────────────────
    private static GraphicsPath BuildShieldPath(int s)
    {
        // Simple shield: rounded top, pointed bottom
        var p = new GraphicsPath();
        float m = s * 0.1f; // margin
        float w = s - m * 2;

        p.AddArc(m, m, w, w * 0.6f, 180, 180);       // top arc
        p.AddLine(m + w, m + w * 0.3f, s / 2f, s - m); // right side → point
        p.AddLine(s / 2f, s - m, m, m + w * 0.3f);    // point → left side
        p.CloseFigure();
        return p;
    }
}
