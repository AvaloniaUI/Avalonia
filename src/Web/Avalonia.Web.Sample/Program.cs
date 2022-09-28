using Avalonia;
using Avalonia.Web;
using ControlCatalog;
using ControlCatalog.Web;

internal partial class Program
{
    private static void Main(string[] args)
    {
        Emscripten.Log(EM_LOG.INFO, "Call from Main");

        BuildAvaloniaApp()
            .AfterSetup(_ =>
            {
                ControlCatalog.Pages.EmbedSample.Implementation = new EmbedSampleWeb();
            }).SetupBrowserApp("out");

        BuildAvaloniaApp().SetupBrowserApp("out");
    }

    public static AppBuilder BuildAvaloniaApp()
           => AppBuilder.Configure<App>();
}
