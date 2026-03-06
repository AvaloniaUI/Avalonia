using System;
using System.Threading.Tasks;
using Avalonia.Browser.Interop;
using Avalonia.Platform.Storage;

namespace Avalonia.Browser.Storage;

internal class BrowserLauncher : ILauncher
{
    public Task<bool> LaunchUriAsync(Uri uri)
    {
        _ = uri ?? throw new ArgumentNullException(nameof(uri));

        if (uri.IsAbsoluteUri)
        {
            return Task.FromResult(NavigationHelper.WindowOpen(uri.AbsoluteUri, "_blank"));
        }
        return Task.FromResult(false);
    }

    public Task<bool> LaunchFileAsync(IStorageItem storageItem)
    {
        _ = storageItem ?? throw new ArgumentNullException(nameof(storageItem));

        return Task.FromResult(false);
    }
}
