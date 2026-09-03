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

    public async Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options)
    {
        var result = await OpenFilePickerWithResultAsync(options).ConfigureAwait(false);
        return result.Files;
    }

    public async Task<OpenFilePickerResult> OpenFilePickerWithResultAsync(FilePickerOpenOptions options)
    {
        var (files, selectedType) = await native.OpenFileDialog(topLevel, options).ConfigureAwait(false);
        return new OpenFilePickerResult { Files = files, SelectedFileType = selectedType };
    }

    public async Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options)
    {
        var result = await SaveFilePickerWithResultAsync(options).ConfigureAwait(false);
        return result.File;
    }

    public async Task<SaveFilePickerResult> SaveFilePickerWithResultAsync(FilePickerSaveOptions options)
    {
        var (file, selectedType) = await native.SaveFileDialog(topLevel, options).ConfigureAwait(false);
        return new SaveFilePickerResult { File = file, SelectedFileType = selectedType };
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
