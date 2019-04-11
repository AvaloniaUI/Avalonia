using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Logging.Serilog;
using Avalonia.Markup.Xaml;

namespace BindingDemo
{
    public class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AvaloniaXamlLoader.Load(this);
        }

        private static void Main()
        {
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .UseReactiveUI()
                .LogToDebug()
                .Start<MainWindow>();
        }
    }
}
