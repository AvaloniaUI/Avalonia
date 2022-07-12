using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Platform.Storage.FileIO;

[Unstable]
public abstract class BclStorageProvider : IStorageProvider
{
    public abstract bool CanOpen { get; }
    public abstract Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options);

    public abstract bool CanSave { get; }
    public abstract Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options);

    public abstract bool CanPickFolder { get; }
    public abstract Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options);

    public virtual Task<IStorageBookmarkFile?> OpenFileBookmarkAsync(string bookmark)
    {
        var file = new FileInfo(bookmark);
        return file.Exists
            ? Task.FromResult<IStorageBookmarkFile?>(new BclStorageFile(file))
            : Task.FromResult<IStorageBookmarkFile?>(null);
    }

    public virtual Task<IStorageBookmarkFolder?> OpenFolderBookmarkAsync(string bookmark)
    {
        var folder = new DirectoryInfo(bookmark);
        return folder.Exists
            ? Task.FromResult<IStorageBookmarkFolder?>(new BclStorageFolder(folder))
            : Task.FromResult<IStorageBookmarkFolder?>(null);
    }
}
