using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;

namespace Avalonia.Web.Interop;

internal record GLInfo(int ContextId, uint FboId, int Stencils, int Samples, int Depth);

[System.Runtime.Versioning.SupportedOSPlatform("browser")] // gets rid of callsite warnings
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

    [JSImport("Canvas.requestAnimationFrame", "avalonia")]
    public static partial void RequestAnimationFrame(JSObject canvas, bool renderLoop);

    [JSImport("Canvas.setCanvasSize", "avalonia")]
    public static partial void SetCanvasSize(JSObject canvas, int height, int width);

    [JSImport("Canvas.initGL", "avalonia")]
    private static partial JSObject InitGL(
        JSObject canvas,
        string canvasId,
        [JSMarshalAs<JSType.Function>] Action renderFrameCallback);
}
