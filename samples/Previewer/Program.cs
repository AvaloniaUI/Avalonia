using System;
using Avalonia;

namespace Previewer
{
    class Program
    {
        static void Main(string[] args)
        {
            AppBuilder.Configure<App>().UsePlatformDetect().Start<MainWindow>();
        }
    }
}