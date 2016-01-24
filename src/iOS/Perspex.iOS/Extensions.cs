using System;
using System.Collections.Generic;
using System.Text;
using CoreGraphics;

namespace Perspex.iOS
{
    static class Extensions
    {

        public static Size ToPerspex(this CGSize size) => new Size(size.Width, size.Height);

        public static Point ToPerspex(this CGPoint point) => new Point(point.X, point.Y);
    }
}
