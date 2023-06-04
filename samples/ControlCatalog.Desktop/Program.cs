using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using ControlCatalog.NetCore;
using ControlCatalog.Pages;

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
                .AfterSetup(builder =>
                {
                    builder.Instance!.AttachDevTools(new Avalonia.Diagnostics.DevToolsOptions()
                    {
                        StartupScreenIndex = 1,
                    });

                    EmbedSample.Implementation = new EmbedSampleWin();
                })
                .UsePlatformDetect();

        private static void ConfigureAssetAssembly(AppBuilder builder)
        {
            AvaloniaLocator.CurrentMutable
                .GetRequiredService<IAssetLoader>()
                .SetDefaultAssembly(typeof(App).Assembly);
        }
    }
}
