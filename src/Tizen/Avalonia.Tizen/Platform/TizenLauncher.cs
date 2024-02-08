using Avalonia.Platform.Storage;
using Avalonia.Tizen.Platform;
using Tizen.Applications;

namespace Avalonia.Tizen;

internal class TizenLauncher : ILauncher
{
    public async Task<bool> LaunchUriAsync(Uri uri)
    {
        if (uri is null)
        {
            throw new ArgumentNullException(nameof(uri));
        }

        if (!uri.IsAbsoluteUri)
        {
            return false;
        }

        if (!await Permissions.RequestPrivilegeAsync(Permissions.LaunchPrivilege))
        {
            return false;
        }

        var appControl = new AppControl
        {
            Operation = AppControlOperations.ShareText,
            Uri = uri.AbsoluteUri
        };

        if (uri.AbsoluteUri.StartsWith("geo:"))
            appControl.Operation = AppControlOperations.Pick;
        else if (uri.AbsoluteUri.StartsWith("http"))
            appControl.Operation = AppControlOperations.View;
        else if (uri.AbsoluteUri.StartsWith("mailto:"))
            appControl.Operation = AppControlOperations.Compose;
        else if (uri.AbsoluteUri.StartsWith("sms:"))
            appControl.Operation = AppControlOperations.Compose;
        else if (uri.AbsoluteUri.StartsWith("tel:"))
            appControl.Operation = AppControlOperations.Dial;

        AppControl.SendLaunchRequest(appControl);

        return true;
    }

    public async Task<bool> LaunchFileAsync(IStorageItem storageItem)
    {
        if (storageItem is null)
        {
            throw new ArgumentNullException(nameof(storageItem));
        }

        if (!await Permissions.RequestPrivilegeAsync(Permissions.LaunchPrivilege))
        {
            return false;
        }

        var appControl = new AppControl
        {
            Operation = AppControlOperations.View,
            Mime = "*/*",
            Uri = "file://" + storageItem.Path,
        };

        AppControl.SendLaunchRequest(appControl);

        return true;
    }
}
