using Avalonia;

namespace Sandbox
{
    public class Program
    {
        static void Main(string[] args)
        {
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .With(new AvaloniaNativePlatformOptions
                { 
                    AvaloniaNativeLibraryPath = "/Users/trd/github/Avalonia/Build/Products/Release/libAvalonia.Native.OSX.dylib", 
                })
                .LogToTrace()
                .StartWithClassicDesktopLifetime(args);
        }
    }
}
