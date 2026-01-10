using System;
using Avalonia;
using IntegrationTestApp.Embedding;

namespace IntegrationTestApp
{
    class Program
    {
        public static bool OverlayPopups { get; private set; }

        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args)
        {
            OverlayPopups = args.Contains("--overlayPopups");
            
            BuildAvaloniaApp()
                .With(new Win32PlatformOptions
                {
                    OverlayPopups = OverlayPopups,
                })
                .With(new AvaloniaNativePlatformOptions
                {
                    OverlayPopups = OverlayPopups,
                })
                .StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithDeveloperTools()
                .AfterSetup(builder =>
                {
                    NativeTextBox.Factory = 
                        OperatingSystem.IsWindows() ? new Win32TextBoxFactory() :
                        OperatingSystem.IsMacOS() ? new MacOSTextBoxFactory() :
                        null;
                })
                .LogToTrace();
    }
}
