using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using Avalonia.Browser.Interop;
using Avalonia.Browser.Rendering;
using Avalonia.Logging;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;

namespace Avalonia.Browser.Skia;

internal abstract class BrowserSurface : IDisposable
{
    protected BrowserSurface(JSObject jsSurface, Compositor compositor)
    {
        JsSurface = jsSurface;

        Compositor = compositor;
        Scaling = 1;
        ClientSize = new Size(1, 1);
        RenderSize = new PixelSize(1, 1);
    }

    public Compositor Compositor { get; }

    public JSObject JsSurface { get; private set; }
    public double Scaling { get; private set; }
    public Size ClientSize { get; private set; }
    public PixelSize RenderSize { get; private set; }

    public bool IsValid => RenderSize.Width > 0 && RenderSize.Height > 0 && Scaling > 0;

    public event Action? SizeChanged;
    public event Action? ScalingChanged;

    protected virtual void Initialize()
    {
        var w = JsSurface.GetPropertyAsInt32("width");
        var h = JsSurface.GetPropertyAsInt32("height");
        var s = JsSurface.GetPropertyAsDouble("scaling");
        OnSizeChanged(w, h, s);
    }

    public virtual void Dispose()
    {
        CanvasHelper.Destroy(JsSurface);
        JsSurface.Dispose();
        JsSurface = null!;
        RenderSize = default;
        ClientSize = default;
    }

    public virtual void OnSizeChanged(double pixelWidth, double pixelHeight, double dpr)
    {
        var oldScaling = Scaling;
        var oldClientSize = ClientSize;
        RenderSize = new PixelSize((int)pixelWidth, (int)pixelHeight);
        ClientSize = RenderSize.ToSize(dpr);
        Scaling = dpr;
        if (oldClientSize != ClientSize)
            SizeChanged?.Invoke();
        if (Math.Abs(oldScaling - dpr) > 0.0001)
            ScalingChanged?.Invoke();
    }

    public virtual object[] GetRenderSurfaces() => [this];
}
