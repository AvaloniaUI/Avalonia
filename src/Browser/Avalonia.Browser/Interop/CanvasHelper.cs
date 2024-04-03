using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;

namespace Avalonia.Browser.Interop;

internal record GLInfo(int ContextId, uint FboId, int Stencils, int Samples, int Depth);

internal static partial class CanvasHelper
{
    public static (JSObject CanvasView, GLInfo? GLInfo) CreateSurface(
        JSObject container, BrowserRenderingMode renderingMode)
    {
        var isGlMode = renderingMode is BrowserRenderingMode.WebGL1 or BrowserRenderingMode.WebGL2;

        var canvasView = Create(container, (int)renderingMode);

        GLInfo? glInfo = null;
        if (isGlMode)
        {
            glInfo = new GLInfo(
                canvasView.GetPropertyAsInt32("contextHandle")!,
                (uint)canvasView.GetPropertyAsInt32("fboId"),
                canvasView.GetPropertyAsInt32("stencil"),
                canvasView.GetPropertyAsInt32("sample"),
                canvasView.GetPropertyAsInt32("depth"));
        }

        return (canvasView, glInfo);
    }

    [JSImport("CanvasFactory.requestAnimationFrame", AvaloniaModule.MainModuleName)]
    public static partial void RequestAnimationFrame(JSObject canvasSurface, [JSMarshalAs<JSType.Function>] Action renderFrameCallback);

    [JSImport("CanvasFactory.onSizeChanged", AvaloniaModule.MainModuleName)]
    public static partial void OnSizeChanged(
        JSObject canvasSurface,
        [JSMarshalAs<JSType.Function<JSType.Number, JSType.Number, JSType.Number>>]
        Action<int, int, double> onSizeChanged);

    [JSImport("CanvasFactory.create", AvaloniaModule.MainModuleName)]
    private static partial JSObject Create(JSObject canvasSurface, int mode);

    [JSImport("CanvasFactory.destroy", AvaloniaModule.MainModuleName)]
    public static partial void Destroy(JSObject canvasSurface);

    [JSImport("CanvasFactory.putPixelData", AvaloniaModule.MainModuleName)]
    public static partial void PutPixelData(JSObject canvasSurface, [JSMarshalAs<JSType.MemoryView>] ArraySegment<byte> data, int width, int height);

    [JSImport("globalThis.setTimeout")]
    public static partial int SetTimeout([JSMarshalAs<JSType.Function>] Action callback, int intervalMs);

    [JSImport("globalThis.clearTimeout")]
    public static partial int ClearTimeout(int id);

    [JSImport("globalThis.setInterval")]
    public static partial int SetInterval([JSMarshalAs<JSType.Function>] Action callback, int intervalMs);

    [JSImport("globalThis.clearInterval")]
    public static partial int ClearInterval(int id);
}
