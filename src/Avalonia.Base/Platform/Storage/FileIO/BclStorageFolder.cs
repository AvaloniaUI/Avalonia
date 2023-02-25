﻿using System;
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

    public Task<IReadOnlyList<IStorageItem>> GetItemsAsync()
    {
         var items = DirectoryInfo.GetDirectories()
            .Select(d => (IStorageItem)new BclStorageFolder(d))
            .Concat(DirectoryInfo.GetFiles().Select(f => new BclStorageFile(f)))
            .ToArray();

         return Task.FromResult<IReadOnlyList<IStorageItem>>(items);
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
}
