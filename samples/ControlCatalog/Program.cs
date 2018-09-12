using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Skia;
using Avalonia.Threading;

namespace ControlCatalog.NetCore
{
    static class Program
    {
        
        static void Main(string[] args)
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
            BuildAvaloniaApp().Start<MainWindow>();
            return;
            var app = BuildAvaloniaApp().SetupWithoutStarting().Instance;
            var src = new CancellationTokenSource();
            int cnt = 0;
            DispatcherTimer timer = null;
            timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, delegate
            {
                cnt++;
                Console.WriteLine("Tick " + cnt);
                if (cnt == 3)
                {
                    timer.Stop();
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Console.WriteLine("Invoked");
                        src.Cancel();
                    });
                }
            });
            timer.Start();

            app.Run(src.Token);

        }

        /// <summary>
        /// This method is needed for IDE previewer infrastructure
        /// </summary>
        public static AppBuilder BuildAvaloniaApp()
        {
            var libraryPath = Path.Combine(Directory.GetCurrentDirectory(),
                                           "../../src/Avalonia.Native.OSX/build/Avalonia.Native.OSX/Build/Products/Debug/libAvalonia.Native.OSX.dylib");

            return AppBuilder.Configure<App>().UseAvaloniaNative(libraryPath).UseSkia();
        }

    }
}
