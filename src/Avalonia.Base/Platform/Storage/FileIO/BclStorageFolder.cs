using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading.Tasks;

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

    public async IAsyncEnumerable<IStorageItem> GetItemsAsync()
    {
        var items = DirectoryInfo.EnumerateDirectories()
            .Select(d => (IStorageItem)new BclStorageFolder(d))
            .Concat(DirectoryInfo.EnumerateFiles().Select(f => new BclStorageFile(f)));

        foreach (var item in items)
        {
            yield return item;
        }
    }

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

    public async Task DeleteAsync()
    {
        DirectoryInfo.Delete(true);
    }

    public async Task<IStorageItem?> MoveAsync(IStorageFolder destination)
    {
        if (destination is BclStorageFolder storageFolder)
        {
            var newPath = System.IO.Path.Combine(storageFolder.DirectoryInfo.FullName, DirectoryInfo.Name);
            DirectoryInfo.MoveTo(newPath);

            return new BclStorageFolder(new DirectoryInfo(newPath));
        }

        return null;
    }

    public async Task<IStorageFile?> CreateFileAsync(string name)
    {
        var fileName = System.IO.Path.Combine(DirectoryInfo.FullName, name);
        var newFile = new FileInfo(fileName);
        
        using var stream = newFile.Create();

        return new BclStorageFile(newFile);
    }

    public async Task<IStorageFolder?> CreateFolderAsync(string name)
    {
        var newFolder = DirectoryInfo.CreateSubdirectory(name);

        return new BclStorageFolder(newFolder);
    }
}
