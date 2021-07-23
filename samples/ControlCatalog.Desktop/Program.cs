using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;

namespace ControlCatalog
{
    internal class Program
    {
        [STAThread]
        public static int Main(string[] args)
            => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        /// <summary>
        /// This method is needed for IDE previewer infrastructure
        /// </summary>
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .LogToTrace()
                .With(new AvaloniaNativePlatformOptions
                { 
                    AvaloniaNativeLibraryPath = "/Users/trd/github/Avalonia/Build/Products/Release/libAvalonia.Native.OSX.dylib", 
                })
                .UsePlatformDetect();

        private static void ConfigureAssetAssembly(AppBuilder builder)
        {
            AvaloniaLocator.CurrentMutable
                .GetService<IAssetLoader>()
                .SetDefaultAssembly(typeof(App).Assembly);
        }
    }
}
