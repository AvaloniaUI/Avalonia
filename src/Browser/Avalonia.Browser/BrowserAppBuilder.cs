using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Browser.Interop;
using Avalonia.Browser.Rendering;
using Avalonia.Controls;
using Avalonia.Metadata;

namespace Avalonia.Browser;

public enum BrowserRenderingMode
{
    Software2D = 1,
    WebGL1,
    WebGL2
}

public record BrowserPlatformOptions
{
    /// <summary>
    /// Gets or sets Avalonia rendering modes with fallbacks.
    /// The first element in the array has the highest priority.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">Thrown if no values were matched.</exception>
    public IReadOnlyList<BrowserRenderingMode> RenderingMode { get; set; } = new[]
    {
        BrowserRenderingMode.WebGL2, BrowserRenderingMode.WebGL1, BrowserRenderingMode.Software2D
    };

    /// <summary>
    /// Defines paths where avalonia modules and service locator should be resolved.
    /// If null, default path resolved depending on the backend (browser or blazor) is used.
    /// </summary>
    public Func<string, string>? FrameworkAssetPathResolver { get; set; }

    /// <summary>
    /// Defines if the service worker used by Avalonia should be registered.
    /// If registered, service worker can work as a save file picker fallback on the browsers that don't support native implementation.
    /// For more details, see https://github.com/jimmywarting/native-file-system-adapter#a-note-when-downloading-with-the-polyfilled-version.
    /// </summary>
    [Unstable("This property might not work reliably.")]
    public bool RegisterAvaloniaServiceWorker { get; set; }

    /// <summary>
    /// If <see cref="RegisterAvaloniaServiceWorker"/> is enabled, it is possible to redefine scope for the worker.
    /// By default, current domain root is used as a scope.
    /// </summary>
    public string? AvaloniaServiceWorkerScope { get; set; }

    /// <summary>
    /// Avalonia uses "native-file-system-adapter" polyfill for the file dialogs.
    /// If native implementation is available, by default it is used.
    /// This property forces polyfill to be always used.
    /// For more details, see https://github.com/jimmywarting/native-file-system-adapter#a-note-when-downloading-with-the-polyfilled-version.
    /// </summary>
    public bool PreferFileDialogPolyfill { get; set; }

    /// <summary>
    /// Defines if Avalonia should create a controlled dispatcher loop on the web worker thread.
    /// If used only when WasmEnableThreads is set to true. Default value is true.
    /// </summary>
    public bool? PreferManagedThreadDispatcher { get; set; } = true;
}

public static class BrowserAppBuilder
{
    /// <summary>
    /// Configures browser backend, loads avalonia javascript modules and creates a single view lifetime from the passed <see paramref="mainDivId"/> parameter.
    /// </summary>
    /// <param name="builder">Application builder.</param>
    /// <param name="mainDivId">ID of the html element where avalonia content should be rendered.</param>
    /// <param name="options">Browser backend specific options.</param>
    public static async Task StartBrowserAppAsync(
        this AppBuilder builder,
        string mainDivId, BrowserPlatformOptions? options = null)
    {
        if (mainDivId is null)
        {
            throw new ArgumentNullException(nameof(mainDivId));
        }

        builder = await PreSetupBrowser(builder, options);

        var lifetime = new BrowserSingleViewLifetime();
        builder
            .AfterApplicationSetup(_ =>
            {
                lifetime.View = new AvaloniaView(mainDivId);
            });

        if (BrowserWindowingPlatform.IsManagedDispatcherEnabled)
        {
            var tcs = new TaskCompletionSource();
            var thread = new Thread(() =>
            {
                try
                {
                    builder
                        .SetupWithLifetime(lifetime);
                    tcs.TrySetResult();
                    builder.Instance!.Run(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });
#pragma warning disable CA1416
            thread.Start();
#pragma warning restore CA1416
            await tcs.Task;
        }
        else
        {
            builder
                .SetupWithLifetime(lifetime);
        }
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

        var lifetime = new BrowserSingleViewLifetime();
        builder
            .SetupWithLifetime(lifetime);
    }

    internal static async Task<AppBuilder> PreSetupBrowser(AppBuilder builder, BrowserPlatformOptions? options)
    {
        options ??= AvaloniaLocator.Current.GetService<BrowserPlatformOptions>() ?? new BrowserPlatformOptions();
        options.FrameworkAssetPathResolver ??= fileName => $"./{fileName}";

        AvaloniaLocator.CurrentMutable.Bind<BrowserPlatformOptions>().ToConstant(options);

        await AvaloniaModule.ImportMain();

        BrowserWindowingPlatform.GlobalThis = DomHelper.GetGlobalThis();

        if (BrowserWindowingPlatform.IsThreadingEnabled)
        {
            await RenderWorker.InitializeAsync();
        }

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
            .UseBrowserRuntimePlatformSubsystem()
            .UseWindowingSubsystem(BrowserWindowingPlatform.Register)
            .UseSkia();
    }
}
