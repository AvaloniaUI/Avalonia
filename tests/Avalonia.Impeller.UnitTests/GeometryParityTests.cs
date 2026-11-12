using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Skia;
using Xunit;

namespace Avalonia.Impeller.UnitTests;

/// <summary>Geometry from the Impeller render interface must answer like the Skia backend.</summary>
public class GeometryParityTests
{
    private static readonly IPlatformRenderInterface Impeller = new PlatformRenderInterface(new ImpellerOptions());
    private static readonly IPlatformRenderInterface Skia = CreateSkia();

    private static IPlatformRenderInterface CreateSkia()
    {
        using (AvaloniaLocator.EnterScope())
        {
            SkiaPlatform.Initialize();
            return AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();
        }
    }

    public static IEnumerable<object[]> Geometries() =>
    [
        ["ellipse", (Func<IPlatformRenderInterface, IGeometryImpl>)(r => r.CreateEllipseGeometry(new Rect(10, 20, 80, 50)))],
        ["rectangle", (Func<IPlatformRenderInterface, IGeometryImpl>)(r => r.CreateRectangleGeometry(new Rect(5, 5, 60, 40)))],
        ["line", (Func<IPlatformRenderInterface, IGeometryImpl>)(r => r.CreateLineGeometry(new Point(10, 10), new Point(90, 60)))],
        ["stream", (Func<IPlatformRenderInterface, IGeometryImpl>)Stream],
        ["unstroked segments", (Func<IPlatformRenderInterface, IGeometryImpl>)UnstrokedStream],
        ["trailing moves", (Func<IPlatformRenderInterface, IGeometryImpl>)TrailingMovesStream],
        ["group", (Func<IPlatformRenderInterface, IGeometryImpl>)(r => r.CreateGeometryGroup(FillRule.EvenOdd,
            [r.CreateRectangleGeometry(new Rect(0, 0, 60, 60)), r.CreateEllipseGeometry(new Rect(20, 20, 60, 60))]))],
        ["union", (Func<IPlatformRenderInterface, IGeometryImpl>)(r => Combined(r, GeometryCombineMode.Union))],
        ["intersect", (Func<IPlatformRenderInterface, IGeometryImpl>)(r => Combined(r, GeometryCombineMode.Intersect))],
        ["xor", (Func<IPlatformRenderInterface, IGeometryImpl>)(r => Combined(r, GeometryCombineMode.Xor))],
        ["exclude", (Func<IPlatformRenderInterface, IGeometryImpl>)(r => Combined(r, GeometryCombineMode.Exclude))],
        ["transformed", (Func<IPlatformRenderInterface, IGeometryImpl>)(r => Stream(r).WithTransform(
            Matrix.CreateRotation(0.4) * Matrix.CreateScale(1.5, 0.8) * Matrix.CreateTranslation(10, 5)))],
    ];

    private static IGeometryImpl Combined(IPlatformRenderInterface r, GeometryCombineMode mode) =>
        r.CreateCombinedGeometry(mode, r.CreateRectangleGeometry(new Rect(0, 0, 60, 60)),
            r.CreateEllipseGeometry(new Rect(30, 20, 60, 60)));

    private static IGeometryImpl Stream(IPlatformRenderInterface r)
    {
        using (var ctx = r.CreateStreamGeometryBuilder())
        {
            ctx.SetFillRule(FillRule.NonZero);
            ctx.BeginFigure(new Point(10, 10), true);
            ctx.LineTo(new Point(80, 10));
            ctx.ArcTo(new Point(80, 70), new Size(30, 30), 0, false, SweepDirection.Clockwise);
            ctx.CubicBezierTo(new Point(60, 90), new Point(30, 50), new Point(10, 70));
            ctx.QuadraticBezierTo(new Point(0, 40), new Point(10, 10));
            ctx.EndFigure(true);
            ctx.BeginFigure(new Point(30, 30), true);
            ctx.ArcTo(new Point(50, 30), new Size(10, 15), 30, true, SweepDirection.CounterClockwise);
            ctx.EndFigure(false);
            return ctx.ToGeometry();
        }
    }

