using System;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Rendering;

/// <summary>
/// Measures the recording / replay pipeline under workloads representative of
/// COLR v1 glyph drawing and SVG document rendering — both motivating
/// consumers for the API. Each benchmark targets one axis of cost.
/// All benchmarks run against the headless render interface so results
/// reflect the framework cost without GPU variance.
/// </summary>
[MemoryDiagnoser]
public class DrawingRecordingBenchmarks
{
    private static readonly IImmutableBrush s_brush = new ImmutableSolidColorBrush(Colors.Red);
    private static readonly IPen s_pen = new ImmutablePen(Brushes.Black, 1);

    private readonly DrawingContext _target;
    private DrawingRecording _smallRecording = null!;
    private DrawingRecording _mediumRecording = null!;
    private DrawingRecording _largeRecording = null!;
    private DrawingRecording _subRecording = null!;
    private DrawingRecording _parentWithSubs = null!;
    private DrawingRecording _layeredRecording = null!;

    public DrawingRecordingBenchmarks()
    {
        AvaloniaLocator.CurrentMutable.Bind<IPlatformRenderInterface>()
            .ToConstant(new HeadlessPlatformRenderInterface());
        _target = new PlatformDrawingContext(
            new HeadlessPlatformRenderInterface.HeadlessDrawingContextStub(), true);
    }

    [Params(5, 50, 500)]
    public int OpCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _smallRecording = CreateRecording(5);
        _mediumRecording = CreateRecording(50);
        _largeRecording = CreateRecording(500);

        // SVG <use> / glyph-cache pattern: one small recording referenced many
        // times by a parent recording.
        _subRecording = CreateRecording(20);
        _parentWithSubs = DrawingRecording.Create(ctx =>
        {
            for (int i = 0; i < 25; i++)
            {
                ctx.DrawRecording(_subRecording,
                    Matrix.CreateTranslation(i * 4.0, i * 4.0),
                    DrawingRecordingOwnership.Shared);
            }
        });

        // SVG <g filter><g opacity> pattern.
        _layeredRecording = DrawingRecording.Create(ctx =>
        {
            using (ctx.PushLayer(new LayerOptions
            {
                Opacity = 0.5,
                Effect = new ImmutableBlurEffect(2)
            }))
            {
                for (int i = 0; i < 50; i++)
                    ctx.DrawRectangle(s_brush, s_pen, new Rect(i, i, 10, 10));
            }
        });
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _smallRecording?.Dispose();
        _mediumRecording?.Dispose();
        _largeRecording?.Dispose();
        _parentWithSubs?.Dispose();
        _subRecording?.Dispose();
        _layeredRecording?.Dispose();
    }

    [Benchmark]
    public DrawingRecording CreateImmutable()
    {
        var rec = CreateRecording(OpCount);
        rec.Dispose();
        return rec;
    }

    [Benchmark]
    public void Replay()
    {
        var rec = PickRecording(OpCount);
        _target.DrawRecording(rec);
    }

    /// <summary>
    /// Baseline for <see cref="Replay"/>: emit the same draw operations
    /// directly into the target context, no recording in between. The
    /// difference between this and <see cref="Replay"/> is the per-call
    /// dispatch cost of <see cref="DrawingContext.DrawRecording(DrawingRecording)"/>.
    /// </summary>
    [Benchmark]
    public void DirectDraw()
    {
        for (int i = 0; i < OpCount; i++)
            _target.DrawRectangle(s_brush, s_pen, new Rect(i, i, 10, 10));
    }

    [Benchmark]
    public void Replay_With_SubRecordings()
    {
        _target.DrawRecording(_parentWithSubs);
    }

    [Benchmark]
    public Rect Bounds_Cached()
    {
        // Bounds is computed lazily on first read for immutable recordings
        // and cached. Steady-state cost is field access.
        return PickRecording(OpCount).Bounds;
    }

    [Benchmark]
    public Rect GetBounds_Matrix()
    {
        // Re-evaluated on every call; iterates items applying the matrix
        // per-item-bounds. Linear in item count.
        return PickRecording(OpCount).GetBounds(Matrix.CreateRotation(0.5));
    }

    [Benchmark]
    public bool HitTest_FirstItem()
    {
        // Hits the first drawn rect immediately — measures short-circuit cost.
        return PickRecording(OpCount).HitTest(new Point(5, 5));
    }

    [Benchmark]
    public bool HitTest_Miss()
    {
        // Misses everything — must visit every item.
        return PickRecording(OpCount).HitTest(new Point(-1000, -1000));
    }

    [Benchmark]
    public void Replay_Layered()
    {
        // Includes PushLayer with blur effect — exercise the layer probe + paint
        // composition path. Replays a fixed-size 50-item recording regardless
        // of OpCount.
        _target.DrawRecording(_layeredRecording);
    }

    private DrawingRecording PickRecording(int op) => op switch
    {
        5 => _smallRecording,
        50 => _mediumRecording,
        _ => _largeRecording
    };

    private static DrawingRecording CreateRecording(int opCount) =>
        DrawingRecording.Create(ctx =>
        {
            for (int i = 0; i < opCount; i++)
                ctx.DrawRectangle(s_brush, s_pen, new Rect(i, i, 10, 10));
        });
}
