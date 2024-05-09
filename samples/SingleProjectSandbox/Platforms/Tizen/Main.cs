using Avalonia;
using Avalonia.Tizen;

namespace SingleProjectSandbox;

internal class Program : NuiTizenApplication<App>
{
    protected override AppBuilder CreateAppBuilder() =>
        App.BuildAvaloniaApp().UseTizen();

    internal static void Main(string[] args)
    {
        var app = new Program();
        app.Run(args);
    }
}