    private static IGeometryImpl UnstrokedStream(IPlatformRenderInterface r)
    {
        using (var ctx = r.CreateStreamGeometryBuilder())
        {
            ctx.BeginFigure(new Point(0, 0), true);
            ctx.LineTo(new Point(60, 0));
            ctx.LineTo(new Point(60, 60), isStroked: false);
            ctx.LineTo(new Point(0, 60));
            ctx.EndFigure(true);
            ctx.BeginFigure(new Point(70, 0), false);
            ctx.LineTo(new Point(100, 30));
            ctx.EndFigure(false);
            return ctx.ToGeometry();
        }
    }

    // Figures without segments only move the pen, but still count towards the bounds.
    private static IGeometryImpl TrailingMovesStream(IPlatformRenderInterface r)
    {
        using (var ctx = r.CreateStreamGeometryBuilder())
        {
            ctx.BeginFigure(new Point(10, 90), false);
            ctx.LineTo(new Point(90, 10));
            ctx.EndFigure(false);
            ctx.BeginFigure(new Point(0, 0), false);
            ctx.EndFigure(false);
            ctx.BeginFigure(new Point(100, 100), false);
            ctx.EndFigure(false);
            return ctx.ToGeometry();
        }
    }

    private static readonly IPen[] Pens =
    [
        new ImmutablePen(Brushes.Black, 6),
        new ImmutablePen(Brushes.Black, 4, new ImmutableDashStyle([3, 1, 1, 1], 1), PenLineCap.Round,
            PenLineJoin.Bevel),
        new ImmutablePen(Brushes.Black, 10, null, PenLineCap.Square, PenLineJoin.Miter, 2),
    ];

    private static IEnumerable<Point> Grid()
    {
        for (var y = -10.25; y < 110; y += 3)
            for (var x = -10.25; x < 110; x += 3)
                yield return new Point(x, y);
    }

    private static void AssertClose(Rect expected, Rect actual, double tolerance, string what)
    {
        Assert.True(Math.Abs(expected.X - actual.X) <= tolerance && Math.Abs(expected.Y - actual.Y) <= tolerance &&
                    Math.Abs(expected.Width - actual.Width) <= tolerance &&
                    Math.Abs(expected.Height - actual.Height) <= tolerance,
            $"{what}: expected {expected}, got {actual}");
    }

    // Hit tests agree except for points within a hair of an edge.
    private static void AssertHitTests(Func<Point, bool> expected, Func<Point, bool> actual, string what)
    {
        var total = 0;
        var mismatches = 0;
        foreach (var point in Grid())
        {
            total++;
            if (expected(point) != actual(point))
                mismatches++;
        }
        Assert.True(mismatches <= total / 200, $"{what}: {mismatches} of {total} hit tests differ");
    }

    [Theory]
    [MemberData(nameof(Geometries))]
    public void Queries_Match_Skia(string name, Func<IPlatformRenderInterface, IGeometryImpl> create)
    {
        var skia = create(Skia);
        var impeller = create(Impeller);

        AssertClose(skia.Bounds, impeller.Bounds, 0.5, $"{name} bounds");
        AssertHitTests(skia.FillContains, impeller.FillContains, $"{name} fill");

        foreach (var pen in Pens)
        {
            AssertClose(skia.GetRenderBounds(pen), impeller.GetRenderBounds(pen), 0.5, $"{name} render bounds");
            AssertHitTests(p => skia.StrokeContains(pen, p), p => impeller.StrokeContains(pen, p), $"{name} stroke");

            var skiaWidened = skia.GetWidenedGeometry(pen);
            var impellerWidened = impeller.GetWidenedGeometry(pen);
            AssertClose(skiaWidened.Bounds, impellerWidened.Bounds, 0.5, $"{name} widened bounds");
            AssertHitTests(skiaWidened.FillContains, impellerWidened.FillContains, $"{name} widened");
        }
    }

