using System;
using Avalonia;

namespace UbuntuTouchDemo;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        var builder = AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();
        var wayland = Array.IndexOf(args, "--wayland") >= 0;
        if (wayland)
            builder.UseWayland();
        Console.WriteLine($"Windowing backend: {(wayland ? "Wayland" : "X11")}");
        return builder.StartWithClassicDesktopLifetime(args);
    }
}
