using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Media.TextFormatting;
using Avalonia.OpenGL;
using Avalonia.Platform;
using SkiaSharp;
using Xunit;

namespace Avalonia.Impeller.UnitTests;

/// <summary>Impeller's drawing must cover the same pixels as Skia's.</summary>
public class RenderingParityTests
{
    private const int Size = 100;

    public static IEnumerable<object[]> Drawings() =>
    [
        ["ellipse", (Action<IDrawingContextImpl, IPlatformRenderInterface>)((c, r) =>
            c.DrawGeometry(Brushes.Black, null, r.CreateEllipseGeometry(new Rect(10, 20, 80, 50))))],
        ["arcs and curves", (Action<IDrawingContextImpl, IPlatformRenderInterface>)((c, r) =>
            c.DrawGeometry(Brushes.Black, null, Curves(r)))],
        ["even-odd group", (Action<IDrawingContextImpl, IPlatformRenderInterface>)((c, r) =>
            c.DrawGeometry(Brushes.Black, null, r.CreateGeometryGroup(FillRule.EvenOdd,
                [r.CreateRectangleGeometry(new Rect(5, 5, 60, 60)), r.CreateEllipseGeometry(new Rect(25, 25, 60, 60))])))],
        ["xor", (Action<IDrawingContextImpl, IPlatformRenderInterface>)((c, r) =>
            c.DrawGeometry(Brushes.Black, null, r.CreateCombinedGeometry(GeometryCombineMode.Xor,
                r.CreateRectangleGeometry(new Rect(5, 5, 60, 60)), r.CreateEllipseGeometry(new Rect(30, 20, 60, 60)))))],
        ["stroke", (Action<IDrawingContextImpl, IPlatformRenderInterface>)((c, r) =>
            c.DrawGeometry(null, new ImmutablePen(Brushes.Black, 6, null, PenLineCap.Round, PenLineJoin.Round),
                Curves(r)))],
        ["dashed stroke", (Action<IDrawingContextImpl, IPlatformRenderInterface>)((c, r) =>
            c.DrawGeometry(null, new ImmutablePen(Brushes.Black, 4, new ImmutableDashStyle([3, 1, 1, 1], 1)),
                Curves(r)))],
        ["dashed rectangle", (Action<IDrawingContextImpl, IPlatformRenderInterface>)((c, _) =>
            c.DrawRectangle(null, new ImmutablePen(Brushes.Black, 3, new ImmutableDashStyle([2, 2], 0)),
                new RoundedRect(new Rect(10, 10, 80, 60), 10)))],
        // Skia boosts the contrast of glyph masks, so compare against Skia filling the same outlines.
        ["text", (Action<IDrawingContextImpl, IPlatformRenderInterface>)DrawText],
    ];

    private static IGeometryImpl Curves(IPlatformRenderInterface r)
    {
        using var ctx = r.CreateStreamGeometryBuilder();
        ctx.BeginFigure(new Point(10, 10), true);
        ctx.LineTo(new Point(80, 10));
        ctx.ArcTo(new Point(80, 70), new Size(30, 30), 0, false, SweepDirection.Clockwise);
        ctx.CubicBezierTo(new Point(60, 90), new Point(30, 50), new Point(10, 70));
        ctx.QuadraticBezierTo(new Point(0, 40), new Point(10, 10));
        ctx.EndFigure(true);
        return ctx.ToGeometry();
    }

    private static void DrawText(IDrawingContextImpl context, IPlatformRenderInterface render)
    {
        var typeface = Typeface.Default.GlyphTypeface;
        var shaped = TextShaper.Current.ShapeText("Agj&",
            new TextShaperOptions(typeface, 36, 0, CultureInfo.InvariantCulture));
        var run = new GlyphRun(typeface, 36, "Agj&".AsMemory(), shaped, new Point(8, 60));
        if (context is ImpellerDrawingContext)
            context.DrawGlyphRun(Brushes.Black, run.PlatformImpl.Item);
        else
            context.DrawGeometry(Brushes.Black, null, render.BuildGlyphRunGeometry(run));
    }

    private static byte[] Alpha(IImpellerOffscreenLayer layer)
    {
        using var image = layer.Snapshot();
        using var bitmap = SKBitmap.FromImage(image);
        var alpha = new byte[Size * Size];
        for (var y = 0; y < Size; y++)
            for (var x = 0; x < Size; x++)
                alpha[y * Size + x] = bitmap.GetPixel(x, y).Alpha;
        return alpha;
    }

    [Theory]
    [MemberData(nameof(Drawings))]
    public void Drawing_Matches_Skia(string name, Action<IDrawingContextImpl, IPlatformRenderInterface> draw)
    {
        var graphics = ImpellerTestApp.GlGraphics;
        Assert.SkipWhen(graphics is null, "Needs headless OpenGL ES.");
        using var app = ImpellerTestApp.Start();
        var render = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();
        using var glContext = (IGlContext)graphics!.CreateContext();
        using var current = glContext.EnsureCurrent();
        using var backend = ImpellerGlBackendContext.Create(glContext);

        using var impeller = backend.CreateTextureLayer(new PixelSize(Size, Size), new Vector(96, 96));
        using (var context = impeller.CreateDrawingContext())
            draw(context, render);
        using var skia = new ImpellerSkiaRasterLayer(new PixelSize(Size, Size), new Vector(96, 96));
        using (var context = skia.CreateDrawingContext())
            draw(context, render);

        var expected = Alpha(skia);
        var actual = Alpha(impeller);
        var covered = 0;
        var mismatches = 0;
        for (var i = 0; i < expected.Length; i++)
        {
            if (expected[i] > 0 || actual[i] > 0)
                covered++;
            if (Math.Abs(expected[i] - actual[i]) > 128)
                mismatches++;
        }

        Assert.True(covered > 50, $"{name}: nothing was drawn.");
        Assert.True(mismatches <= covered / 20, $"{name}: {mismatches} of {covered} pixels differ.");
    }
}
