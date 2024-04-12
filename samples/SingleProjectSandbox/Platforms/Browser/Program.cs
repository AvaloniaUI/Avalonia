using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia.Browser;
using SingleProjectSandbox;

[assembly:SupportedOSPlatform("browser")]

internal static class Program
{
    public static async Task Main(string[] args)
    {
        await App.BuildAvaloniaApp()
            .StartBrowserAppAsync("out");
    }
}
