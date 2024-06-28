using System.IO;
using System.Threading.Tasks;

namespace Avalonia.Platform.Storage.FileIO;

internal sealed class BclStorageFile(FileInfo fileInfo) : BclStorageItem(fileInfo), IStorageBookmarkFile
{
    public Task<Stream> OpenReadAsync() => Task.FromResult<Stream>(OpenReadCore(fileInfo));
    public Task<Stream> OpenWriteAsync() => Task.FromResult<Stream>(OpenWriteCore(fileInfo));
}
