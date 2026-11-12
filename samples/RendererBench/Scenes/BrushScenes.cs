using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace RendererBench.Scenes;

/// <summary>
/// Rotating rectangles with cached linear, radial and conic gradients, plus gradients rebuilt every frame.
/// </summary>
internal sealed class GradientsScene : DrawnScene
{
    private readonly IBrush[] _brushes;
    private readonly (double X, double Y, double W, double H)[] _items;
    private readonly int _dynamicCount;

    public GradientsScene(double scale) : base(scale)
    {
        var rng = new Random(4);
        var brushes = new List<IBrush>();
        for (var i = 0; i < 24; i++)
        {
            var stops = new GradientStops();
            var stopCount = 2 + i % 4;
            for (var s = 0; s < stopCount; s++)
                stops.Add(new GradientStop(Palette[(i + s * 3) % Palette.Length].Color, s / (stopCount - 1.0)));

            GradientBrush brush = (i % 3) switch
            {
                0 => new LinearGradientBrush
                {
                    StartPoint = RelativePoint.TopLeft, EndPoint = RelativePoint.BottomRight
                },
                1 => new RadialGradientBrush(),
                _ => new ConicGradientBrush { Angle = i * 15 },
            };
            brush.GradientStops = stops;
            brushes.Add(brush.ToImmutable());
        }

        _brushes = brushes.ToArray();
        _items = new (double, double, double, double)[Count(500)];
        for (var i = 0; i < _items.Length; i++)
            _items[i] = (rng.NextDouble(), rng.NextDouble(), 30 + rng.Next(90), 30 + rng.Next(90));
        _dynamicCount = Count(40);
    }

    protected override void Draw(DrawingContext context, Size size)
    {
        for (var i = 0; i < _items.Length; i++)
        {
            var (x, y, w, h) = _items[i];
            var matrix = Matrix.CreateRotation(Time * 0.4 + i) *
                         Matrix.CreateTranslation(x * size.Width, y * size.Height);
            using (context.PushTransform(matrix))
                context.DrawRectangle(_brushes[i % _brushes.Length], null, new Rect(-w / 2, -h / 2, w, h));
        }

        for (var i = 0; i < _dynamicCount; i++)
        {
            var offset = (Math.Sin(Time * 2 + i) + 1) / 2;
            var brush = new LinearGradientBrush
            {
                GradientStops =
                {
                    new GradientStop(Palette[i % Palette.Length].Color, 0),
                    new GradientStop(Colors.White, offset),
                    new GradientStop(Palette[(i + 5) % Palette.Length].Color, 1),
                }
            };
            context.DrawRectangle(brush, null, new Rect(20 + i % 10 * 120, 20 + i / 10 * 120, 100, 100));
        }
    }
}

/// <summary>
/// Scaled and rotated bitmaps at each interpolation mode over a scrolling tiled image brush.
/// </summary>
internal sealed class ImagesScene : DrawnScene
{
    private static readonly BitmapInterpolationMode[] s_modes =
    {
        BitmapInterpolationMode.LowQuality, BitmapInterpolationMode.MediumQuality,
        BitmapInterpolationMode.HighQuality,
    };

    private readonly WriteableBitmap _bitmap;
    private readonly ImageBrush _tiles;
    private readonly TranslateTransform _tileOffset = new();
    private readonly (double X, double Y, double Size)[] _items;

    public ImagesScene(double scale) : base(scale)
    {
        _bitmap = CreateBitmap();
        _tiles = new ImageBrush(_bitmap)
        {
            TileMode = TileMode.Tile,
            DestinationRect = new RelativeRect(0, 0, 64, 64, RelativeUnit.Absolute),
            Transform = _tileOffset,
        };

        var rng = new Random(5);
        _items = new (double, double, double)[Count(600)];
        for (var i = 0; i < _items.Length; i++)
            _items[i] = (rng.NextDouble(), rng.NextDouble(), 32 + rng.Next(128));
    }

    private static unsafe WriteableBitmap CreateBitmap()
    {
        const int size = 256;
        var bitmap = new WriteableBitmap(new PixelSize(size, size), new Vector(96, 96), PixelFormat.Bgra8888,
            AlphaFormat.Premul);
        using var fb = bitmap.Lock();
        for (var y = 0; y < size; y++)
        {
            var row = (uint*)((byte*)fb.Address + y * fb.RowBytes);
            for (var x = 0; x < size; x++)
            {
                var checker = ((x / 16) + (y / 16)) % 2 == 0;
                var r = (uint)x;
                var g = (uint)y;
                var b = checker ? 255u : (uint)((x ^ y) & 0xFF);
                row[x] = 0xFF000000 | (r << 16) | (g << 8) | b;
            }
        }

        return bitmap;
    }

    protected override void Draw(DrawingContext context, Size size)
    {
        _tileOffset.X = Time * 30 % 64;
        _tileOffset.Y = Time * 20 % 64;
        context.DrawRectangle(_tiles, null, new Rect(size));

        var source = new Rect(_bitmap.Size);
        for (var i = 0; i < _items.Length; i++)
        {
            var (x, y, s) = _items[i];
            var matrix = Matrix.CreateRotation(Time * 0.3 + i) *
                         Matrix.CreateTranslation(x * size.Width, y * size.Height);
            using (context.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = s_modes[i % 3] }))
            using (context.PushTransform(matrix))
                context.DrawImage(_bitmap, source, new Rect(-s / 2, -s / 2, s, s));
        }
    }
}
