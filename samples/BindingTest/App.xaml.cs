using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Diagnostics;
using Avalonia.Logging.Serilog;
using Avalonia.Markup.Xaml;
using Serilog;

namespace BindingTest
{
    public class App : Application
    {
        public App()
        {
            RegisterServices();
        }

        private static void Main()
        {
            InitializeLogging();

            new App()
                .UseWin32()
                .UseDirect2D()
                .LoadFromXaml()
                .RunWithMainWindow<MainWindow>();
        }

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
