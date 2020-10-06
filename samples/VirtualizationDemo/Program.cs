﻿using Avalonia;
using Avalonia.ReactiveUI;

namespace VirtualizationDemo
{
    class Program
    {
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .UseReactiveUI()
                .LogToDebug();

        public static int Main(string[] args)
            => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }
}
