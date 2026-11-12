using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace RendererBench.Scenes;

/// <summary>
/// Groups with nested opacity, rounded clips, geometry clips and opacity masks.
/// </summary>
internal sealed class LayersScene : DrawnScene
{
    private readonly (double X, double Y)[] _groups;
    private readonly IBrush _mask;
    private readonly Geometry _clip = new EllipseGeometry(new Rect(0, 0, 120, 120));

    public LayersScene(double scale) : base(scale)
    {
        var rng = new Random(6);
        _groups = new (double, double)[Count(150)];
        for (var i = 0; i < _groups.Length; i++)
            _groups[i] = (rng.NextDouble(), rng.NextDouble());

        _mask = new LinearGradientBrush
        {
            StartPoint = RelativePoint.TopLeft,
            EndPoint = RelativePoint.BottomRight,
            GradientStops = { new GradientStop(Colors.Black, 0), new GradientStop(Colors.Transparent, 1) }
        }.ToImmutable();
    }

    protected override void Draw(DrawingContext context, Size size)
    {
        var bounds = new Rect(0, 0, 120, 120);
        for (var i = 0; i < _groups.Length; i++)
        {
            var (x, y) = _groups[i];
            var origin = new Point(x * size.Width - 60 + Math.Sin(Time + i) * 25, y * size.Height - 60);
            using (context.PushTransform(Matrix.CreateTranslation(origin.X, origin.Y)))
            using (context.PushOpacity(0.6 + 0.3 * Math.Sin(Time + i)))
            using (context.PushClip(new RoundedRect(bounds, 16)))
            using (i % 3 == 0 ? context.PushOpacityMask(_mask, bounds) : default)
            using (i % 5 == 0 ? context.PushGeometryClip(_clip) : default)
            {
                for (var r = 0; r < 4; r++)
                    context.DrawRectangle(Palette[(i + r) % Palette.Length], null,
                        new Rect(r * 30 - 10, r * 25, 80, 50));

                using (context.PushOpacity(0.5))
                    context.DrawEllipse(Palette[(i + 7) % Palette.Length], null, new Point(60, 60), 45, 45);
            }
        }
    }
}

/// <summary>
/// Moving borders with box shadows, blur effects and drop shadow effects.
/// </summary>
internal sealed class EffectsScene : Scene
{
    private readonly Canvas _canvas = new();
    private readonly (TranslateTransform Transform, double X, double Y)[] _items;

    public EffectsScene(double scale) : base(scale)
    {
        var rng = new Random(7);
        var shadow = BoxShadows.Parse("0 6 18 0 #60000000");
        _items = new (TranslateTransform, double, double)[Count(48)];
        for (var i = 0; i < _items.Length; i++)
        {
            var transform = new TranslateTransform();
            var border = new Border
            {
                Width = 160,
                Height = 100,
                CornerRadius = new CornerRadius(12),
                Background = Palette[i % Palette.Length],
                BoxShadow = shadow,
                RenderTransform = transform,
                Effect = (i % 3) switch
                {
                    0 => new BlurEffect { Radius = 6 },
                    1 => new DropShadowEffect { BlurRadius = 12, OffsetX = 4, OffsetY = 4 },
                    _ => null,
                },
            };
            _canvas.Children.Add(border);
            _items[i] = (transform, rng.NextDouble(), rng.NextDouble());
        }
    }

    public override Control Build() => _canvas;

    public override void Advance(long frame)
    {
        var time = frame / 60.0;
        var size = _canvas.Bounds.Size;
        for (var i = 0; i < _items.Length; i++)
        {
            var (transform, x, y) = _items[i];
            transform.X = x * (size.Width - 160) + Math.Sin(time + i) * 40;
            transform.Y = y * (size.Height - 100) + Math.Cos(time * 0.8 + i) * 40;
        }
    }
}
