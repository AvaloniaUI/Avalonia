using Avalonia;

namespace Sandbox
{
    public class Program
    {
        static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        public static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
            .With(new Win32PlatformOptions
            {
                OverlayPopups = true
            })
                .LogToTrace(Avalonia.Logging.LogEventLevel.Verbose, "Focus");
    }
}
