using System;
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
            AppBuilder.Configure<App>()
                .UseWin32()
                .UseSkia()
                .SetupWithoutStarting();
            System.Windows.Forms.Application.Run(new EmbedToWinFormsDemo());
        }
    }
}
