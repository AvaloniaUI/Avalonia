using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Avalonia.Controls;

namespace Avalonia.DesignerSupport.TestApp
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            AppBuilder.Configure<App>().UseDirect2D1().UseWin32().Start<MainWindow>();
        }
    }
}
