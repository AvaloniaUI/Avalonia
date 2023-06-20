using Avalonia;
using Avalonia.ReactiveUI;

namespace Generators.Sandbox;

internal static class Program
{
    public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UseReactiveUI()
            .UsePlatformDetect()
            .LogToTrace();
}
