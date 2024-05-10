using System;
using System.Collections.Generic;
using System.Numerics;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Reactive;

namespace Avalonia.Rendering.Composition.Server;

internal interface IDirtyRectTracker
{
    void AddRect(LtrbPixelRect rect);
    IDisposable BeginDraw(IDrawingContextImpl ctx);
    bool IsEmpty { get; }
    bool Intersects(LtrbRect rect);
    bool Contains(Point pt);
    void Reset();
    void Visualize(IDrawingContextImpl context);
    LtrbPixelRect CombinedRect { get; }
    IList<LtrbPixelRect> Rects { get; }
}

internal class DirtyRectTracker : IDirtyRectTracker
{
    private LtrbPixelRect _rect;
    private Rect _doubleRect;
    private LtrbRect _normalRect;
    private LtrbPixelRect[] _rectsForApi = new LtrbPixelRect[1];
    private Random _random = new();
    public void AddRect(LtrbPixelRect rect)
    {
        _rect = _rect.Union(rect);
    }
    
    public IDisposable BeginDraw(IDrawingContextImpl ctx)
    {
        ctx.PushClip(_rect.ToRectWithNoScaling());
        _doubleRect = _rect.ToRectWithNoScaling();
        _normalRect = new(_doubleRect);
        return Disposable.Create(ctx.PopClip);
    }

    public bool IsEmpty => _rect.IsEmpty;
    public bool Intersects(LtrbRect rect) => _normalRect.Intersects(rect);
    public bool Contains(Point pt) => _rect.Contains((int)pt.X, (int)pt.Y);

    public void Reset() => _rect = default;
    public void Visualize(IDrawingContextImpl context)
    {
        context.DrawRectangle(
            new ImmutableSolidColorBrush(
                new Color(30, (byte)_random.Next(255), (byte)_random.Next(255), (byte)_random.Next(255))),
            null, _doubleRect);
    }

    public LtrbPixelRect CombinedRect => _rect;

    public IList<LtrbPixelRect> Rects
    {
        get
        {
            if (_rect.IsEmpty)
                return Array.Empty<LtrbPixelRect>();
            _rectsForApi[0] = _rect;
            return _rectsForApi;
        }
    }
}

internal class RegionDirtyRectTracker : IDirtyRectTracker
{
    private readonly IPlatformRenderInterfaceRegion _region;
    private Random _random = new();

    public RegionDirtyRectTracker(IPlatformRenderInterface platformRender)
    {
        _region = platformRender.CreateRegion();
    }

    public void AddRect(LtrbPixelRect rect) => _region.AddRect(rect);

    public IDisposable BeginDraw(IDrawingContextImpl ctx)
    {
        ctx.PushClip(_region);
        return Disposable.Create(ctx.PopClip);
    }

    public bool IsEmpty => _region.IsEmpty;
    public bool Intersects(LtrbRect rect) => _region.Intersects(rect);
    public bool Contains(Point pt) => _region.Contains(pt);

    public void Reset() => _region.Reset();

    public void Visualize(IDrawingContextImpl context)
    {
        context.DrawRegion(
            new ImmutableSolidColorBrush(
                new Color(150, (byte)_random.Next(255), (byte)_random.Next(255), (byte)_random.Next(255))),
            null, _region);
    }

    public LtrbPixelRect CombinedRect => _region.Bounds;
    public IList<LtrbPixelRect> Rects => _region.Rects;
}
