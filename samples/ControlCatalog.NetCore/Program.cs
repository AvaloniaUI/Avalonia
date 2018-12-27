using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Dialogs;
using Avalonia.Dialogs.Internal;
using Avalonia.Skia;

namespace ControlCatalog.NetCore
{
    static class Program
    {
        
        static void Main(string[] args)
        {
            Thread.CurrentThread.TrySetApartmentState(ApartmentState.STA);
            var b = BuildAvaloniaApp();
            b.SetupWithoutStarting();
            var window = new Window();
            window.Show();
            new OpenFileDialog()
            {
                Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter {Name = "All files", Extensions = {"*"}},
                    new FileDialogFilter {Name = "Image files", Extensions = {"jpg", "png", "gif"}}
                },
                Directory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Title = "My dialog",
                InitialFileName = "config.local.json",
                AllowMultiple = true
            }.ShowAsync(window).ContinueWith(_ => { window.Close(); }, TaskContinuationOptions.ExecuteSynchronously);

            b.Instance.Run(window);
        }

        /// <summary>
        /// This method is needed for IDE previewer infrastructure
        /// </summary>
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>().UsePlatformDetect().UseSkia().UseReactiveUI().UseManagedSystemDialogs();

        static void ConsoleSilencer()
        {
            Console.CursorVisible = false;
            while (true)
                Console.ReadKey(true);
        }
    }
}
