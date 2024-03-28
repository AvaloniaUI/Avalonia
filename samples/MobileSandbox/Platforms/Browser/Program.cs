using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using MobileSandbox;

[assembly:SupportedOSPlatform("browser")]

internal partial class Program
{
    public static async Task Main(string[] args)
    {
        await BuildAvaloniaApp()
            .StartBrowserAppAsync("out");
    }
    
    public static AppBuilder BuildAvaloniaApp()
           => AppBuilder.Configure<App>();
}
