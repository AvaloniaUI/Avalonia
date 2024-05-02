using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Avalonia.Utilities;

namespace Avalonia.Platform.Storage.FileIO;

internal class BclStorageFolder : IStorageBookmarkFolder
{
    public BclStorageFolder(DirectoryInfo directoryInfo)
    {
        DirectoryInfo = directoryInfo ?? throw new ArgumentNullException(nameof(directoryInfo));
        if (!DirectoryInfo.Exists)
        {
            throw new ArgumentException("Directory must exist", nameof(directoryInfo));
        }
    }

    public string Name => DirectoryInfo.Name;

    public DirectoryInfo DirectoryInfo { get; }

    public bool CanBookmark => true;

    public Uri Path
    {
        get
        {
            try
            {
                return StorageProviderHelpers.FilePathToUri(DirectoryInfo.FullName);
            }
            catch (SecurityException)
            {
                return new Uri(DirectoryInfo.Name, UriKind.Relative);
            }
        }
    }
    
    public Task<StorageItemProperties> GetBasicPropertiesAsync()
    {
        var props = new StorageItemProperties(
            null,
            DirectoryInfo.CreationTimeUtc,
            DirectoryInfo.LastAccessTimeUtc);
        return Task.FromResult(props);
    }

    public Task<IStorageFolder?> GetParentAsync()
    {
        if (DirectoryInfo.Parent is { } directory)
        {
            return Task.FromResult<IStorageFolder?>(new BclStorageFolder(directory));
        }
        return Task.FromResult<IStorageFolder?>(null);
    }

    public IAsyncEnumerable<IStorageItem> GetItemsAsync()
        => DirectoryInfo.EnumerateDirectories()
            .Select(d => (IStorageItem)new BclStorageFolder(d))
            .Concat(DirectoryInfo.EnumerateFiles().Select(f => new BclStorageFile(f)))
            .AsAsyncEnumerable();

    public virtual Task<string?> SaveBookmarkAsync()
    {
        return Task.FromResult<string?>(DirectoryInfo.FullName);
    }
    
    public Task ReleaseBookmarkAsync()
    {
        // No-op
        return Task.CompletedTask;
    }

    protected virtual void Dispose(bool disposing)
    {
    }

    ~BclStorageFolder()
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
        DirectoryInfo.Delete(true);
        return Task.CompletedTask;
    }

    public Task<IStorageItem?> MoveAsync(IStorageFolder destination)
    {
        if (destination is BclStorageFolder storageFolder)
        {
            var newPath = System.IO.Path.Combine(storageFolder.DirectoryInfo.FullName, DirectoryInfo.Name);
            DirectoryInfo.MoveTo(newPath);

            return Task.FromResult<IStorageItem?>(new BclStorageFolder(new DirectoryInfo(newPath)));
        }

        return Task.FromResult<IStorageItem?>(null);
    }

    public Task<IStorageFile?> CreateFileAsync(string name)
    {
        var fileName = System.IO.Path.Combine(DirectoryInfo.FullName, name);
        var newFile = new FileInfo(fileName);
        
        using var stream = newFile.Create();

        return Task.FromResult<IStorageFile?>(new BclStorageFile(newFile));
    }

    public Task<IStorageFolder?> CreateFolderAsync(string name)
    {
        var newFolder = DirectoryInfo.CreateSubdirectory(name);

        return Task.FromResult<IStorageFolder?>(new BclStorageFolder(newFolder));
    }
}
