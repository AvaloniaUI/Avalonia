using System;
using System.Collections.Generic;
using Avalonia.Collections.Pooled;

namespace Avalonia.Rendering.Composition.Server;

internal partial class ServerCompositionTarget
{
    public readonly IDirtyRectTracker DirtyRects;
    
    public void AddDirtyRect(Rect rect)
    {
        if (rect.Width == 0 && rect.Height == 0)
            return;
        var snapped = PixelRect.FromRect(SnapToDevicePixels(rect, Scaling), 1);
        DebugEvents?.RectInvalidated(rect);
        DirtyRects.AddRect(snapped);
        _redrawRequested = true;
    }
    
    public Rect SnapToDevicePixels(Rect rect) => SnapToDevicePixels(rect, Scaling);
        
    private static Rect SnapToDevicePixels(Rect rect, double scale)
    {
        return new Rect(
            new Point(
                Math.Floor(rect.X * scale) / scale,
                Math.Floor(rect.Y * scale) / scale),
            new Point(
                Math.Ceiling(rect.Right * scale) / scale,
                Math.Ceiling(rect.Bottom * scale) / scale));
    }
    
    
}