#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;

namespace Avalonia.Native;

internal sealed class StorageProviderImpl(TopLevelImpl topLevel, StorageProviderApi native) : IStorageProvider
{
    public bool CanOpen => true;

    public bool CanSave => true;

    public bool CanPickFolder => true;

    public Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options)
    {
        return native.OpenFileDialog(topLevel, options);
    }

    public Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options)
    {
        return native.SaveFileDialog(topLevel, options);
    }

    public Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
    {
        return native.SelectFolderDialog(topLevel, options);
    }

    public Task<IStorageBookmarkFile?> OpenFileBookmarkAsync(string bookmark)
    {
        return Task.FromResult(native.TryGetStorageItem(native.ReadBookmark(bookmark, false)) as IStorageBookmarkFile);
    }

    public Task<IStorageBookmarkFolder?> OpenFolderBookmarkAsync(string bookmark)
    {
        return Task.FromResult(native.TryGetStorageItem(native.ReadBookmark(bookmark, true)) as IStorageBookmarkFolder);
    }

    public Task<IStorageFile?> TryGetFileFromPathAsync(Uri fileUri)
    {
        return Task.FromResult(native.TryGetStorageItem(fileUri) as IStorageFile);
    }

    public Task<IStorageFolder?> TryGetFolderFromPathAsync(Uri folderPath)
    {
        return Task.FromResult(native.TryGetStorageItem(folderPath) as IStorageFolder);
    }

    public Task<IStorageFolder?> TryGetWellKnownFolderAsync(WellKnownFolder wellKnownFolder)
    {
        if (BclStorageProvider.TryGetWellKnownFolderCore(wellKnownFolder) is { } directoryInfo)
        {
            return Task.FromResult<IStorageFolder?>(new BclStorageFolder(directoryInfo));
        }

        return Task.FromResult<IStorageFolder?>(null);
    }
}
