using System;
using Avalonia.Controls;
using ControlCatalog;
using Avalonia;

namespace WindowsInteropTest
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            AppBuilder.Configure<App>().UseWin32().UseDirect2D1().SetupWithoutStarting();
            System.Windows.Forms.Application.Run(new SelectorForm());
        }
    }
}
