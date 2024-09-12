using System;
using System.Collections.Generic;
using Avalonia.Collections.Pooled;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

internal partial class ServerCompositionTarget
{
    public readonly IDirtyRectTracker DirtyRects;

    static int Clamp0(int value, int max) => Math.Max(Math.Min(value, max), 0);
    
    public void AddDirtyRect(LtrbRect rect)
    {
        if (rect.IsZeroSize)
            return;
        
        DebugEvents?.RectInvalidated(rect.ToRect());
        
        var snapped = LtrbPixelRect.FromRectWithNoScaling(SnapToDevicePixels(rect, Scaling));

        var clamped = new LtrbPixelRect(
            Clamp0(snapped.Left, _pixelSize.Width),
            Clamp0(snapped.Top, _pixelSize.Height),
            Clamp0(snapped.Right, _pixelSize.Width),
            Clamp0(snapped.Bottom, _pixelSize.Height)
        );
        
        if (!clamped.IsEmpty)
            DirtyRects.AddRect(clamped);
        _redrawRequested = true;
    }

    public Rect SnapToDevicePixels(Rect rect) => SnapToDevicePixels(new(rect), Scaling).ToRect();
    public LtrbRect SnapToDevicePixels(LtrbRect rect) => SnapToDevicePixels(rect, Scaling);
        
    public static LtrbRect SnapToDevicePixels(LtrbRect rect, double scale)
    {
        return new LtrbRect(
            Math.Floor(rect.Left * scale) / scale,
            Math.Floor(rect.Top * scale) / scale,
            Math.Ceiling(rect.Right * scale) / scale,
            Math.Ceiling(rect.Bottom * scale) / scale);
    }
    
    
}