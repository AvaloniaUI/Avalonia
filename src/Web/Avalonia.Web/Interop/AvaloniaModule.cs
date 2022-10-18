using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Avalonia.Web.Interop;

internal static class AvaloniaModule
{
    public const string MainModuleName = "avalonia";
    public const string StorageModuleName = "storage";

    public static Task ImportMain()
    {
        var options = AvaloniaLocator.Current.GetService<BrowserPlatformOptions>() ?? new BrowserPlatformOptions();
        return JSHost.ImportAsync(MainModuleName, options.FrameworkAssetPathResolver("avalonia.js"));
    }

    public static Task ImportStorage()
    {
        var options = AvaloniaLocator.Current.GetService<BrowserPlatformOptions>() ?? new BrowserPlatformOptions();
        return JSHost.ImportAsync(StorageModuleName, options.FrameworkAssetPathResolver("storage.js"));
    }
}
