using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Browser;
using ControlCatalog;
using ControlCatalog.Browser;

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
