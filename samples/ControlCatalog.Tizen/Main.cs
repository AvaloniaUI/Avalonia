using System;
using Avalonia;
using Avalonia.Tizen;
using ElmSharp;
using SkiaSharp;
using Tizen.Applications;

namespace ControlCatalog.Tizen;

class Program : NuiTizenApplication<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder) => 
        base.CustomizeAppBuilder(builder).AfterSetup(_ =>
        {
            Pages.EmbedSample.Implementation = new EmbedSampleNuiTizen();
        });

    static void Main(string[] args)
    {
        var app = new Program();
        app.Run(args);
    }
}
