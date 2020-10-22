using Avalonia;
using Avalonia.ReactiveUI;

namespace Sandbox
{
    public class Program
    {
        static void Main(string[] args)
        {
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .UseReactiveUI()
                .LogToTrace()
                .StartWithClassicDesktopLifetime(args);
        }
    }
}
