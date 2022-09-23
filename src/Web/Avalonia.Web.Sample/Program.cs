using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Avalonia.Web;
//using SkiaSharp;

internal class Program
{

    
    private static void Main(string[] args)
    {
        Console.WriteLine("Hello, Browser!");

        Console.WriteLine();
        

        foreach(var arg in args)
        {
            Console.WriteLine(arg);
        }
        AvaloniaRuntime.Init(); 
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
