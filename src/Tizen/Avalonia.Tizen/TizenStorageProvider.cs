using System.Security;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;
using Avalonia.Tizen.Platform;
using Tizen.Applications;

namespace Avalonia.Tizen;
internal class TizenStorageProvider : IStorageProvider
{
    public bool CanOpen => true;

    public bool CanSave => false;

    public bool CanPickFolder => false;

    public Task<IStorageBookmarkFile?> OpenFileBookmarkAsync(string bookmark)
    {
        return Task.FromException<IStorageBookmarkFile?>(
           new PlatformNotSupportedException("Bookmark is not supported by Tizen"));
    }

    private static async Task CheckPermission()
    {
        Permissions.EnsureDeclared(Permissions.AppManagerLaunchPrivilege);
        if (await Permissions.RequestPrivilegeAsync(Permissions.MediaStoragePrivilege) == false)
        {
            throw new SecurityException("Application doesn't have storage permission.");
        }
    }

    public async Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options)
    {
        await CheckPermission();

        var tcs = new TaskCompletionSource<IReadOnlyList<IStorageFile>>();

#pragma warning disable CS8603 // Possible null reference return.
        var fileType = options.FileTypeFilter?
            .Where(w => w.MimeTypes != null)
            .SelectMany(s => s.MimeTypes);
#pragma warning restore CS8603 // Possible null reference return.

        var appControl = new AppControl
        {
            Operation = AppControlOperations.Pick,
            Mime = fileType?.Any() == true
                ? fileType.Aggregate((o, n) => o + ";" + n)
                : "*/*"
        };
        appControl.ExtraData.Add(AppControlData.SectionMode, options.AllowMultiple ? "multiple" : "single");
        if (options.SuggestedStartLocation?.Path is { } startupPath)
            appControl.ExtraData.Add(AppControlData.Path, startupPath.ToString());
        appControl.LaunchMode = AppControlLaunchMode.Single;

        var fileResults = new List<IStorageFile>();

        AppControl.SendLaunchRequest(appControl, (_, reply, result) =>
        {
            if (result == AppControlReplyResult.Succeeded)
            {
                if (reply.ExtraData.Count() > 0)
                {
                    var selectedFiles = reply.ExtraData.Get<IEnumerable<string>>(AppControlData.Selected).ToList();
                    fileResults.AddRange(selectedFiles.Select(f => new BclStorageFile(new(f))));
                }
            }

            tcs.TrySetResult(fileResults);
        });

        return await tcs.Task;
    }

    public Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
    {
        return Task.FromException<IReadOnlyList<IStorageFolder>>(
            new PlatformNotSupportedException("Open folder is not supported by Tizen"));
    }

    public Task<IStorageBookmarkFolder?> OpenFolderBookmarkAsync(string bookmark)
    {
        return Task.FromException<IStorageBookmarkFolder?>(
           new PlatformNotSupportedException("Open folder is not supported by Tize"));
    }

    public Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options)
    {
        return Task.FromException<IStorageFile?>(
            new PlatformNotSupportedException("Save file picker is not supported by Tizen"));
    }

    public async Task<IStorageFile?> TryGetFileFromPathAsync(Uri filePath)
    {
        await CheckPermission();

        if (filePath is not { IsAbsoluteUri: true, Scheme: "file" })
        {
            throw new ArgumentException("File path is expected to be an absolute link with \"file\" scheme.");
        }

        var path = Path.Combine(global::Tizen.Applications.Application.Current.DirectoryInfo.Resource, filePath.AbsolutePath);
        var file = new FileInfo(path);
        if (!file.Exists)
        {
            return null;
        }

        return new BclStorageFile(file);
    }

    public async Task<IStorageFolder?> TryGetFolderFromPathAsync(Uri folderPath)
    {
        if (folderPath is null)
        {
            throw new ArgumentNullException(nameof(folderPath));
        }

        await CheckPermission();

        if (folderPath is not { IsAbsoluteUri: true, Scheme: "file" })
        {
            throw new ArgumentException("File path is expected to be an absolute link with \"file\" scheme.");
        }

        var path = Path.Combine(global::Tizen.Applications.Application.Current.DirectoryInfo.Resource, folderPath.AbsolutePath);
        var directory = new System.IO.DirectoryInfo(path);
        if (!directory.Exists)
            return null;

        return new BclStorageFolder(directory);
    }

    public Task<IStorageFolder?> TryGetWellKnownFolderAsync(WellKnownFolder wellKnownFolder)
    {
        var folder = wellKnownFolder switch
        {
            WellKnownFolder.Desktop => null,
            WellKnownFolder.Documents => global::Tizen.Applications.Application.Current.DirectoryInfo.Data,
            WellKnownFolder.Downloads => global::Tizen.Applications.Application.Current.DirectoryInfo.SharedData,
            WellKnownFolder.Music => null,
            WellKnownFolder.Pictures => null,
            WellKnownFolder.Videos => null,
            _ => throw new ArgumentOutOfRangeException(nameof(wellKnownFolder), wellKnownFolder, null),
        };

        if (folder == null)
            return Task.FromResult<IStorageFolder?>(null);

        var storageFolder = new BclStorageFolder(new System.IO.DirectoryInfo(folder));
        return Task.FromResult<IStorageFolder?>(storageFolder);
    }
}
