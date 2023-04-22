using System;
using System.Threading.Tasks;
using Avalonia.Browser.Interop;

namespace Avalonia.Browser;

public class BrowserPlatformOptions
{
    /// <summary>
    /// Defines paths where avalonia modules and service locator should be resolved.
    /// If null, default path resolved depending on the backend (browser or blazor) is used.
    /// </summary>
    public Func<string, string>? FrameworkAssetPathResolver { get; set; }
}

public static class BrowserAppBuilder
{
    /// <summary>
    /// Configures browser backend, loads avalonia javascript modules and creates a single view lifetime from the passed <see paramref="mainDivId"/> parameter.
    /// </summary>
    /// <param name="builder">Application builder.</param>
    /// <param name="mainDivId">ID of the html element where avalonia content should be rendered.</param>
    /// <param name="options">Browser backend specific options.</param>
    public static async Task StartBrowserAppAsync(this AppBuilder builder, string mainDivId, BrowserPlatformOptions? options = null)
    {
        if (mainDivId is null)
        {
            throw new ArgumentNullException(nameof(mainDivId));
        }
        
        builder = await PreSetupBrowser(builder, options);

        var lifetime = new BrowserSingleViewLifetime();
        builder
            .AfterSetup(_ =>
            {
                lifetime.View = new AvaloniaView(mainDivId);
            })
            .SetupWithLifetime(lifetime);
    }

    /// <summary>
    /// Loads avalonia javascript modules and configures browser backend.
    /// </summary>
    /// <param name="builder">Application builder.</param>
    /// <param name="options">Browser backend specific options.</param>
    /// <remarks>
    /// This method doesn't creates any avalonia views to be rendered. To do so create an <see cref="AvaloniaView"/> object.
    /// Alternatively, you can call <see cref="StartBrowserAppAsync"/> method instead of <see cref="SetupBrowserAppAsync"/>.
    /// </remarks>
    public static async Task SetupBrowserAppAsync(this AppBuilder builder, BrowserPlatformOptions? options = null)
    {
        builder = await PreSetupBrowser(builder, options);

        builder
            .SetupWithoutStarting();
    }

    internal static async Task<AppBuilder> PreSetupBrowser(AppBuilder builder, BrowserPlatformOptions? options)
    {
        options ??= new BrowserPlatformOptions();
        options.FrameworkAssetPathResolver ??= fileName => $"./{fileName}";

        AvaloniaLocator.CurrentMutable.Bind<BrowserPlatformOptions>().ToConstant(options);
        
        await AvaloniaModule.ImportMain();

        if (builder.WindowingSubsystemInitializer is null)
        {
            builder = builder.UseBrowser();
        }

        return builder;
    }
    
    public static AppBuilder UseBrowser(
        this AppBuilder builder)
    {
        return builder
            .UseWindowingSubsystem(BrowserWindowingPlatform.Register)
            .UseSkia();
    }
}
