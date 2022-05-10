using Avalonia;
using NativeEmbedSample;

namespace NativeEmbedSample.Desktop;

public class Program
{
    static int Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .With(new AvaloniaNativePlatformOptions()
            {
            })
            .UsePlatformDetect();

}
