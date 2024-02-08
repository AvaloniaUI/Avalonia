using System;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Foundation;
using UIKit;

namespace Avalonia.iOS;

internal class IOSLauncher : ILauncher
{
    public Task<bool> LaunchUriAsync(Uri uri)
    {
        _ = uri ?? throw new ArgumentNullException(nameof(uri));

        if (uri.IsAbsoluteUri && UIApplication.SharedApplication.CanOpenUrl(uri))
        {
            return UIApplication.SharedApplication.OpenUrlAsync(uri!, new UIApplicationOpenUrlOptions());
        }

        return Task.FromResult(false);
    }

    public Task<bool> LaunchFileAsync(IStorageItem storageItem)
    {
        _ = storageItem ?? throw new ArgumentNullException(nameof(storageItem));

#if !TVOS
        var uri = (storageItem as Storage.IOSStorageItem)?.Url
                  ?? (storageItem.TryGetLocalPath() is { } localPath ? NSUrl.FromFilename(localPath) : null);
        if (uri is not null)
        {
            var documentController = new UIDocumentInteractionController()
            {
                Name = storageItem.Name,
                Url = uri
            };

            var result = documentController.PresentPreview(true);
            return Task.FromResult(result);
        }
#endif

        return Task.FromResult(false);
    }
}
