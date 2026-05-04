#nullable enable

using System;
using System.Numerics;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace Avalonia.Benchmarks.Rendering;

class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddJob(Job.Default
            .WithMsBuildArguments("/p:DisableHitTestAabbTree=true")
            .WithId("Linear"));
        AddJob(Job.Default
            .WithMsBuildArguments("/p:DisableHitTestAabbTree=false")
            .WithId("AabbTree"));
        HideColumns(Column.Arguments);
    }
}

[MemoryDiagnoser]
[Config(typeof(BenchmarkConfig))]
public class CompositionHitTesting
{
    private const int CellSize = 8;
    private const int CellStride = 12;
    private const int TreeDepth = 4;

    private CompositorTestServices? _services;
    private Point _hitPoint;
    private Border? _expectedHit;

    [Params(1, 2, 4, 8, 16, 32, 64, 1024, 4096, 16384)]
    public int VisualCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var canvas = BuildGrid(VisualCount, out var size, out _expectedHit);

        _services = new CompositorTestServices(size);
        _services.TopLevel.Content = canvas;
        _services.RunJobs();

        _hitPoint = new Point(CellSize / 2d, CellSize / 2d);

        if (!ReferenceEquals(HitTestFirst(), _expectedHit))
            throw new InvalidOperationException("Hit test returned an unexpected visual.");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _services?.Dispose();
        _services = null;
        _expectedHit = null;
    }

    [Benchmark]
    public Visual? HitTestFirst()
    {
        return _services!.Renderer.HitTestFirst(_hitPoint, _services.TopLevel, null);
    }

    internal static Canvas BuildGrid(int visualCount, out Size size, out Border? firstChild)
    {
        var columns = (int)Math.Ceiling(Math.Sqrt(visualCount));
        var rows = (visualCount + columns - 1) / columns;
        size = new Size(columns * CellStride, rows * CellStride);
        firstChild = null;

        var root = new Canvas
        {
            Width = size.Width,
            Height = size.Height
        };

        var leafHost = root;
        for (var depth = 0; depth < TreeDepth; depth++)
        {
            var nested = new Canvas
            {
                Width = size.Width,
                Height = size.Height
            };
            leafHost.Children.Add(nested);
            leafHost = nested;
        }

        for (var i = 0; i < visualCount; i++)
        {
            var child = new Border
            {
                Width = CellSize,
                Height = CellSize,
                Background = Brushes.Red
            };

            Canvas.SetLeft(child, i % columns * CellStride);
            Canvas.SetTop(child, i / columns * CellStride);
            leafHost.Children.Add(child);

            if (i == 0)
                firstChild = child;
        }

        return root;
    }
}

[MemoryDiagnoser]
[Config(typeof(BenchmarkConfig))]
public class CompositionHitTestingAnimated
{
    private const int CellSize = 8;
    private const int CellStride = 12;
    private const int TreeDepth = 4;

    private CompositorTestServices? _services;
    private CompositionVisual? _animatedVisual;
    private Border? _expectedHit;
    private Point _hitPoint;

    [Params(1, 2, 4, 8, 16, 32, 64, 1024, 4096, 16384)]
    public int VisualCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var canvas = BuildDeepAnimatedGrid(VisualCount, out var size, out _expectedHit);

        _services = new CompositorTestServices(size);
        _services.TopLevel.Content = canvas;
        _services.RunJobs();

        _animatedVisual = _expectedHit!.CompositionVisual;
        StartOffsetAnimation();
        _services.RunJobs();
        UpdateHitPoint();

        if (!ReferenceEquals(HitTestAnimatedChild(), _expectedHit))
            throw new InvalidOperationException("Hit test returned an unexpected visual.");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _services?.Dispose();
        _services = null;
        _animatedVisual = null;
        _expectedHit = null;
    }

    [Benchmark]
    public Visual? HitTestAnimatedChild()
    {
        _services!.RunJobs();
        UpdateHitPoint();
        return _services.Renderer.HitTestFirst(_hitPoint, _services.TopLevel, null);
    }

    private void StartOffsetAnimation()
    {
        var animation = _animatedVisual!.Compositor.CreateVector3KeyFrameAnimation();
        animation.Target = "Offset";
        animation.InsertKeyFrame(0f, new Vector3(CellStride, CellStride, 0), new LinearEasing());
        animation.InsertKeyFrame(1f, new Vector3(CellStride * 3, CellStride * 3, 0), new LinearEasing());
        animation.Duration = TimeSpan.FromSeconds(1);
        animation.Direction = PlaybackDirection.Alternate;
        animation.IterationBehavior = AnimationIterationBehavior.Forever;
        _animatedVisual.StartAnimation("Offset", animation);
    }

    private void UpdateHitPoint()
    {
        var server = _animatedVisual!.Server;
        var bounds = server.GetReadback(server.Compositor.Readback.LastCompletedWrite)!.TransformedSubtreeBounds!.Value;
        _hitPoint = new Point((bounds.Left + bounds.Right) / 2, (bounds.Top + bounds.Bottom) / 2);
    }

    private static Canvas BuildDeepAnimatedGrid(int visualCount, out Size size, out Border target)
    {
        var branchCount = Math.Min(8, Math.Max(1, visualCount / 64));
        var leavesPerBranch = (visualCount + branchCount - 1) / branchCount;
        var columns = (int)Math.Ceiling(Math.Sqrt(leavesPerBranch + 1));
        var rows = (leavesPerBranch + columns - 1) / columns;
        var branchSize = new Size(columns * CellStride + CellStride * 4, rows * CellStride + CellStride * 4);

        size = new Size(branchSize.Width * branchCount, branchSize.Height);
        target = null!;

        var root = new Canvas
        {
            Width = size.Width,
            Height = size.Height
        };

        var remaining = visualCount;
        for (var branch = 0; branch < branchCount; branch++)
        {
            var branchRoot = new Canvas
            {
                Width = branchSize.Width,
                Height = branchSize.Height
            };
            Canvas.SetLeft(branchRoot, branch * branchSize.Width);
            root.Children.Add(branchRoot);

            var leafHost = branchRoot;
            for (var depth = 0; depth < TreeDepth; depth++)
            {
                var nested = new Canvas
                {
                    Width = branchSize.Width,
                    Height = branchSize.Height
                };
                leafHost.Children.Add(nested);
                leafHost = nested;
            }

            var count = Math.Min(leavesPerBranch, remaining);
            remaining -= count;

            if (branch == 0)
                count--;

            for (var i = 0; i < count; i++)
            {
                var child = new Border
                {
                    Width = CellSize,
                    Height = CellSize,
                    Background = Brushes.Red
                };

                Canvas.SetLeft(child, (i % columns) * CellStride);
                Canvas.SetTop(child, (i / columns) * CellStride);
                leafHost.Children.Add(child);
            }

            if (branch == 0)
            {
                target = new Border
                {
                    Width = CellSize,
                    Height = CellSize,
                    Background = Brushes.Blue
                };

                Canvas.SetLeft(target, CellStride);
                Canvas.SetTop(target, CellStride);
                leafHost.Children.Add(target);
            }
        }

        return root;
    }
}
