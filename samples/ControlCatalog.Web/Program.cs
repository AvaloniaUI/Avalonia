using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Web;
using ControlCatalog;
using ControlCatalog.Web;

[assembly:SupportedOSPlatform("browser")]

internal partial class Program
{
    private static void Main(string[] args)
    {
        BuildAvaloniaApp()
            .AfterSetup(_ =>
            {
                ControlCatalog.Pages.EmbedSample.Implementation = new EmbedSampleWeb();
            }).SetupBrowserApp("out");
    }

    public static AppBuilder BuildAvaloniaApp()
           => AppBuilder.Configure<App>();
}
