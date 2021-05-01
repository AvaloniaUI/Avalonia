using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests.Media
#endif
{
    public class WriteableBitmapTests : TestBase
    {
        public WriteableBitmapTests()
            : base(@"Media\WriteableBitmap")
        {
            Directory.CreateDirectory(OutputPath);
        }

        [Fact]
        public void WriteableBitmap_DrawingContext()
        {
            using var target = new WriteableBitmap(
                new PixelSize(100, 100),
                new Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Premul);

            using (var context = target.CreateDrawingContext())
            {
                var geometry = new PolylineGeometry(
                    new[] { new Point(5, 0), new Point(8, 8), new Point(0, 3), new Point(10, 3), new Point(2, 8) },
                    true);
                context.FillRectangle(Brushes.White, new Rect(0, 0, 100, 100));
                context.PushPostTransform(Matrix.CreateScale(10, 12));
                context.DrawGeometry(Brushes.Violet, null, geometry);
            }

            var testName = nameof(WriteableBitmap_DrawingContext);
            target.Save(System.IO.Path.Combine(OutputPath, testName + ".out.png"));
            CompareImagesNoRenderer(testName);
        }

        [Fact]
        public void WriteableBitmap_DrawingContext_Two_Draws()
        {
            using var target = new WriteableBitmap(
                new PixelSize(100, 100),
                new Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Premul);

            using (var context = target.CreateDrawingContext())
            {
                var geometry = new PolylineGeometry(
                    new[] { new Point(5, 0), new Point(8, 8), new Point(0, 3), new Point(10, 3), new Point(2, 8) },
                    true);
                context.FillRectangle(Brushes.White, new Rect(0, 0, 100, 100));
                context.PushPostTransform(Matrix.CreateScale(10, 12));
                context.DrawGeometry(Brushes.Violet, null, geometry);
            }

            using (var context = target.CreateDrawingContext())
            {
                var geometry = new EllipseGeometry(new Rect(40, 40, 20, 20));
                context.DrawGeometry(Brushes.Red, null, geometry);
            }

            var testName = nameof(WriteableBitmap_DrawingContext_Two_Draws);
            target.Save(System.IO.Path.Combine(OutputPath, testName + ".out.png"));
            CompareImagesNoRenderer(testName);
        }
    }
}
