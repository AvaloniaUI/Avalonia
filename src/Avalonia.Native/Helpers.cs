// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Native.Interop;

namespace Avalonia.Native
{
    public static class Helpers
    {
        public static Point ToAvaloniaPoint (this AvnPoint pt)
        {
            return new Point(pt.X, pt.Y);
        }

        public static PixelPoint ToAvaloniaPixelPoint(this AvnPoint pt)
        {
            return new PixelPoint((int)pt.X, (int)pt.Y);
        }

        public static AvnPoint ToAvnPoint (this Point pt)
        {
            return new AvnPoint { X = pt.X, Y = pt.Y };
        }

        public static AvnPoint ToAvnPoint(this PixelPoint pt)
        {
            return new AvnPoint { X = pt.X, Y = pt.Y };
        }

        public static AvnSize ToAvnSize (this Size size)
        {
            return new AvnSize { Height = size.Height, Width = size.Width };
        }

        public static Size ToAvaloniaSize (this AvnSize size)
        {
            return new Size(size.Width, size.Height);
        }

        public static Rect ToAvaloniaRect (this AvnRect rect)
        {
            return new Rect(rect.X, rect.Y, rect.Width, rect.Height);
        }

        public static PixelRect ToAvaloniaPixelRect(this AvnRect rect)
        {
            return new PixelRect((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
        }
    }
}
