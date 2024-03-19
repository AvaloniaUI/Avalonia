using System;
using System.Collections.Generic;
using Avalonia.Metadata;

namespace Avalonia.Platform;

[Unstable, PrivateApi]
public interface IPlatformRenderInterfaceRegion : IDisposable
{
    void AddRect(PixelRect rect);
    void Reset();
    bool IsEmpty { get; }
    PixelRect Bounds { get; }
    IList<PixelRect> Rects { get; }
    bool Intersects(Rect rect);
    bool Contains(Point pt);
}