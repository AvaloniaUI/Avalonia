using System;
using Avalonia.Media;
using CoreGraphics;
using ObjCRuntime;
using UIKit;

namespace Avalonia.iOS
{
    static class Extensions
    {

        public static Size ToAvalonia(this CGSize size) => new Size(size.Width, size.Height);

        public static Point ToAvalonia(this CGPoint point) => new Point(point.X, point.Y);

        static nfloat ColorComponent(byte c) => ((float) c) / 255;

        public static UIColor ToUiColor(this Color color) => new UIColor(
            ColorComponent(color.R),
            ColorComponent(color.G),
            ColorComponent(color.B),
            ColorComponent(color.A));
    }
}
