using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace RendererBench.Scenes;

/// <summary>
/// A synthetic workload. Content depends only on the frame number, so both backends draw the same frames.
/// </summary>
internal abstract class Scene
{
    public static IReadOnlyDictionary<string, Func<double, Scene>> All { get; } =
        new Dictionary<string, Func<double, Scene>>
        {
            ["rects"] = s => new RectsScene(s),
            ["paths"] = s => new PathsScene(s),
            ["text-static"] = s => new TextScene(s, dynamic: false),
            ["text-dynamic"] = s => new TextScene(s, dynamic: true),
            ["gradients"] = s => new GradientsScene(s),
            ["images"] = s => new ImagesScene(s),
            ["layers"] = s => new LayersScene(s),
            ["effects"] = s => new EffectsScene(s),
        };

    protected Scene(double scale)
    {
        Scale = scale;
    }

    protected double Scale { get; }

    protected static IImmutableSolidColorBrush[] Palette { get; } = new[]
        {
            0xFFE53935, 0xFFD81B60, 0xFF8E24AA, 0xFF5E35B1, 0xFF3949AB, 0xFF1E88E5,
            0xFF039BE5, 0xFF00ACC1, 0xFF00897B, 0xFF43A047, 0xFF7CB342, 0xFFFDD835,
        }
        .Select(c => (IImmutableSolidColorBrush)new ImmutableSolidColorBrush(Color.FromUInt32(c)))
        .ToArray();

    protected int Count(int baseCount) => Math.Max(1, (int)(baseCount * Scale));

    public abstract Control Build();

    public abstract void Advance(long frame);
}

/// <summary>
/// A scene drawn by one control's render override.
/// </summary>
internal abstract class DrawnScene : Scene
{
    private readonly Surface _surface;

    protected DrawnScene(double scale) : base(scale)
    {
        _surface = new Surface(this);
    }

    protected long Frame { get; private set; }

    protected double Time => Frame / 60.0;

    public override Control Build() => _surface;

    public override void Advance(long frame)
    {
        Frame = frame;
        _surface.InvalidateVisual();
    }

    protected abstract void Draw(DrawingContext context, Size size);

    private sealed class Surface : Control
    {
        private readonly DrawnScene _scene;

        public Surface(DrawnScene scene)
        {
            _scene = scene;
            ClipToBounds = true;
        }

        public override void Render(DrawingContext context) => _scene.Draw(context, Bounds.Size);
    }
}
