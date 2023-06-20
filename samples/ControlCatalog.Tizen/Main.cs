using System;
using Avalonia.Tizen;
using ElmSharp;
using SkiaSharp;
using Tizen.Applications;

namespace ControlCatalog.Tizen;

class Program : NuiTizenApplication<App>
{
    static void Main(string[] args)
    {
        var app = new Program();
        app.Run(args);
    }
}
