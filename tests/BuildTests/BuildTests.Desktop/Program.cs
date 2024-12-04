using System;
using Avalonia;

namespace BuildTests.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
        => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>()
            .UseSkia()
            .LogToTrace();

        // We don't use Avalonia.Desktop with UsePlatformDetect() because Avalonia.Native is only built on macOS,
        // causing restore to fail for the exact package versions we're using in this solution.
        if (OperatingSystem.IsWindows())
            builder.UseWin32();
        else if (OperatingSystem.IsLinux())
            builder.UseX11();

        return builder;
    }
}
