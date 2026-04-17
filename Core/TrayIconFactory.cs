using System.Drawing;
using System.Drawing.Drawing2D;

namespace VpnMonitoring.Core;

/// <summary>
/// Creates a small tray icon at runtime without requiring an embedded .ico file.
/// Green circle = connected, red circle = disconnected.
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

        var shieldColor = connected
            ? Color.FromArgb(0x22, 0xC5, 0x5E)   // emerald green
            : Color.FromArgb(0xEF, 0x44, 0x44);   // red

        float margin = Size * 0.06f;
        var circleRect = new RectangleF(margin, margin, Size - margin * 2, Size - margin * 2);

        using var fillBrush = new SolidBrush(shieldColor);
        g.FillEllipse(fillBrush, circleRect);

        // ── Icon inside circle ───────────────────────────────────────────────
        using var wPen = new Pen(Color.White, 2.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        if (connected)
        {
            var pts = new PointF[]
            {
                new(10, 16),
                new(14, 21),
                new(22, 11)
            };
            g.DrawLines(wPen, pts);
        }
        else
        {
            g.DrawLine(wPen, 11, 11, 21, 21);
            g.DrawLine(wPen, 21, 11, 11, 21);
        }

        var hIcon = bmp.GetHicon();
        // FromHandle creates a copy, so we must destroy the GDI handle afterwards.
        var icon = System.Drawing.Icon.FromHandle(hIcon);
        return icon;
    }

}
