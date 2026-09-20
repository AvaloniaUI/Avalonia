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

    /// <remark>
    /// Service locator path is resolved relative to the document root,
    /// Not relatively to the caller framework, so we can't use FrameworkAssetPathResolver.
    /// </remark>
    public static string ResolveServiceWorkerPath() => $"./{AssetsBasePath}/sw.js";

    [JSImport("Caniuse.isMobile", AvaloniaModule.MainModuleName)]
    public static partial bool IsMobile();

    [JSImport("Caniuse.isTv", AvaloniaModule.MainModuleName)]
    public static partial bool IsTv();

    [JSImport("registerServiceWorker", AvaloniaModule.MainModuleName)]
    public static partial void RegisterServiceWorker(string path, string? scope);
}
