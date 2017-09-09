using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Logging.Serilog;
using Avalonia.Platform;
using Serilog;

namespace RenderTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            InitializeLogging();

            // TODO: Make this work with GTK/Skia/Cairo depending on command-line args
            // again.
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
                //.UseWin32().UseDirect2D1()
                //.UseWin32().UseSkia()
                //.UseGtk().UseCairo()
                .UseReactiveUI()
                .Start<MainWindow>();
        }

        // This will be made into a runtime configuration extension soon!
        private static void InitializeLogging()
        {
#if DEBUG
            SerilogLogger.Initialize(new LoggerConfiguration()
                .MinimumLevel.Warning()
                .WriteTo.Trace(outputTemplate: "{Area}: {Message}")
                .CreateLogger());
#endif
        }
    }
}
