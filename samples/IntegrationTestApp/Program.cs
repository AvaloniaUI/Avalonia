using System;
using System.Diagnostics;
using Avalonia;
using IntegrationTestApp.Embedding;

namespace IntegrationTestApp
{
    class Program
    {
        public static bool OverlayPopups { get; private set; }
        public static bool TrayActivation { get; private set; }

        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args)
        {
            OverlayPopups = args.Contains("--overlayPopups");
            TrayActivation = args.Contains("--tray-activation");
            if (TrayActivation)
                Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
            var builder = BuildAvaloniaApp();
            if (args.Contains("--wayland"))
            {
                var options = new WaylandPlatformOptions();
                if (args.Contains("--software"))
                    options.GlProfiles = [];
                if (args.Contains("--dmabuf"))
                    options.UseDmabufSwapchain = true;
                builder.UseWayland().With(options);
            }
            if (TrayActivation)
                builder.LogToTrace(Avalonia.Logging.LogEventLevel.Verbose,
                    "WaylandProtocol", "FreeDesktopPlatform");
            
            builder
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
#if DEBUG
                .WithDeveloperTools()
#endif
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
