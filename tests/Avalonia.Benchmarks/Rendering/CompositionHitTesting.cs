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
        _services!.Renderer.CompositionTarget.Server.Compositor.Readback.NextRead();

        var rootReadback = _rootVisual!.TryGetValidReadback();
        if (rootReadback == null)
            return null;

        using var results = new PooledList<CompositionVisual>();
        LinearHitTestCore(_rootVisual, _hitPoint.Transform(rootReadback.Matrix), results);

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

    private static void LinearHitTestCore(CompositionVisual visual, Point parentPoint, PooledList<CompositionVisual> result)
    {
        if (visual.Visible == false)
            return;

        var readback = visual.TryGetValidReadback();
        if (readback == null)
            return;

        if (!visual.DisableSubTreeBoundsHitTestOptimization &&
            (readback.TransformedSubtreeBounds == null ||
             !readback.TransformedSubtreeBounds.Value.Contains(parentPoint)))
            return;

        if (!readback.Matrix.TryInvert(out var inverted))
            return;

        var point = parentPoint.Transform(inverted);

        if (visual.ClipToBounds
            && (point.X < 0 || point.Y < 0 || point.X > visual.Size.X || point.Y > visual.Size.Y))
            return;

        if (visual.Clip?.FillContains(point) == false)
            return;

        if (visual is CompositionContainerVisual container)
        {
            for (var c = container.Children.Count - 1; c >= 0; c--)
                LinearHitTestCore(container.Children[c], point, result);
        }

        if (visual.HitTest(point))
            result.Add(visual);
    }
}
