﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Skia;
using Avalonia.ReactiveUI;

namespace ControlCatalog.NetCore
{
    static class Program
    {

        static int Main(string[] args)
        {
            Thread.CurrentThread.TrySetApartmentState(ApartmentState.STA);
            if (args.Contains("--wait-for-attach"))
            {
                Console.WriteLine("Attach debugger and use 'Set next statement'");
                while (true)
                {
                    Thread.Sleep(100);
                    if (Debugger.IsAttached)
                        break;
                }
            }

            var builder = BuildAvaloniaApp();
            if (args.Contains("--fbdev"))
            {
                System.Threading.ThreadPool.QueueUserWorkItem(_ => ConsoleSilencer());
                return builder.StartLinuxFramebuffer(args);
            }
            else
                return builder.StartWithClassicDesktopLifetime(args);
        }
        
        /// <summary>
        /// This method is needed for IDE previewer infrastructure
        /// </summary>
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .With(new X11PlatformOptions {EnableMultiTouch = true})
                .With(new Win32PlatformOptions
                {
                    EnableMultitouch = true,
                    AllowEglInitialization = true
                })
                .UseSkia()
                .UseReactiveUI();

        static void ConsoleSilencer()
        {
            Console.CursorVisible = false;
            while (true)
                Console.ReadKey(true);
        }
    }
}
