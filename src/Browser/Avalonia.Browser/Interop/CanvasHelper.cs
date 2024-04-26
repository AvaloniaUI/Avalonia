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

    [JSImport("CanvasFactory.onSizeChanged", AvaloniaModule.MainModuleName)]
    public static partial void OnSizeChanged(
        JSObject canvasSurface,
        [JSMarshalAs<JSType.Function<JSType.Number, JSType.Number, JSType.Number>>]
        // TODO: this callback should be <int, int, double>. Revert after next .NET 9 preview.  
        Action<double, double, double> onSizeChanged);

    [JSImport("CanvasFactory.create", AvaloniaModule.MainModuleName)]
    private static partial JSObject Create(JSObject canvasSurface, int mode);

    [JSImport("CanvasFactory.destroy", AvaloniaModule.MainModuleName)]
    public static partial void Destroy(JSObject canvasSurface);

    [JSImport("CanvasFactory.ensureSize", AvaloniaModule.MainModuleName)]
    public static partial void EnsureSize(JSObject canvasSurface);

    [JSImport("CanvasFactory.putPixelData", AvaloniaModule.MainModuleName)]
    public static partial void PutPixelData(JSObject canvasSurface, [JSMarshalAs<JSType.MemoryView>] ArraySegment<byte> data, int width, int height);
}
