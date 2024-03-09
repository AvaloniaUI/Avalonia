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
    void AddRect(PixelRect rect);
    IDisposable BeginDraw(IDrawingContextImpl ctx);
    bool IsEmpty { get; }
    bool Intersects(Rect rect);
    bool Contains(Point pt);
    void Reset();
    void Visualize(IDrawingContextImpl context);
    PixelRect CombinedRect { get; }
    IList<PixelRect> Rects { get; }
}

internal class DirtyRectTracker : IDirtyRectTracker
{
    private PixelRect _rect;
    private Rect _doubleRect;
    private PixelRect[] _rectsForApi = new PixelRect[1];
    private Random _random = new();
    public void AddRect(PixelRect rect)
    {
        _rect = _rect.Union(rect);
    }
    
    public IDisposable BeginDraw(IDrawingContextImpl ctx)
    {
        ctx.PushClip(_rect.ToRect(1));
        _doubleRect = _rect.ToRect(1);
        return Disposable.Create(ctx.PopClip);
    }

    public bool IsEmpty => _rect.Width == 0 | _rect.Height == 0;
    public bool Intersects(Rect rect) => _doubleRect.Intersects(rect);
    public bool Contains(Point pt) => _rect.Contains(PixelPoint.FromPoint(pt, 1));

    public void Reset() => _rect = default;
    public void Visualize(IDrawingContextImpl context)
    {
        context.DrawRectangle(
            new ImmutableSolidColorBrush(
                new Color(30, (byte)_random.Next(255), (byte)_random.Next(255), (byte)_random.Next(255))),
            null, _doubleRect);
    }

    public PixelRect CombinedRect => _rect;

    public IList<PixelRect> Rects
    {
        get
        {
            if (_rect.Width == 0 || _rect.Height == 0)
                return Array.Empty<PixelRect>();
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

    public void AddRect(PixelRect rect) => _region.AddRect(rect);

    public IDisposable BeginDraw(IDrawingContextImpl ctx)
    {
        ctx.PushClip(_region);
        return Disposable.Create(ctx.PopClip);
    }

    public bool IsEmpty => _region.IsEmpty;
    public bool Intersects(Rect rect) => _region.Intersects(rect);
    public bool Contains(Point pt) => _region.Contains(pt);

    public void Reset() => _region.Reset();

    public void Visualize(IDrawingContextImpl context)
    {
        context.DrawRegion(
            new ImmutableSolidColorBrush(
                new Color(150, (byte)_random.Next(255), (byte)_random.Next(255), (byte)_random.Next(255))),
            null, _region);
    }

    public PixelRect CombinedRect => _region.Bounds;
    public IList<PixelRect> Rects => _region.Rects;
}
