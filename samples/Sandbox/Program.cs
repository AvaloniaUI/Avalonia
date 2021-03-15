using System.Diagnostics;
using Avalonia;

namespace Sandbox
{
    public class Program
    {
        static void Main(string[] args)
        {
            sw.Start();
            JetBrains.Profiler.Api.MeasureProfiler.StartCollectingData();
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .StartWithClassicDesktopLifetime(args);
        }

        public static Stopwatch sw = new Stopwatch();
    }
}
