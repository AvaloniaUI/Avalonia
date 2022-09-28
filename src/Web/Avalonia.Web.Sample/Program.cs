using Avalonia;
using Avalonia.Web;
using ControlCatalog;

internal partial class Program
{
    private static void Main(string[] args)
    {
        Emscripten.Log(EM_LOG.ERROR, "MyError");

        BuildAvaloniaApp().SetupBrowserApp("out");
    }

    public static AppBuilder BuildAvaloniaApp()
           => AppBuilder.Configure<App>();
}
