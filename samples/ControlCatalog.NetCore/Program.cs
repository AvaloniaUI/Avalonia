using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia;

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
            if (args.Contains("--fbdev"))
                AppBuilder.Configure<App>().InitializeWithLinuxFramebuffer(tl =>
                {
                    tl.Content = new MainView();
                    System.Threading.ThreadPool.QueueUserWorkItem(_ => ConsoleSilencer());
                });
            else
                AppBuilder.Configure<App>()
                    .CustomPlatformDetect()
                    .UseReactiveUI()
                    .Start<MainWindow>();
        }

        static AppBuilder CustomPlatformDetect(this AppBuilder builder)
        {
            //This is needed because we still aren't ready to have MonoMac backend as default one
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return builder.UseSkia().UseMonoMac();
            return builder.UsePlatformDetect();
        }

        static void ConsoleSilencer()
        {
            Console.CursorVisible = false;
            while (true)
                Console.ReadKey(true);
        }
    }
}