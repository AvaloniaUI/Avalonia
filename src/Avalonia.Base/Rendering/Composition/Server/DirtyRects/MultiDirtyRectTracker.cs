using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Reactive;

namespace Avalonia.Rendering.Composition.Server;


internal partial class MultiDirtyRectTracker : IDirtyRectTracker
{
    private readonly double _maxOverhead;
    private readonly CDirtyRegion2 _regions;
    private readonly IPlatformRenderInterfaceRegion _clipRegion;
    private readonly List<LtrbRect> _inflatedRects = new();
    private Random _random = new();

    public MultiDirtyRectTracker(IPlatformRenderInterface platformRender, int maxDirtyRects, double maxOverhead)
    {
        _maxOverhead = maxOverhead;
        _regions = new CDirtyRegion2(maxDirtyRects);
        _clipRegion = platformRender.CreateRegion();
    }
    
    public void AddRect(LtrbRect rect) => _regions.Add(rect);

    public void FinalizeFrame(LtrbRect bounds)
    {
        _inflatedRects.Clear();
        _clipRegion.Reset();

        var dirtyRegions = _regions.GetUninflatedDirtyRegions();
        
        LtrbRect? combined = default;
        foreach (var rect in dirtyRegions)
        {
            var inflated = rect.Inflate(new(1)).IntersectOrEmpty(bounds);
            _inflatedRects.Add(inflated);
            _clipRegion.AddRect(LtrbPixelRect.FromRectUnscaled(inflated));
            combined = LtrbRect.FullUnion(combined, inflated);
        }

        CombinedRect = combined ?? default;
    }

    public IDisposable BeginDraw(IDrawingContextImpl ctx)
    {
        ctx.PushClip(_clipRegion);
        return Disposable.Create(ctx.PopClip);
    }

    public bool IsEmpty => _regions.IsEmpty;

    public bool Intersects(LtrbRect rect)
    {
        foreach(var r in _inflatedRects)
        {
            if (r.Intersects(rect))
                return true;
        }

        return false;
    }

    public void Initialize(LtrbRect bounds)
    {
        
        _regions.Initialize(bounds, _maxOverhead);
        _inflatedRects.Clear();
        _clipRegion.Reset();
        CombinedRect = default;
    }

    public void Visualize(IDrawingContextImpl context)
    {
        context.DrawRegion(
            new ImmutableSolidColorBrush(
                new Color(150, (byte)_random.Next(255), (byte)_random.Next(255), (byte)_random.Next(255))),
            null, _clipRegion);
    }

    public LtrbRect CombinedRect { get; private set; }
    
    public IReadOnlyList<LtrbRect> InflatedRects => _inflatedRects;
}
