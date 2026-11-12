using System;
using Avalonia;

namespace ImpellerSandbox;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    // RENDERER=skia renders the same window with Skia for comparison.
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseImpellerIfRequested()
            .WithInterFont()
            .LogToTrace();
    }
}
