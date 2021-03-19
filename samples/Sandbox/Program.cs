using System.Diagnostics;
using Avalonia;

namespace Sandbox
{
    public class Program
    {
        static void Main(string[] args)
        {
            sw = Stopwatch.StartNew();
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .StartWithClassicDesktopLifetime(args);
        }

        public static Stopwatch sw;
    }
}
