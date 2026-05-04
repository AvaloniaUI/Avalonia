#nullable enable

using System;
using Avalonia.Collections.Pooled;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Rendering;

[MemoryDiagnoser]
public class CompositionHitTesting
{
    private const int CellSize = 8;
    private const int CellStride = 12;

    private CompositorTestServices? _services;
    private CompositionVisual? _rootVisual;
    private Point _hitPoint;
    private Point _globalHitPoint;
    private Border? _expectedHit;

    [Params(1024, 4096, 16384)]
    public int VisualCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var columns = (int)Math.Ceiling(Math.Sqrt(VisualCount));
        var rows = (VisualCount + columns - 1) / columns;
        var size = new Size(columns * CellStride, rows * CellStride);
        var canvas = new Canvas
        {
            Width = size.Width,
            Height = size.Height
        };

        for (var i = 0; i < VisualCount; i++)
        {
            var child = new Border
            {
                Width = CellSize,
                Height = CellSize,
                Background = Brushes.Red
            };

            Canvas.SetLeft(child, i % columns * CellStride);
            Canvas.SetTop(child, i / columns * CellStride);
            canvas.Children.Add(child);

            if (i == 0)
                _expectedHit = child;
        }

        _services = new CompositorTestServices(size);
        _services.TopLevel.Content = canvas;
        _services.RunJobs();

        _rootVisual = _services.TopLevel.CompositionVisual;
        _hitPoint = new Point(CellSize / 2d, CellSize / 2d);
        _globalHitPoint = _hitPoint * _services.Renderer.CompositionTarget.Scaling;

        if (!ReferenceEquals(HitTestFirst_RTree(), _expectedHit))
            throw new InvalidOperationException("R-tree hit test returned an unexpected visual.");

        if (!ReferenceEquals(HitTestFirst_Linear(), _expectedHit))
            throw new InvalidOperationException("Linear reference hit test returned an unexpected visual.");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _services?.Dispose();
        _services = null;
        _rootVisual = null;
        _expectedHit = null;
    }

    [Benchmark(Baseline = true)]
    public Visual? HitTestFirst_Linear()
    {
        _services!.Renderer.CompositionTarget.Server.Readback.NextRead();

        using var results = new PooledList<CompositionVisual>();
        LinearHitTestCore(_rootVisual!, _globalHitPoint, results);

        foreach (var visual in results)
        {
            if (visual is CompositionDrawListVisual drawListVisual)
                return drawListVisual.Visual;
        }

        return null;
    }

    [Benchmark]
    public Visual? HitTestFirst_RTree()
    {
        return _services!.Renderer.HitTestFirst(_hitPoint, _services.TopLevel, null);
    }

    private static void LinearHitTestCore(CompositionVisual visual, Point globalPoint, PooledList<CompositionVisual> result)
    {
        if (visual.Visible == false)
            return;

        if (!TryTransformTo(visual, globalPoint, out var point))
            return;

        if (visual.ClipToBounds
            && (point.X < 0 || point.Y < 0 || point.X > visual.Size.X || point.Y > visual.Size.Y))
            return;

        if (visual.Clip?.FillContains(point) == false)
            return;

        if (visual is CompositionContainerVisual container)
        {
            for (var c = container.Children.Count - 1; c >= 0; c--)
                LinearHitTestCore(container.Children[c], globalPoint, result);
        }

        if (visual.HitTest(point))
            result.Add(visual);
    }

    private static bool TryTransformTo(CompositionVisual visual, Point globalPoint, out Point point)
    {
        point = default;

        var matrix = visual.TryGetServerGlobalTransform();
        if (matrix == null || !matrix.Value.TryInvert(out var inverted))
            return false;

        point = globalPoint * inverted;
        return true;
    }
}
