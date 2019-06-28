using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Avalonia.LogicalTree;
using Avalonia.Skia;
using Avalonia.ReactiveUI;
using Avalonia.Threading;

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
            else if (args.Contains("--vnc"))
            {
                return builder.StartWithHeadlessVncPlatform(null, 5901, args, ShutdownMode.OnMainWindowClose);
            }
            else if (args.Contains("--full-headless"))
            {
                return builder
                    .UseHeadless(true)
                    .AfterSetup(_ =>
                    {
                        DispatcherTimer.RunOnce(async () =>
                        {
                            var window = ((IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime)
                                .MainWindow;
                            var tc = window.GetLogicalDescendants().OfType<TabControl>().First();
                            foreach (var page in tc.Items.Cast<TabItem>().ToList())
                            {
                                // Skip DatePicker because of some layout bug in grid
                                if (page.Header.ToString() == "DatePicker")
                                    continue;
                                Console.WriteLine("Selecting " + page.Header);
                                tc.SelectedItem = page;
                                await Task.Delay(500);
                            }
                            Console.WriteLine("Selecting the first page");
                            tc.SelectedItem = tc.Items.OfType<object>().First();
                            await Task.Delay(500);
                            Console.WriteLine("Clicked through all pages, triggering GC");
                            for (var c = 0; c < 3; c++)
                            {
                                GC.Collect(2, GCCollectionMode.Forced);
                                await Task.Delay(500);
                            }

                            void FormatMem(string metric, long bytes)
                            {
                                Console.WriteLine(metric + ": " + bytes / 1024 / 1024 + "MB");
                            }

                            FormatMem("GC allocated bytes", GC.GetTotalMemory(true));
                            FormatMem("WorkingSet64", Process.GetCurrentProcess().WorkingSet64);

                        }, TimeSpan.FromSeconds(1));
                    })
                    .StartWithClassicDesktopLifetime(args);
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
