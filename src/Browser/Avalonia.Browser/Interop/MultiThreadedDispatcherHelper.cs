using System;
using System.Runtime.InteropServices.JavaScript;

namespace Avalonia.Browser.Interop;

internal static partial class MultiThreadedDispatcherHelper
{
    [JSImport("MultiThreadedDispatcherHelper.setWakeupEvent", AvaloniaModule.MainModuleName)]
    private static partial void SetWakeupEvent(int ptr);

    public static void SetWakeupEvent(IntPtr ptr) => SetWakeupEvent(checked((int)ptr));
}
