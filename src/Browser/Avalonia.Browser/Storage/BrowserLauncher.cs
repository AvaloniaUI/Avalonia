using System;
using System.Threading.Tasks;
using Avalonia.Browser.Interop;
using Avalonia.Platform.Storage;

namespace Avalonia.Browser.Storage;

internal class BrowserLauncher : Launcher
{
    protected override Task<bool> LaunchUriAsyncImpl(Uri uri)
    {
        _ = uri ?? throw new ArgumentNullException(nameof(uri));

        if (uri.IsAbsoluteUri)
        {
            var window = NavigationHelper.WindowOpen(uri.AbsoluteUri, "_blank");
            return Task.FromResult(window is not null);
        }
        return Task.FromResult(false);
    }

    protected override Task<bool> LaunchFileAsyncImpl(IStorageItem storageItem)
    {
        _ = storageItem ?? throw new ArgumentNullException(nameof(storageItem));

        return Task.FromResult(false);
    }
}
