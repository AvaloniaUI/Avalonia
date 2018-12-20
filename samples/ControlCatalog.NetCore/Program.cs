using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.FreeDesktop;
using Avalonia.Gtk3;
using Avalonia.Skia;

namespace ControlCatalog.NetCore
{
    static class Program
    {
        
        static void Main(string[] args)
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

            if (args.Contains("--fbdev"))
                AppBuilder.Configure<App>().InitializeWithLinuxFramebuffer(tl =>
                {
                    tl.Content = new MainView();
                    System.Threading.ThreadPool.QueueUserWorkItem(_ => ConsoleSilencer());
                });
            else
                BuildAvaloniaApp().Start(AppMain, args);
        }


        
        static void AppMain(Application app, string[] args)
        {
            var w = new MainWindow();
            var list = new AvaloniaList<SimpleMenuItem>
            {
                new SimpleMenuItem()
                {
                    Text = "Test",
                    SubItems = new AvaloniaList<SimpleMenuItem>
                    {
                        new SimpleMenuItem {Text = "Item 1"},
                        new SimpleMenuItem {Text = "Item 2"},
                    }
                }
            };
            async void InitializeMenu()
            {
                var exporter = new DBusExportedMenu(list);
                await exporter.RegisterAsync(w.PlatformImpl.Handle.Handle.ToInt32());
            }
            InitializeMenu();
            app.Run(w);
        }

        /// <summary>
        /// This method is needed for IDE previewer infrastructure
        /// </summary>
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>().UsePlatformDetect().UseSkia().UseReactiveUI().UseGtk3(new Gtk3PlatformOptions
            {
                UseDeferredRendering = false
            });

        static void ConsoleSilencer()
        {
            Console.CursorVisible = false;
            while (true)
                Console.ReadKey(true);
        }
    }
}
