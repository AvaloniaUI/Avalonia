using System;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Web;
using Avalonia.Web.Blazor;
using ControlCatalog;
//using SkiaSharp;

internal partial class Program
{

    [JSImport("globalThis.document.getElementById")]
    internal static partial JSObject GetElementById(string id);

    private static void Main(string[] args)
    {
        BuildAvaloniaApp().SetupBrowserApp("out");
    }

    public static AppBuilder BuildAvaloniaApp()
           => AppBuilder.Configure<App>();
}

public partial class MyClass
{
    [JSExport]
    internal static async Task TestDynamicModule()
    {
        await JSHost.ImportAsync("storage.ts", "./storage.js");
        var fileApiSupported = AvaloniaRuntime.IsFileApiSupported();

        Console.WriteLine("DynamicModule result: " + fileApiSupported);
    }
}