    [Theory]
    [MemberData(nameof(Geometries))]
    public void Measurements_Match_Skia(string name, Func<IPlatformRenderInterface, IGeometryImpl> create)
    {
        var skia = create(Skia);
        var impeller = create(Impeller);

        Assert.Equal(skia.ContourLength, impeller.ContourLength, 0.05);

        var length = skia.ContourLength;
        foreach (var fraction in new[] { 0.0, 0.1, 0.37, 0.5, 0.9, 1.0, 1.5 })
        {
            var distance = length * fraction;
            Assert.Equal(skia.TryGetPointAndTangentAtDistance(distance, out var skiaPoint, out var skiaTangent),
                impeller.TryGetPointAndTangentAtDistance(distance, out var impellerPoint, out var impellerTangent));
            Assert.True(((Vector)(skiaPoint - impellerPoint)).Length < 0.05, $"{name} point at {fraction}");
            Assert.True(((Vector)(skiaTangent - impellerTangent)).Length < 0.01, $"{name} tangent at {fraction}");

            Assert.Equal(skia.TryGetPointAtDistance(distance, out skiaPoint),
                impeller.TryGetPointAtDistance(distance, out impellerPoint));
            Assert.True(((Vector)(skiaPoint - impellerPoint)).Length < 0.05, $"{name} point at {fraction}");
        }

        foreach (var startOnBeginFigure in new[] { true, false })
        {
            var hasSkia = skia.TryGetSegment(length * 0.2, length * 0.7, startOnBeginFigure, out var skiaSegment);
            var hasImpeller = impeller.TryGetSegment(length * 0.2, length * 0.7, startOnBeginFigure,
                out var impellerSegment);
            Assert.Equal(hasSkia, hasImpeller);
            if (hasSkia)
            {
                AssertClose(skiaSegment!.Bounds, impellerSegment!.Bounds, 0.05, $"{name} segment");
                Assert.Equal(skiaSegment.ContourLength, impellerSegment.ContourLength, 0.05);
            }
        }
    }

    [Theory]
    [MemberData(nameof(Geometries))]
    public void Intersections_Match_Skia(string name, Func<IPlatformRenderInterface, IGeometryImpl> create)
    {
        foreach (var other in new[] { new Rect(0, 0, 200, 200), new Rect(40, 30, 10, 10), new Rect(300, 300, 5, 5) })
        {
            var skia = create(Skia);
            var impeller = create(Impeller);
            Assert.Equal(skia.GetFillIntersectionResult(Skia.CreateRectangleGeometry(other)),
                impeller.GetFillIntersectionResult(Impeller.CreateRectangleGeometry(other)));

            var skiaIntersection = skia.Intersect(Skia.CreateRectangleGeometry(other));
            var impellerIntersection = impeller.Intersect(Impeller.CreateRectangleGeometry(other));
            Assert.Equal(skiaIntersection is null, impellerIntersection is null);
            if (skiaIntersection is not null)
                AssertHitTests(skiaIntersection.FillContains, impellerIntersection!.FillContains, $"{name} intersect");
        }
    }

    [Fact]
    public void Stream_Geometry_Built_From_Another_Is_Independent()
    {
        var geometry = Stream(Impeller);
        IGeometryImpl extended;
        using (var ctx = Impeller.CreateStreamGeometryBuilder(geometry))
        {
            ctx.BeginFigure(new Point(200, 200), true);
            ctx.LineTo(new Point(220, 200));
            ctx.LineTo(new Point(220, 220));
            ctx.EndFigure(true);
            extended = ctx.ToGeometry();
        }

        Assert.True(extended.FillContains(new Point(218, 205)));
        Assert.False(geometry.FillContains(new Point(218, 205)));
        Assert.Equal(geometry.Bounds, Stream(Impeller).Bounds);
    }
}
