using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Web;
using Avalonia;
using Avalonia.Browser;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Logging;
using Avalonia.Rendering;
using Avalonia.Threading;
using ControlCatalog;
using ControlCatalog.Browser;

[assembly:SupportedOSPlatform("browser")]
#nullable enable

internal partial class Program
{
    public static async Task Main(string[] args)
    {
        Trace.Listeners.Add(new ConsoleTraceListener());
    
        var options = ParseArgs(args) ?? new BrowserPlatformOptions();
    
        await BuildAvaloniaApp()
            .LogToTrace()
            .AfterSetup(_ =>
            {
                ControlCatalog.Pages.EmbedSample.Implementation = new EmbedSampleWeb();
            })
            .StartBrowserAppAsync("out", options);

        Dispatcher.UIThread.Invoke(() =>
        {
            if (Application.Current!.ApplicationLifetime is ISingleTopLevelApplicationLifetime lifetime)
            {
                lifetime.TopLevel!.RendererDiagnostics.DebugOverlays = RendererDebugOverlays.Fps;
            }
        });
    }

    // Test with multiple AvaloniaView at once. 
    // private static AvaloniaView _avaloniaView1;
    // private static AvaloniaView _avaloniaView2;
    // public static async Task Main(string[] args)
    // {
    //     Trace.Listeners.Add(new ConsoleTraceListener());
    //
    //     var options = ParseArgs(args) ?? new BrowserPlatformOptions();
    //
    //     await BuildAvaloniaApp()
    //         .LogToTrace()
    //         .SetupBrowserAppAsync(options);
    //
    //     _avaloniaView1 = new AvaloniaView("out1");
    //     _avaloniaView1.Content = new TextBlock { Text = "Hello" };
    //
    //     _avaloniaView2 = new AvaloniaView("out2");
    //     _avaloniaView2.Content = new TextBlock { Text = "World" };
    //
    //     Dispatcher.UIThread.Invoke(() =>
    //     {
    //         var topLevel = TopLevel.GetTopLevel(_avaloniaView1.Content);
    //         topLevel!.RendererDiagnostics.DebugOverlays = RendererDebugOverlays.Fps;
    //     });
    // }
    
    public static AppBuilder BuildAvaloniaApp()
           => AppBuilder.Configure<App>();

    private static BrowserPlatformOptions? ParseArgs(string[] args)
    {
        try
        {
            if (args.Length == 0
                || !Uri.TryCreate(args[0], UriKind.Absolute, out var uri)
                || uri.Query.Length <= 1)
            {
                uri = new Uri("http://localhost");
            }

            var queryParams = HttpUtility.ParseQueryString(uri.Query);
            var options = new BrowserPlatformOptions();

            if (bool.TryParse(queryParams[nameof(options.PreferFileDialogPolyfill)], out var preferDialogsPolyfill))
            {
                options.PreferFileDialogPolyfill = preferDialogsPolyfill;
            }

            if (queryParams[nameof(options.RenderingMode)] is { } renderingModePairs)
            {
                options.RenderingMode = renderingModePairs
                    .Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(entry => Enum.Parse<BrowserRenderingMode>(entry, true))
                    .ToArray();
            }

            Console.WriteLine("DemoBrowserPlatformOptions.PreferFileDialogPolyfill: " + options.PreferFileDialogPolyfill);
            Console.WriteLine("DemoBrowserPlatformOptions.RenderingMode: " + string.Join(";", options.RenderingMode));
            return options;
        }
        catch (Exception ex)
        {
            Console.WriteLine("ParseArgs of DemoBrowserPlatformOptions failed: " + ex);
            return null;
        }
    }
}
