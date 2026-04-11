using Avalonia;

namespace WindowDecorationsDemo;

public static class Program
{
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithDeveloperTools()
            .LogToTrace();
}
