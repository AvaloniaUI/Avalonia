using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;

namespace Direct3DInteropSample
{
    class Program
    {
        static void Main(string[] args)
        {
            AppBuilder.Configure<App>()
                .With(new Win32PlatformOptions {UseDeferredRendering = false})
                .UseWin32().UseDirect2D1().Start<MainWindow>();
        }
    }
}
