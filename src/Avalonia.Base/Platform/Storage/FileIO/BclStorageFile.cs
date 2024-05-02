using System;
using System.IO;
using System.Security;
using System.Threading.Tasks;

namespace Avalonia.Platform.Storage.FileIO;

internal class BclStorageFile : IStorageBookmarkFile
{
    public BclStorageFile(FileInfo fileInfo)
    {
        FileInfo = fileInfo ?? throw new ArgumentNullException(nameof(fileInfo));
    }

    public FileInfo FileInfo { get; }
    
    public string Name => FileInfo.Name;

    public virtual bool CanBookmark => true;

    public Uri Path
    {
        get
        {
            try
            {
                if (FileInfo.Directory is not null)
                {
                    return StorageProviderHelpers.FilePathToUri(FileInfo.FullName);
                } 
            }
            catch (SecurityException)
            {
            }
            return new Uri(FileInfo.Name, UriKind.Relative);
        }
    }
    
    public Task<StorageItemProperties> GetBasicPropertiesAsync()
    {
        if (FileInfo.Exists)
        {
            return Task.FromResult(new StorageItemProperties(
                (ulong)FileInfo.Length,
                FileInfo.CreationTimeUtc,
                FileInfo.LastAccessTimeUtc));
        }
        return Task.FromResult(new StorageItemProperties());
    }

    public Task<IStorageFolder?> GetParentAsync()
    {
        if (FileInfo.Directory is { } directory)
        {
            return Task.FromResult<IStorageFolder?>(new BclStorageFolder(directory));
        }
        return Task.FromResult<IStorageFolder?>(null);
    }

    public Task<Stream> OpenReadAsync()
    {
        return Task.FromResult<Stream>(FileInfo.OpenRead());
    }

    public Task<Stream> OpenWriteAsync()
    {
        var stream = new FileStream(FileInfo.FullName, FileMode.Create, FileAccess.Write, FileShare.Write);
        return Task.FromResult<Stream>(stream);
    }

    public virtual Task<string?> SaveBookmarkAsync()
    {
        return Task.FromResult<string?>(FileInfo.FullName);
    }

    public Task ReleaseBookmarkAsync()
    {
        // No-op
        return Task.CompletedTask;
    }

    protected virtual void Dispose(bool disposing)
    {
    }

    ~BclStorageFile()
    {
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public Task DeleteAsync()
    {
        FileInfo.Delete();
        return Task.CompletedTask;
    }

    public Task<IStorageItem?> MoveAsync(IStorageFolder destination)
    {
        if (destination is BclStorageFolder storageFolder)
        {
            var newPath = System.IO.Path.Combine(storageFolder.DirectoryInfo.FullName, FileInfo.Name);
            FileInfo.MoveTo(newPath);

            return Task.FromResult<IStorageItem?>(new BclStorageFile(new FileInfo(newPath)));
        }

        return Task.FromResult<IStorageItem?>(null);
    }
}
