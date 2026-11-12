using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace RendererBench.Scenes;

/// <summary>
/// Many small filled rounded rectangles, a quarter of them stroked.
/// </summary>
internal sealed class RectsScene : DrawnScene
{
    private readonly (double X, double Y, double W, double H, double R)[] _items;
    private readonly ImmutablePen _pen = new(0xFF000000, 1);

    public RectsScene(double scale) : base(scale)
    {
        var rng = new Random(1);
        _items = new (double, double, double, double, double)[Count(4000)];
        for (var i = 0; i < _items.Length; i++)
            _items[i] = (rng.NextDouble(), rng.NextDouble(), 8 + rng.NextDouble() * 32, 8 + rng.NextDouble() * 32,
                rng.NextDouble() * 8);
    }

    protected override void Draw(DrawingContext context, Size size)
    {
        for (var i = 0; i < _items.Length; i++)
        {
            var (x, y, w, h, r) = _items[i];
            var dx = Math.Sin(Time * 1.3 + i) * 40;
            var dy = Math.Cos(Time * 0.7 + i * 0.5) * 40;
            var rect = new Rect(x * size.Width + dx, y * size.Height + dy, w, h);
            context.DrawRectangle(Palette[i % Palette.Length], i % 4 == 0 ? _pen : null, new RoundedRect(rect, r));
        }
    }
}

/// <summary>
/// Cached bezier paths with solid and dashed strokes, stroked ellipses, and stars rebuilt every frame.
/// </summary>
internal sealed class PathsScene : DrawnScene
{
    private readonly (Geometry Geometry, double X, double Y)[] _paths;
    private readonly (double X, double Y, double R)[] _ellipses;
    private readonly int _dynamicCount;
    private readonly ImmutablePen[] _pens;

    public PathsScene(double scale) : base(scale)
    {
        var rng = new Random(2);
        _paths = new (Geometry, double, double)[Count(200)];
        for (var i = 0; i < _paths.Length; i++)
        {
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(new Point(rng.Next(200), rng.Next(200)), false);
                for (var s = 0; s < 4; s++)
                    ctx.CubicBezierTo(new Point(rng.Next(200), rng.Next(200)),
                        new Point(rng.Next(200), rng.Next(200)), new Point(rng.Next(200), rng.Next(200)));
                ctx.EndFigure(false);
            }

            _paths[i] = (geometry, rng.NextDouble(), rng.NextDouble());
        }

        _ellipses = new (double, double, double)[Count(600)];
        for (var i = 0; i < _ellipses.Length; i++)
            _ellipses[i] = (rng.NextDouble(), rng.NextDouble(), 4 + rng.NextDouble() * 30);

        _dynamicCount = Count(30);
        var dash = new ImmutableDashStyle(new double[] { 4, 2 }, 0);
        _pens = new[]
        {
            new ImmutablePen(Palette[0], 2, lineCap: PenLineCap.Round),
            new ImmutablePen(Palette[5], 3, dash),
            new ImmutablePen(Palette[9], 1),
        };
    }

    protected override void Draw(DrawingContext context, Size size)
    {
        for (var i = 0; i < _paths.Length; i++)
        {
            var (geometry, x, y) = _paths[i];
            var matrix = Matrix.CreateRotation(Time * 0.5 + i) *
                         Matrix.CreateTranslation(x * size.Width, y * size.Height);
            using (context.PushTransform(matrix))
                context.DrawGeometry(null, _pens[i % _pens.Length], geometry);
        }

        for (var i = 0; i < _ellipses.Length; i++)
        {
            var (x, y, r) = _ellipses[i];
            var center = new Point(x * size.Width + Math.Sin(Time + i) * 20, y * size.Height);
            context.DrawEllipse(null, _pens[(i + 1) % _pens.Length], center, r, r * 0.6);
        }

        for (var i = 0; i < _dynamicCount; i++)
        {
            var star = new StreamGeometry();
            using (var ctx = star.Open())
            {
                const int points = 10;
                for (var p = 0; p < points; p++)
                {
                    var radius = p % 2 == 0 ? 40 + Math.Sin(Time * 3 + i) * 15 : 18;
                    var angle = p * Math.PI * 2 / points + Time;
                    var pt = new Point(Math.Cos(angle) * radius, Math.Sin(angle) * radius);
                    if (p == 0)
                        ctx.BeginFigure(pt, true);
                    else
                        ctx.LineTo(pt);
                }

                ctx.EndFigure(true);
            }

            var col = i % 6;
            using (context.PushTransform(Matrix.CreateTranslation(60 + col * 100, 60 + i / 6 * 100)))
                context.DrawGeometry(Palette[i % Palette.Length], _pens[2], star);
        }
    }
}
