using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Avalonia.Web;

public partial class AvaloniaRuntime
{
    public record GLInfo(int ContextId, uint FboId, int Stencils, int Samples, int Depth);

    [JSExport]
    internal static void StartAvaloniaView(JSObject canvas)
    {
        Init();
        // setup, get gl context...
        var info = InitGL(canvas, "testCanvas");

    
       /* var glInfo = new GLInfo(
            info.GetPropertyAsInt32("context"), 
            (uint)info.GetPropertyAsInt32("fboId"), 
            info.GetPropertyAsInt32("stencil"), 
            info.GetPropertyAsInt32("sample"), 
            info.GetPropertyAsInt32("depth"));

        Console.WriteLine($"{glInfo.ContextId}, {glInfo.FboId}");*/
    }

    public static void Init ()
    {
        if (false)
        {
            SkiaSharp.GRGlInterface.Create();
        }
    }

    [JSImport("Canvas.Foo", "avalonia.ts")]
    internal static partial void Foo(JSObject canvas);

    [JSImport("Canvas.initGL", "avalonia.ts")]
    internal static partial JSObject InitGL(JSObject canvas, string canvasId);

    [JSImport("StorageProvider.isFileApiSupported", "storage.ts")]
    public static partial bool IsFileApiSupported();

    //[DllImport("__Internal")]
    //public

}

