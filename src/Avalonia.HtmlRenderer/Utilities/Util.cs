// "Therefore those skilled at the unorthodox
// are infinite as heaven and earth,
// inexhaustible as the great rivers.
// When they come to an end,
// they begin again,
// like the days and months;
// they die and are reborn,
// like the four seasons."
// 
// - Sun Tsu,
// "The Art of War"

using Avalonia;
using Avalonia.Media;
using TheArtOfDev.HtmlRenderer.Adapters.Entities;

namespace TheArtOfDev.HtmlRenderer.Avalonia.Utilities
{
    /// <summary>
    /// Utilities for converting WPF entities to HtmlRenderer core entities.
    /// </summary>
    internal static class Util
    {
        /// <summary>
        /// Convert from WPF point to core point.
        /// </summary>
        public static RPoint Convert(Point p)
        {
            return new RPoint(p.X, p.Y);
        }

        /// <summary>
        /// Convert from WPF point to core point.
        /// </summary>
        public static Point[] Convert(RPoint[] points)
        {
            Point[] myPoints = new Point[points.Length];
            for (int i = 0; i < points.Length; i++)
                myPoints[i] = Convert(points[i]);
            return myPoints;
        }

        /// <summary>
        /// Convert from core point to WPF point.
        /// </summary>
        public static Point Convert(RPoint p)
        {
            return new Point(p.X, p.Y);
        }

        /// <summary>
        /// Convert from core point to WPF point.
        /// </summary>
        public static Point ConvertRound(RPoint p)
        {
            return new Point((int)p.X, (int)p.Y);
        }

        /// <summary>
        /// Convert from WPF size to core size.
        /// </summary>
        public static RSize Convert(Size s)
        {
            return new RSize(s.Width, s.Height);
        }

        /// <summary>
        /// Convert from core size to WPF size.
        /// </summary>
        public static Size Convert(RSize s)
        {
            return new Size(s.Width, s.Height);
        }

        /// <summary>
        /// Convert from core point to WPF point.
        /// </summary>
        public static Size ConvertRound(RSize s)
        {
            return new Size((int)s.Width, (int)s.Height);
        }

        /// <summary>
        /// Convert from WPF rectangle to core rectangle.
        /// </summary>
        public static RRect Convert(Rect r)
        {
            return new RRect(r.X, r.Y, r.Width, r.Height);
        }

        /// <summary>
        /// Convert from core rectangle to WPF rectangle.
        /// </summary>
        public static Rect Convert(RRect r)
        {
            return new Rect(r.X, r.Y, r.Width, r.Height);
        }

        /// <summary>
        /// Convert from core rectangle to WPF rectangle.
        /// </summary>
        public static Rect ConvertRound(RRect r)
        {
            return new Rect((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height);
        }

        /// <summary>
        /// Convert from WPF color to core color.
        /// </summary>
        public static RColor Convert(Color c)
        {
            return RColor.FromArgb(c.A, c.R, c.G, c.B);
        }

        /// <summary>
        /// Convert from core color to WPF color.
        /// </summary>
        public static Color Convert(RColor c)
        {
            return Color.FromArgb(c.A, c.R, c.G, c.B);
        }
    }
}