using System;
using System.Runtime.InteropServices.JavaScript;

namespace Avalonia.Browser.Interop;

internal static partial class TimerHelper
{
    [JSImport("TimerHelper.runAnimationFrames", AvaloniaModule.MainModuleName)]
    public static partial void RunAnimationFrames();

    public static Action<double>? AnimationFrame;
    [JSExport]
    public static void JsExportOnAnimationFrame(double d)
    {
        AnimationFrame?.Invoke(d);
    }
}
