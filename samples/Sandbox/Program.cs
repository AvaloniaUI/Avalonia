using Avalonia;

namespace Sandbox
{
    public class Program
    {
        static void Main(string[] args)
        {
            sw = System.Diagnostics.Stopwatch.StartNew();
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .StartWithClassicDesktopLifetime(args);
        }

        public static System.Diagnostics.Stopwatch sw;
    }
}
