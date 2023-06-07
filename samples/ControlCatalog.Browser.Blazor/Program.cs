using System;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser.Blazor;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ControlCatalog.Browser.Blazor;

public class Program
{
    public static async Task  Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        await StartAvaloniaApp();
        await host.RunAsync();
    }

    public static async Task StartAvaloniaApp()
    {
        await AppBuilder.Configure<ControlCatalog.App>()
            .StartBlazorAppAsync();
    }
    
    public static WebAssemblyHostBuilder CreateHostBuilder(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        
        builder.RootComponents.Add<App>("#app");

        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

        return builder;
    }
}




