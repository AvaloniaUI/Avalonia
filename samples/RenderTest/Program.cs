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
        // TODO: Make this work with GTK/Skia/Cairo depending on command-line args
        // again.
        static void Main(string[] args) => BuildAvaloniaApp().Start<MainWindow>();

        // App configuration, used by the entry point and previewer
        static AppBuilder BuildAvaloniaApp()
           => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .UseReactiveUI()
                .LogToDebug();
    }
}
