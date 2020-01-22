using Avalonia;

namespace NativeEmbedSample
{
    class Program
    {
        static int Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect();

    }
}
