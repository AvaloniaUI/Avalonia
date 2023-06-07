using Avalonia.Media;
using Avalonia.Skia.Helpers;
using SkiaSharp;
using Xunit;

namespace Avalonia.Skia.UnitTests
{
    public class DrawingContextImplTests
    {
        [Fact]
        public void DrawLine_With_Zero_Thickness_Pen_Does_Not_Throw()
        {
            var target = CreateTarget();
            target.DrawLine(new Pen(Brushes.Black, 0), new Point(0, 0), new Point(10, 10));
        }

        [Fact]
        public void DrawRectangle_With_Zero_Thickness_Pen_Does_Not_Throw()
        {
            var target = CreateTarget();
            target.DrawRectangle(Brushes.Black, new Pen(Brushes.Black, 0), new RoundedRect(new Rect(0, 0, 100, 100), new CornerRadius(4)));
        }

        private static DrawingContextImpl CreateTarget()
        {
            var canvas = new SKCanvas(new SKBitmap(100, 100));
            return (DrawingContextImpl)DrawingContextHelper.WrapSkiaCanvas(canvas, new Vector(96, 96));
        }
    }
}
