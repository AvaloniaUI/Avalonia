using Avalonia.Media;
using Avalonia.Native.Interop;

namespace Avalonia.NativeGraphics.Backend
{
    public static class NativeGraphicsExtensions
    {
        public static AvgPoint ToAvgPoint(this Point p)
        {
            return new AvgPoint {X = p.X, Y = p.Y};
        }

        public static AvgRect ToAvgRect(this Rect r)
        {
            return new AvgRect {X = r.X, Y = r.Y, Width = r.Width, Height = r.Height};
        }

        public static AvgVector ToAvgVector(this Vector v)
        {
            return new AvgVector {X = v.X, Y = v.Y};
        }

        public static AvgRoundRect ToAvgRoundRect(this RoundedRect r)
        {
            AvgRoundRect avgRoundRect = new AvgRoundRect();
            avgRoundRect.Rect = r.Rect.ToAvgRect();

            if (r.IsRounded)
            {
                avgRoundRect.IsRounded = 1;
                avgRoundRect.RadiiTopLeft = r.RadiiTopLeft.ToAvgVector();
                avgRoundRect.RadiiBottomLeft = r.RadiiBottomLeft.ToAvgVector();
                avgRoundRect.RadiiBottomRight = r.RadiiBottomRight.ToAvgVector();
                avgRoundRect.RadiiTopRight = r.RadiiTopRight.ToAvgVector();
            }

            return avgRoundRect;
        }
        public static AvgColor ToAvgColor(this Color c)
        {
            return new AvgColor {R = c.R, G = c.G, B = c.B, A = c.A};
        }

        public static AvgBrush ToAvgBrush(this IBrush b)
        {
            var avgBrush = new AvgBrush {Opacity = b.Opacity, Valid = 1};
            if (b is ISolidColorBrush solid)
            {
                avgBrush.Color = solid.Color.ToAvgColor();
            }

            return avgBrush;
        }

        public static AvgPen ToAvgPen(this IPen p)
        {
            var avgPen = new AvgPen
            {
                Valid = 1,
                Brush = p.Brush.ToAvgBrush(),
                MiterLimit = p.MiterLimit,
                Thickness = p.Thickness,
            };

            return avgPen;
        }

    }
}