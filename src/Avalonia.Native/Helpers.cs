using System;
using Avalonia.Native.Interop;

namespace Avalonia.Native
{
    public static class Helpers
    {
        public static Point ToAvaloniaPoint (this AvnPoint pt)
        {
            return new Point(pt.X, pt.Y);
        }

        public static AvnPoint ToAvnPoint (this Point pt)
        {
            return new AvnPoint { X = pt.X, Y = pt.Y };
        }

        public static Size ToAvaloniaSize (this AvnSize size)
        {
            return new Size(size.Width, size.Height);
        }

        public static Rect ToAvaloniaRect (this AvnRect rect)
        {
            return new Rect(rect.X, rect.Y, rect.Width, rect.Height);
        }
    }
}
