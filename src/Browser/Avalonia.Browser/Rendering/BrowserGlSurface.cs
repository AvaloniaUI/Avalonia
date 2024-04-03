using System;
using System.Runtime.InteropServices.JavaScript;
using Avalonia.Browser.Interop;
using Avalonia.Browser.Skia;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Browser.Rendering;

internal sealed class BrowserGlSurface : BrowserSurface
{
    private readonly GRGlInterface _glInterface;

    public BrowserGlSurface(JSObject canvasSurface, GLInfo glInfo, PixelFormat pixelFormat,
        BrowserRenderingMode renderingMode)
        : base(canvasSurface, renderingMode)
    {
        var skiaOptions = AvaloniaLocator.Current.GetService<SkiaOptions>();
        _glInterface = GRGlInterface.Create() ?? throw new InvalidOperationException("Unable to create GRGlInterface.");
        Context = GRContext.CreateGl(_glInterface) ??
                  throw new InvalidOperationException("Unable to create GRContext.");
        if (skiaOptions?.MaxGpuResourceSizeBytes is { } resourceSizeBytes)
        {
            Context.SetResourceCacheLimit(resourceSizeBytes);
        }

        GlInfo = glInfo ?? throw new ArgumentNullException(nameof(glInfo));
        PixelFormat = pixelFormat;
    }

    public PixelFormat PixelFormat { get; }

    public GRContext Context { get; private set; }

    public GLInfo GlInfo { get; }

    public override void Dispose()
    {
        base.Dispose();

        Context.Dispose();
        Context = null!;

        _glInterface.Dispose();
    }

    public void EnsureResize()
    {
        CanvasHelper.EnsureSize(JsSurface);
    }
}
