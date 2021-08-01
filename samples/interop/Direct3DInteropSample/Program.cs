using Avalonia;

namespace Direct3DInteropSample
{
    class Program
    {
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .With(new Win32PlatformOptions { UseDeferredRendering = false })
                .UseWin32()
                .UseDirect2D1();

        public static int Main(string[] args)
            => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }
}
