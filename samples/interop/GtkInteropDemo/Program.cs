using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using ControlCatalog;

namespace GtkInteropDemo
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            AppBuilder.Configure<App>().UseGtk().UseCairo().SetupWithoutStarting();
            new MainWindow().Show();
            Gtk.Application.Run();
        }
    }
}
