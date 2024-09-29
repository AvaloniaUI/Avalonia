using System;
using System.Diagnostics;

namespace Avalonia.X11.XShmExtensions;

internal static class X11ShmDebugLogger
{
    [Conditional("Disable")]
    public static void WriteLine(string message) => Console.WriteLine(message);
}
