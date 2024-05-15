using Avalonia;

namespace Sandbox
{
    public class Program
    {
        static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        public static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<App>()
                .With(new AvaloniaNativePlatformOptions
                {
                    AvaloniaNativeLibraryPath = "/Users/benediktstebner/RiderProjects/Avalonia/native/Avalonia.Native/src/OSX/build/Products/Debug/libAvalonia.Native.OSX.dylib"
                })
                .UsePlatformDetect()
                .LogToTrace();
    }
}
