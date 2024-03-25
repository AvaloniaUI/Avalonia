using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser.Interop;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.Browser.Blazor;

public static class BlazorAppBuilder
{
    /// <summary>
    /// Configures blazor backend, loads avalonia javascript modules and creates a single view lifetime.
    /// </summary>
    /// <param name="builder">Application builder.</param>
    /// <param name="options">Browser backend specific options.</param>
    public static async Task StartBlazorAppAsync(this AppBuilder builder, BrowserPlatformOptions? options = null)
    {
        builder = await BrowserAppBuilder.PreSetupBrowser(builder, options);

        builder.SetupWithLifetime(new BlazorSingleViewLifetime());
    }

    internal class BlazorSingleViewLifetime : ISingleViewApplicationLifetime
    {
        public Control? MainView { get; set; }
    }
}
