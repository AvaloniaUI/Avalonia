#nullable enable

using Avalonia.Collections;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.UnitTests;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Controls.UnitTests.Shapes;

public class ShapeTests : ScopedTestBase
{
    [Fact]
    public void StrokeMiterLimit_Default_Is_Applied_To_Pen()
    {
        using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
        var pen = RenderAndGetPen(new TestShape
        {
            StrokeThickness = 4,
            Stroke = Brushes.Black
        });

        Assert.NotNull(pen);
        Assert.Equal(10, pen.MiterLimit);
    }

    [Fact]
    public void StrokeMiterLimit_Update_Refreshes_Pen()
    {
        using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
        var shape = new TestShape
        {
            StrokeThickness = 4,
            Stroke = Brushes.Black
        };

        RenderAndGetPen(shape);
        shape.StrokeMiterLimit = 2;
        var pen = RenderAndGetPen(shape);

        Assert.NotNull(pen);
        Assert.Equal(2, pen.MiterLimit);
    }

    [Fact]
    public void StrokeThickness_Is_Applied_To_Pen()
    {
        using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
        var pen = RenderAndGetPen(new TestShape
        {
            StrokeThickness = 6,
            Stroke = Brushes.Black
        });

        Assert.NotNull(pen);
        Assert.Equal(6, pen.Thickness);
    }

    [Fact]
    public void StrokeLineCap_And_Join_Are_Applied_To_Pen()
    {
        using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
        var pen = RenderAndGetPen(new TestShape
        {
            Stroke = Brushes.Black,
            StrokeThickness = 4,
            StrokeLineCap = PenLineCap.Round,
            StrokeJoin = PenLineJoin.Bevel
        });

        Assert.NotNull(pen);
        Assert.Equal(PenLineCap.Round, pen.LineCap);
        Assert.Equal(PenLineJoin.Bevel, pen.LineJoin);
    }

    [Fact]
    public void StrokeDashArray_And_Offset_Are_Applied_To_Pen()
    {
        using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
        var pen = RenderAndGetPen(new TestShape
        {
            Stroke = Brushes.Black,
            StrokeThickness = 4,
            StrokeDashArray = new AvaloniaList<double>(1, 2, 3),
            StrokeDashOffset = 1.5
        });

        Assert.NotNull(pen);
        Assert.NotNull(pen.DashStyle);
        Assert.Equal(3, pen.DashStyle!.Dashes!.Count);
        Assert.Equal(1, pen.DashStyle.Dashes[0]);
        Assert.Equal(2, pen.DashStyle.Dashes[1]);
        Assert.Equal(3, pen.DashStyle.Dashes[2]);
        Assert.Equal(1.5, pen.DashStyle.Offset);
    }

    [Fact]
    public void No_Stroke_Produces_No_Pen()
    {
        using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
        var pen = RenderAndGetPen(new TestShape
        {
            Stroke = null,
            StrokeThickness = 4
        });

        Assert.Null(pen);
    }

    private static IPen? RenderAndGetPen(Shape shape)
    {
        using var context = new RecordingDrawingContext();
        shape.Render(context);
        return context.LastPen;
    }

    private class TestShape : Shape
    {
        protected override Geometry CreateDefiningGeometry() =>
            new RectangleGeometry(new Rect(0, 0, 20, 20));
    }

    private class RecordingDrawingContext : DrawingContext
    {
        public IPen? LastPen { get; private set; }

        public void Reset() => LastPen = null;

        internal override void DrawBitmap(IRef<IBitmapImpl> source, double opacity, Rect sourceRect, Rect destRect)
        {
        }

        protected override void DrawLineCore(IPen pen, Point p1, Point p2)
        {
        }

        protected override void DrawGeometryCore(IBrush? brush, IPen? pen, IGeometryImpl geometry)
        {
            LastPen = pen;
        }

        protected override void DrawRectangleCore(IBrush? brush, IPen? pen, RoundedRect rrect, BoxShadows boxShadows = default)
        {
        }

        protected override void DrawEllipseCore(IBrush? brush, IPen? pen, Rect rect)
        {
        }

        public override void Custom(ICustomDrawOperation custom)
        {
        }

        public override void DrawGlyphRun(IBrush? foreground, GlyphRun glyphRun)
        {
        }

        protected override void PushClipCore(RoundedRect rect)
        {
        }

        protected override void PushClipCore(Rect rect)
        {
        }

        protected override void PushGeometryClipCore(Geometry clip)
        {
        }

        protected override void PushOpacityCore(double opacity)
        {
        }

        protected override void PushOpacityMaskCore(IBrush mask, Rect bounds)
        {
        }

        protected override void PushRenderOptionsCore(RenderOptions renderOptions)
        {
        }

        protected override void PushTransformCore(Matrix matrix)
        {
        }

        protected override void PopClipCore()
        {
        }

        protected override void PopGeometryClipCore()
        {
        }

        protected override void PopOpacityCore()
        {
        }

        protected override void PopOpacityMaskCore()
        {
        }

        protected override void PopTransformCore()
        {
        }

        protected override void PopRenderOptionsCore()
        {
        }

        protected override void DisposeCore()
        {
        }
    }
}
