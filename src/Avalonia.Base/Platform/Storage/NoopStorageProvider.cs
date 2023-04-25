using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Platform.Storage.FileIO;

namespace Avalonia.Platform.Storage;

internal class NoopStorageProvider : BclStorageProvider
{
    public override bool CanOpen => false;
    public override Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options)
    {
        return Task.FromResult<IReadOnlyList<IStorageFile>>(Array.Empty<IStorageFile>());
    }

    public override bool CanSave => false;
    public override Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options)
    {
        return Task.FromResult<IStorageFile?>(null);
    }

    public override bool CanPickFolder => false;
    public override Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
    {
        return Task.FromResult<IReadOnlyList<IStorageFolder>>(Array.Empty<IStorageFolder>());
    }
}
