using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;
using Avalonia.Web.Blazor;

namespace Avalonia.Web;


public partial class AvaloniaRuntime
{



    public record GLInfo(int ContextId, uint FboId, int Stencils, int Samples, int Depth);

    [DllImport("libSkiaSharp", CallingConvention = CallingConvention.Cdecl)]
    static extern JSObject InterceptGLObject();

    [JSExport]
    internal static void StartAvaloniaView(JSObject canvas)
    {
        
        // setup, get gl context...
        
    }

    public static GLInfo InitialiseGL (JSObject canvas)
    {
        InterceptGLObject();

        var info = InitGL(canvas, "testCanvas");


        var glInfo = new GLInfo(
            info.GetPropertyAsInt32("context"),
            (uint)info.GetPropertyAsInt32("fboId"),
            info.GetPropertyAsInt32("stencil"),
            info.GetPropertyAsInt32("sample"),
            info.GetPropertyAsInt32("depth"));

        return glInfo;
    }

    [JSImport("Canvas.createCanvas", "avalonia.ts")]
    public static partial JSObject CreateCanvas(JSObject container);

    [JSImport("Canvas.Foo", "avalonia.ts")]
    public static partial void Foo(JSObject canvas);

    [JSImport("Canvas.initGL", "avalonia.ts")]
    private static partial JSObject InitGL(JSObject canvas, string canvasId);

    [JSImport("StorageProvider.isFileApiSupported", "storage.ts")]
    public static partial bool IsFileApiSupported();

}

