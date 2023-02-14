using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using Avalonia.Controls;
using ControlCatalog;
using ControlCatalog.Browser;

[assembly:SupportedOSPlatform("browser")]

internal partial class Program
{
    public static async Task Main(string[] args)
    {
        await BuildAvaloniaApp()
            .AfterSetup(_ =>
            {
                ControlCatalog.Pages.EmbedSample.Implementation = new EmbedSampleWeb();
            })
            .StartBrowserApp("out");
    }

    // Example without a ISingleViewApplicationLifetime
    // private static AvaloniaView _avaloniaView;
    // public static async Task Main(string[] args)
    // {
    //     await BuildAvaloniaApp()
    //         .SetupBrowserApp();
    //
    //     _avaloniaView = new AvaloniaView("out");
    //     _avaloniaView.Content = new TextBlock { Text = "Hello world" };
    // }
    
    public static AppBuilder BuildAvaloniaApp()
           => AppBuilder.Configure<App>();
}
