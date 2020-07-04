using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;

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
