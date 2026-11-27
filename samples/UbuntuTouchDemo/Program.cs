using System;
using Avalonia;

namespace UbuntuTouchDemo;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args) => AppBuilder.Configure<App>()
        .UsePlatformDetect().WithInterFont().LogToTrace()
        .StartWithClassicDesktopLifetime(args);
}
