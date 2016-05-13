using System;
using Avalonia;
using Avalonia.Controls;
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

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private static void Main()
        {
            InitializeLogging();

            AppBuilder.Configure<App>()
                .UseWin32()
                .UseDirect2D1()
                .Start<MainWindow>();
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
