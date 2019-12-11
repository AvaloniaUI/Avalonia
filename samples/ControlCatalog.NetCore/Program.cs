using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.ReactiveUI;
using Avalonia.Dialogs;

namespace ControlCatalog.NetCore
{
    static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
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

            double GetScaling()
            {
                var idx = Array.IndexOf(args, "--scaling");
                if (idx != 0 && args.Length > idx + 1 &&
                    double.TryParse(args[idx + 1], NumberStyles.Any, CultureInfo.InvariantCulture, out var scaling))
                    return scaling;
                return 1;
            }
            if (args.Contains("--fbdev"))
            {
                SilenceConsole();
                return builder.StartLinuxFbDev(args, scaling: GetScaling());
            }
            else if (args.Contains("--drm"))
            {
                SilenceConsole();
                return builder.StartLinuxDrm(args, scaling: GetScaling());
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
                .With(new X11PlatformOptions
                {
                    EnableMultiTouch = true,
                    UseDBusMenu = true
                })
                .With(new Win32PlatformOptions
                {
                    EnableMultitouch = true,
                    AllowEglInitialization = true
                })
                .UseSkia()
                .UseReactiveUI()
                .UseManagedSystemDialogs();

        static void SilenceConsole()
        {
            new Thread(() =>
            {
                Console.CursorVisible = false;
                while (true)
                    Console.ReadKey(true);
            })
            { IsBackground = true }.Start();
        }
    }
}
