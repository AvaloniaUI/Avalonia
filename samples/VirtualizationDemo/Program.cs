using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Logging.Serilog;
using Avalonia.ReactiveUI;
using Serilog;

namespace VirtualizationDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            AppBuilder.Configure<App>()
               .UsePlatformDetect()
               .UseReactiveUI()
               .LogToDebug()
               .Start<MainWindow>();
        }
    }
}
