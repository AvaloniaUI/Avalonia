using System;
using System.Collections.Generic;
using Avalonia.Collections.Pooled;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

internal partial class ServerCompositionTarget
{
    public readonly IDirtyRectTracker DirtyRects;
    
    public void AddDirtyRect(LtrbRect rect)
    {
        if (rect.IsZeroSize)
            return;
        var snapped = LtrbPixelRect.FromRectWithNoScaling(SnapToDevicePixels(rect, Scaling));
        DebugEvents?.RectInvalidated(rect.ToRect());
        DirtyRects.AddRect(snapped);
        _redrawRequested = true;
    }

    public Rect SnapToDevicePixels(Rect rect) => SnapToDevicePixels(new(rect), Scaling).ToRect();
    public LtrbRect SnapToDevicePixels(LtrbRect rect) => SnapToDevicePixels(rect, Scaling);
        
    private static LtrbRect SnapToDevicePixels(LtrbRect rect, double scale)
    {
        return new LtrbRect(
            Math.Floor(rect.Left * scale) / scale,
            Math.Floor(rect.Top * scale) / scale,
            Math.Ceiling(rect.Right * scale) / scale,
            Math.Ceiling(rect.Bottom * scale) / scale);
    }
    
    
}