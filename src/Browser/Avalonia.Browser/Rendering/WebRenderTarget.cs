using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using Avalonia.Browser.Interop;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Reactive;
using SkiaSharp;

namespace Avalonia.Browser.Rendering;

abstract partial class BrowserRenderTarget(JSObject js)
{
    protected readonly JSObject Js = js;

    [JSImport("WebRenderTargetRegistry.getRenderTarget", AvaloniaModule.MainModuleName)]
    private static partial JSObject? GetJsRenderTarget(int id);
    
    [JSImport("WebRenderTarget.setSize", AvaloniaModule.MainModuleName)]
    private static partial void SetJsSize(JSObject target, int w, int h);

    public static BrowserRenderTarget? GetRenderTarget(int id, Func<(PixelSize, double)> sizeGetter)
    {
        var js = GetJsRenderTarget(id);
        if (js == null)
            return null;
        var type = js.GetPropertyAsString("renderTargetType");
        if (type == "webgl")
            return new BrowserWebGlRenderTarget(js, sizeGetter);
        if (type == "software")
            return new BrowserSoftwareRenderTarget(js, sizeGetter);
        throw new NotSupportedException(type);
    }
    
    public abstract IPlatformGraphicsContext? PlatformGraphicsContext { get; }

    protected void UpdateSize(PixelSize size)
    {
        SetJsSize(Js, size.Width, size.Height);
    }
}
