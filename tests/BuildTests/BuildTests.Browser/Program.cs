using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;

namespace BuildTests.Browser;

internal static class Program
{
    private static Task Main()
        => BuildAvaloniaApp().StartBrowserAppAsync("out");

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();
}
