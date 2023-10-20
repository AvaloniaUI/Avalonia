using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;

namespace Avalonia.Browser.Interop;

internal record GLInfo(int ContextId, uint FboId, int Stencils, int Samples, int Depth);

internal static partial class CanvasHelper
{

    [DllImport("libSkiaSharp", CallingConvention = CallingConvention.Cdecl)]
    static extern JSObject InterceptGLObject();

    public static GLInfo InitialiseGL(JSObject canvas, Action renderFrameCallback)
    {
        InterceptGLObject();

        var info = InitGL(canvas, canvas.GetPropertyAsString("id")!, renderFrameCallback);

        var glInfo = new GLInfo(
            info.GetPropertyAsInt32("context"),
            (uint)info.GetPropertyAsInt32("fboId"),
            info.GetPropertyAsInt32("stencil"),
            info.GetPropertyAsInt32("sample"),
            info.GetPropertyAsInt32("depth"));

        return glInfo;
    }

    [JSImport("Canvas.requestAnimationFrame", AvaloniaModule.MainModuleName)]
    public static partial void RequestAnimationFrame(JSObject canvas, bool renderLoop);

    [JSImport("Canvas.setCanvasSize", AvaloniaModule.MainModuleName)]
    public static partial void SetCanvasSize(JSObject canvas, int width, int height);

    [JSImport("Canvas.initGL", AvaloniaModule.MainModuleName)]
    private static partial JSObject InitGL(
        JSObject canvas,
        string canvasId,
        [JSMarshalAs<JSType.Function>] Action renderFrameCallback);

    [JSImport("globalThis.setTimeout")]
    public static partial int SetTimeout([JSMarshalAs<JSType.Function>] Action callback, int intervalMs);

    [JSImport("globalThis.clearTimeout")]
    public static partial int ClearTimeout(int id);

    [JSImport("globalThis.setInterval")]
    public static partial int SetInterval([JSMarshalAs<JSType.Function>] Action callback, int intervalMs);

    [JSImport("globalThis.clearInterval")]
    public static partial int ClearInterval(int id);
}
