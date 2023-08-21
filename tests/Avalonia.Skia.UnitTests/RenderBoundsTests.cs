using System;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests
{
    public class RenderBoundsTests
    {
        [Theory,
            InlineData("M10 20 L 20 10 L 30 20", PenLineCap.Round, PenLineJoin.Miter, 2, 10, 
                8.585786819458008, 8.585786819458008, 22.828428268432617, 12.828428268432617),
            InlineData("M10 10 L 20 10", PenLineCap.Round, PenLineJoin.Miter,2, 10,
                9,9,12,2),
            InlineData("M10 10 L 20 15 L 10 20", PenLineCap.Flat, PenLineJoin.Miter, 2, 20,
                9.552786827087402, 9.105572700500488, 12.683281898498535, 11.788853645324707)
        
        ]
        public void RenderBoundsAreCorrectlyCalculated(string path, PenLineCap cap, PenLineJoin join, double thickness, double miterLimit, double x, double y, double width, double height)
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
                       .With(renderInterface: new PlatformRenderInterface())))
            {
                var geo = PathGeometry.Parse(path);
                var pen = new Pen(Brushes.Black, thickness, null, cap, join, miterLimit);
                var bounds = geo.GetRenderBounds(pen);
                var tolerance = 0.001;
                if (
                    Math.Abs(bounds.X - x) > tolerance
                    || Math.Abs(bounds.Y - y) > tolerance
                    || Math.Abs(bounds.Width - width) > tolerance
                    || Math.Abs(bounds.Height - height) > tolerance)
                    Assert.Fail($"Expected {x}:{y}:{width}:{height}, got {bounds}");
                
                Assert.Equal(new Rect(x, y, width, height), bounds);
            }
        }
    }
}
