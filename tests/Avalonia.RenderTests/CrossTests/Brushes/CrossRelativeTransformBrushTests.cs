using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media;
using CrossUI;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests;
#else
namespace Avalonia.RenderTests.WpfCompare;
#endif

public class CrossRelativeTransformBrushTests : CrossTestBase
{
    public CrossRelativeTransformBrushTests() : base("Media/RelativeTransformBrush")
    {
    }

    // A rotation about an off-centre point of the unit square: bounds-dependent once conjugated
    // into target space, and asymmetric enough to show on a centre-symmetric radial gradient,
    // where a rotation about the centre would be invisible.
    private static readonly Matrix s_unitRotation =
        Matrix.CreateTranslation(-0.3, -0.7)
        * Matrix.CreateRotation(Matrix.ToRadians(30))
        * Matrix.CreateTranslation(0.3, 0.7);

    [CrossFact]
    public void Linear_Gradient_Relative_Transform_Should_Work_As_Expected()
    {
        RenderAndCompare(Scene(ctx =>
            ctx.DrawRectangle(Linear(relative: s_unitRotation), null, new Rect(40, 70, 120, 60))));
    }

    [CrossFact]
    public void Radial_Gradient_Relative_Transform_Should_Work_As_Expected()
    {
        RenderAndCompare(Scene(ctx =>
            ctx.DrawRectangle(Radial(relative: s_unitRotation), null, new Rect(40, 70, 120, 60))));
    }

    [CrossFact]
    public void Relative_Transform_Should_Apply_Before_Transform()
    {
        // A scale rather than a translation: it does not commute with the relative rotation, so the
        // two orders are far enough apart to be told apart by the comparison.
        RenderAndCompare(Scene(ctx =>
            ctx.DrawRectangle(
                Linear(relative: s_unitRotation, absolute: Matrix.CreateScale(0.4, 0.4)),
                null,
                new Rect(40, 70, 120, 60))));
    }

    [CrossFact]
    public void Relative_Transform_Should_Follow_The_Bounds_Of_Each_Fill()
    {
        // One brush instance, two differently sized rects: the unit space is each rect's own, so
        // the two fills must differ in shape.
        RenderAndCompare(Scene(ctx =>
        {
            var shared = Linear(relative: s_unitRotation);
            ctx.DrawRectangle(shared, null, new Rect(40, 30, 120, 60));
            ctx.DrawRectangle(shared, null, new Rect(30, 110, 60, 90));
        }));
    }

    [CrossFact]
    public void Solid_Color_Relative_Transform_Should_Be_Ignored()
    {
        RenderAndCompare(Scene(ctx =>
            ctx.DrawRectangle(
                new CrossSolidColorBrush(Colors.Crimson) { RelativeTransform = s_unitRotation },
                null,
                new Rect(40, 70, 120, 60))));
    }

    [CrossFact]
    public void Drawing_Brush_Relative_Transform_Should_Work_As_Expected()
    {
        RenderAndCompare(Scene(ctx =>
            ctx.DrawRectangle(Checkerboard(relative: s_unitRotation), null, new Rect(40, 70, 120, 60))));
    }

    [CrossFact]
    public void Tiled_Drawing_Brush_Relative_Transform_Should_Work_As_Expected()
    {
        var brush = Checkerboard(relative: s_unitRotation);
        brush.TileMode = TileMode.Tile;
        brush.Viewport = new Rect(0, 0, 30, 30);
        brush.ViewportUnits = BrushMappingMode.Absolute;

        RenderAndCompare(Scene(ctx => ctx.DrawRectangle(brush, null, new Rect(40, 70, 120, 60))));
    }

    [CrossFact]
    public void Image_Brush_Relative_Transform_Should_Work_As_Expected()
    {
        RenderAndCompare(Scene(ctx =>
            ctx.DrawRectangle(
                new CrossImageBrush { Path = RampImagePath, RelativeTransform = s_unitRotation },
                null,
                new Rect(40, 70, 120, 60))));
    }

    // A smooth two-axis ramp rather than a line drawing: every pixel carries position, so a
    // misplaced transform shows up as a colour shift over the whole fill instead of hiding in the
    // resampling noise the two backends produce differently along hard edges.
    [CrossFact]
    public void Tiled_Image_Brush_Relative_And_Absolute_Transform_Should_Work_As_Expected()
    {
        // Tiling and an absolute viewport put the tile offset and the destination translate into
        // the shader matrix, so this is where the slot the two brush transforms occupy matters.
        RenderAndCompare(Scene(ctx => ctx.DrawRectangle(
            new CrossImageBrush
            {
                Path = RampImagePath,
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, 40, 40),
                ViewportUnits = BrushMappingMode.Absolute,
                RelativeTransform = s_unitRotation,
                Transform = Matrix.CreateScale(0.6, 0.6)
            },
            null,
            new Rect(40, 70, 120, 60))));
    }

    private static string RampImagePath => Path.Join(
        Path.GetDirectoryName(typeof(CrossRelativeTransformBrushTests).Assembly.Location),
        "Assets",
        "Ramp64.png");

    private static CrossControl Scene(Action<ICrossDrawingContext> render) =>
        new CrossFuncControl(render)
        {
            Width = 200,
            Height = 200,
            Background = new CrossSolidColorBrush(Colors.White)
        };

    private static CrossLinearGradientBrush Linear(Matrix? relative = null, Matrix? absolute = null) =>
        new()
        {
            GradientStops = { new GradientStop(Colors.Red, 0), new GradientStop(Colors.Blue, 1) },
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 0),
            RelativeTransform = relative,
            Transform = absolute
        };

    private static CrossRadialGradientBrush Radial(Matrix? relative = null, Matrix? absolute = null) =>
        new()
        {
            GradientStops = { new GradientStop(Colors.Red, 0), new GradientStop(Colors.Blue, 1) },
            Center = new Point(0.5, 0.5),
            GradientOrigin = new Point(0.5, 0.5),
            RadiusX = 0.5,
            RadiusY = 0.5,
            RelativeTransform = relative,
            Transform = absolute
        };

    private static CrossDrawingBrush Checkerboard(Matrix? relative = null, Matrix? absolute = null) =>
        new()
        {
            Drawing = new CrossDrawingGroup
            {
                Children = new List<CrossDrawing>
                {
                    new CrossGeometryDrawing(new CrossRectangleGeometry(new Rect(0, 0, 20, 20)))
                    {
                        Brush = new CrossSolidColorBrush(Colors.Crimson)
                    },
                    new CrossGeometryDrawing(new CrossRectangleGeometry(new Rect(0, 0, 10, 10)))
                    {
                        Brush = new CrossSolidColorBrush(Colors.MidnightBlue)
                    },
                    new CrossGeometryDrawing(new CrossRectangleGeometry(new Rect(10, 10, 10, 10)))
                    {
                        Brush = new CrossSolidColorBrush(Colors.MidnightBlue)
                    }
                }
            },
            RelativeTransform = relative,
            Transform = absolute
        };
}
