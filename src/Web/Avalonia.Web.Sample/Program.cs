using System;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using System.Xml.Linq;
using Avalonia.Web;
//using SkiaSharp;

internal partial class Program
{

    [JSImport("globalThis.document.getElementById")]
    internal static partial JSObject GetElementById(string id);

    private static void Main(string[] args)
    {
        Console.WriteLine("Hello, Browser!");

        Console.WriteLine();

        foreach(var arg in args)
        {
            Console.WriteLine(arg);
        }

        var div = GetElementById("out");
        Console.WriteLine("got div");

        var canvas = AvaloniaRuntime.CreateCanvas(div);

        Console.WriteLine("Created canvas");
        
    }
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
