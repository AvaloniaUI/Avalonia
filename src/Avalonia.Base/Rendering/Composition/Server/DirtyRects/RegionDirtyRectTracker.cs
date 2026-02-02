using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Reactive;

namespace Avalonia.Rendering.Composition.Server;

internal class RegionDirtyRectTracker : IDirtyRectTracker
{
    private readonly IPlatformRenderInterfaceRegion _region;
    private readonly List<LtrbRect> _rects = new();
    private Random _random = new();

    public RegionDirtyRectTracker(IPlatformRenderInterface platformRender)
    {
        _region = platformRender.CreateRegion();
    }
    
    public void AddRect(LtrbRect rect) => _rects.Add(rect);
    public LtrbRect? UninflatedCombinedIntersect(LtrbRect rect)
    {
        // We maybe want to disallow backdrop effects when this tracker is used this mode,
        // since that's rather expensive, but so is SKRegion
        LtrbRect? combined = null;
        foreach (var rc in _rects) 
            combined = LtrbRect.FullUnion(combined, rc.IntersectOrNull(rect));
        
        return combined;
    }

    public bool UninflatedIntersects(LtrbRect rect)
    {
        foreach (var rc in _rects) 
            if (rc.Intersects(rect))
                return true;
        return false;
    }

    private LtrbPixelRect GetInflatedPixelRect(LtrbRect rc)
    {
        var inflated = rc.Inflate(new Thickness(1)).IntersectOrEmpty(rc);
        var pixelRect = LtrbPixelRect.FromRectUnscaled(inflated);
        return pixelRect;
    }
    
    public void FinalizeFrame(LtrbRect bounds)
    {
        _region.Reset();
        foreach (var rc in _rects) 
            _region.AddRect(GetInflatedPixelRect(rc));
        CombinedRect = _region.Bounds.ToLtrbRectUnscaled();
    }

    public IDisposable BeginDraw(IDrawingContextImpl ctx)
    {
        ctx.PushClip(_region);
        return Disposable.Create(ctx.PopClip);
    }

    public bool IsEmpty => _rects.Count == 0;

    public bool Intersects(LtrbRect rect) => _region.Intersects(rect);

    public void Initialize(LtrbRect bounds) => _rects.Clear();

    public void Visualize(IDrawingContextImpl context)
    {
        context.DrawRegion(
            new ImmutableSolidColorBrush(
                new Color(150, (byte)_random.Next(255), (byte)_random.Next(255), (byte)_random.Next(255))),
            null, _region);
    }

    public LtrbRect CombinedRect { get; private set; }
    
}