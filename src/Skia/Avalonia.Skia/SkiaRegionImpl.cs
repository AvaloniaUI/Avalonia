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
    private List<LtrbPixelRect>? _rects;
    public void Dispose()
    {
        _region?.Dispose();
        _region = null;
    }

    public void AddRect(LtrbPixelRect rect)
    {
        _rectsValid = false;
        Region.Op(rect.Left, rect.Top, rect.Right, rect.Bottom, SKRegionOperation.Union);
    }

    public void Reset()
    {
        _rectsValid = false;
        Region.SetEmpty();
    }

    public bool IsEmpty => Region.IsEmpty;
    public LtrbPixelRect Bounds => Region.Bounds.ToAvaloniaLtrbPixelRect();

    public IList<LtrbPixelRect> Rects
    {
        get
        {
            _rects ??= new();
            if (!_rectsValid)
            {
                _rects.Clear();
                using var iter = Region.CreateRectIterator();
                while (iter.Next(out var rc))
                    _rects.Add(rc.ToAvaloniaLtrbPixelRect());
            }
            return _rects;
        }
    }

    public bool Intersects(LtrbRect rect) => Region.Intersects(
        new SKRectI((int)rect.Left, (int)rect.Top,
            (int)Math.Ceiling(rect.Right), (int)Math.Ceiling(rect.Bottom)));
    
    public bool Contains(Point pt) => Region.Contains((int)pt.X, (int)pt.Y);
}