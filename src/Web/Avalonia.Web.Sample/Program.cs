using Avalonia;
using Avalonia.Web;
using ControlCatalog;

internal partial class Program
{
    private static void Main(string[] args)
    {
        BuildAvaloniaApp().SetupBrowserApp("out");
    }

    public static AppBuilder BuildAvaloniaApp()
           => AppBuilder.Configure<App>();
}
