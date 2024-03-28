using System;
using Avalonia;
using Avalonia.Tizen;
using ElmSharp;
using SkiaSharp;
using Tizen.Applications;

namespace MobileSandbox;

class Program : NuiTizenApplication<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder) => 
        base.CustomizeAppBuilder(builder);

    static void Main(string[] args)
    {
        var app = new Program();
        app.Run(args);
    }
}
