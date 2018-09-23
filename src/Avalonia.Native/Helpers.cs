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
    }
}
