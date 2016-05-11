using System;
using System.Collections.Generic;
using System.Text;
using CoreGraphics;

namespace Avalonia.iOS
{
    static class Extensions
    {

        public static Size ToAvalonia(this CGSize size) => new Size(size.Width, size.Height);

        public static Point ToAvalonia(this CGPoint point) => new Point(point.X, point.Y);
    }
}
