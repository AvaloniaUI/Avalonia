using System;
using System.Runtime.InteropServices.JavaScript;

namespace Avalonia.Browser.Interop;

internal static partial class TimerHelper
{
    [JSImport("TimerHelper.runAnimationFrames", AvaloniaModule.MainModuleName)]
    public static partial void RunAnimationFrames(
        [JSMarshalAs<JSType.Function<JSType.Number, JSType.Boolean>>] Func<double, bool> renderFrameCallback);

    [JSImport("globalThis.setTimeout")]
    public static partial int SetTimeout([JSMarshalAs<JSType.Function>] Action callback, int intervalMs);

    [JSImport("globalThis.clearTimeout")]
    public static partial int ClearTimeout(int id);

    [JSImport("globalThis.setInterval")]
    public static partial int SetInterval([JSMarshalAs<JSType.Function>] Action callback, int intervalMs);

    [JSImport("globalThis.clearInterval")]
    public static partial int ClearInterval(int id);
}
