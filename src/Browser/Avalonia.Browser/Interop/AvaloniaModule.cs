using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Avalonia.Browser.Interop;

internal static partial class AvaloniaModule
{
    private static readonly Lazy<Task> s_importMain = new(ImportMainToCurrentContext);
    
    public static Task ImportMainToCurrentContext()
    {
        var options = AvaloniaLocator.Current.GetService<BrowserPlatformOptions>() ?? new BrowserPlatformOptions();
        return JSHost.ImportAsync(MainModuleName, options.FrameworkAssetPathResolver!("avalonia.js"));
    }

    private static readonly Lazy<Task> s_importStorage = new(() =>
    {
        var options = AvaloniaLocator.Current.GetService<BrowserPlatformOptions>() ?? new BrowserPlatformOptions();
        return JSHost.ImportAsync(StorageModuleName, options.FrameworkAssetPathResolver!("storage.js"));
    });

    public const string MainModuleName = "avalonia";
    public const string StorageModuleName = "storage";

    public const string AssetsBasePath = "_content/Avalonia.Browser";

    public static Task ImportMain() => s_importMain.Value;

    public static Task ImportStorage() => s_importStorage.Value;

    /// <remarks>
    /// serviceWorker.register resolves the path against the document, not the caller framework,
    /// so FrameworkAssetPathResolver does not apply. The worker also has to sit at the app root:
    /// it is scoped to its own directory, and the save picker polyfill looks it up with
    /// getRegistration(), which matches against the document URL.
    /// </remarks>
    public static string ResolveServiceWorkerPath() => "./avalonia-sw.js";

    [JSImport("Caniuse.isMobile", AvaloniaModule.MainModuleName)]
    public static partial bool IsMobile();

    [JSImport("Caniuse.isTv", AvaloniaModule.MainModuleName)]
    public static partial bool IsTv();

    [JSImport("registerServiceWorker", AvaloniaModule.MainModuleName)]
    public static partial void RegisterServiceWorker(string path, string? scope);
}
