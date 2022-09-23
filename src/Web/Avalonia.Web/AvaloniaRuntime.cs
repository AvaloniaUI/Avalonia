using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;

namespace Avalonia.Web;

public partial class AvaloniaRuntime
{
    public record GLInfo(int ContextId, uint FboId, int Stencils, int Samples, int Depth);

    [DllImport("libSkiaSharp", CallingConvention = CallingConvention.Cdecl)]
    static extern JSObject InterceptGLObject();

    [JSExport]
    internal static void StartAvaloniaView(JSObject canvas)
    {
        InterceptGLObject();
        // setup, get gl context...
        var info = InitGL(canvas, "testCanvas");

    
        var glInfo = new GLInfo(
            info.GetPropertyAsInt32("context"), 
            (uint)info.GetPropertyAsInt32("fboId"), 
            info.GetPropertyAsInt32("stencil"), 
            info.GetPropertyAsInt32("sample"), 
            info.GetPropertyAsInt32("depth"));

        Console.WriteLine($"{glInfo.ContextId}, {glInfo.FboId}");
    }

    [JSImport("Canvas.Foo", "avalonia.ts")]
    internal static partial void Foo(JSObject canvas);

    [JSImport("Canvas.initGL", "avalonia.ts")]
    internal static partial JSObject InitGL(JSObject canvas, string canvasId);

    [JSImport("StorageProvider.isFileApiSupported", "storage.ts")]
    public static partial bool IsFileApiSupported();

}

