using System;
using System.Diagnostics;
using Avalonia.Logging;

namespace Avalonia.X11.XShmExtensions;

internal static class X11ShmDebugLogger
{
    [Conditional("Disable")] // Do not open it unless you need to debug XShm
    public static void WriteLine(string message)
    {
        Logger.TryGet(LogEventLevel.Debug, LogArea.X11Platform)?.Log(null, message);
    }
}
