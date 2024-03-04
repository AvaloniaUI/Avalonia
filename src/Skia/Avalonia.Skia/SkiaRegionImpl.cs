using System;
using System.Collections.Generic;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia;

internal class SkiaRegionImpl : IPlatformRenderInterfaceRegion
{
    private SKRegion? _region = new();
    public SKRegion Region => _region ?? throw new ObjectDisposedException(nameof(SkiaRegionImpl));
    private bool _rectsValid;
    private List<PixelRect>? _rects;
    public void Dispose()
    {
        _region?.Dispose();
        _region = null;
    }

    public void AddRect(PixelRect rect)
    {
        _rectsValid = false;
        Region.Op(rect.X, rect.Y, rect.Right, rect.Bottom, SKRegionOperation.Union);
    }

    public void Reset()
    {
        _rectsValid = false;
        Region.SetEmpty();
    }

    public bool IsEmpty => Region.IsEmpty;
    public PixelRect Bounds => Region.Bounds.ToAvaloniaPixelRect();

    public IList<PixelRect> Rects
    {
        get
        {
            _rects ??= new();
            if (!_rectsValid)
            {
                _rects.Clear();
                using var iter = Region.CreateRectIterator();
                while (iter.Next(out var rc))
                    _rects.Add(rc.ToAvaloniaPixelRect());
            }
            return _rects;
        }
    }

    public bool Intersects(Rect rect) => Region.Intersects(PixelRect.FromRect(rect, 1).ToSKRectI());
    public bool Contains(Point pt) => Region.Contains((int)pt.X, (int)pt.Y);
}