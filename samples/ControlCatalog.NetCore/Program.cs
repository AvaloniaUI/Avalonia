using System;
using System.Linq;
using Avalonia;

namespace ControlCatalog.NetCore
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Contains("--fbdev"))
                AppBuilder.Configure<App>()
                    .InitializeWithLinuxFramebuffer(tl => tl.Content = new MainView());
            else
                AppBuilder.Configure<App>()
                    .UsePlatformDetect()
                    .Start<MainWindow>();
        }
    }
}