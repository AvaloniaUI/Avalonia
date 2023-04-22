using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Logging;
using Avalonia.Platform.Storage;
using Foundation;

using UIKit;

#nullable enable

namespace Avalonia.iOS.Storage;

internal abstract class IOSStorageItem : IStorageBookmarkItem
{
    private readonly string _filePath;

    protected IOSStorageItem(NSUrl url)
    {
        Url = url ?? throw new ArgumentNullException(nameof(url));

        using (var doc = new UIDocument(url))
        {
            _filePath = doc.FileUrl?.Path ?? url.FilePathUrl?.Path ?? string.Empty;
            Name = doc.LocalizedName 
                ?? System.IO.Path.GetFileName(_filePath) 
                ?? url.FilePathUrl?.LastPathComponent
                ?? string.Empty;
        }
    }

    internal NSUrl Url { get; }
    internal string FilePath => _filePath;

    public bool CanBookmark => true;

    public string Name { get; }
    public Uri Path => Url!;

    public Task<StorageItemProperties> GetBasicPropertiesAsync()
    {
        var attributes = NSFileManager.DefaultManager.GetAttributes(_filePath, out var error);
        if (error is not null)
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.IOSPlatform)?.
                Log(this, "GetBasicPropertiesAsync returned an error: {ErrorCode} {ErrorMessage}", error.Code, error.LocalizedFailureReason);
        }
        return Task.FromResult(new StorageItemProperties(attributes?.Size, (DateTime)attributes?.CreationDate, (DateTime)attributes?.ModificationDate));
    }

    public Task<IStorageFolder?> GetParentAsync()
    {
        return Task.FromResult<IStorageFolder?>(new IOSStorageFolder(Url.RemoveLastPathComponent()));
    }

    public Task DeleteAsync()
    {
        return NSFileManager.DefaultManager.Remove(Url, out var error)
            ? Task.CompletedTask
            : Task.FromException(new NSErrorException(error));
    }

    public async Task<IStorageItem?> MoveAsync(IStorageFolder destination)
    {
        if (destination is not IOSStorageFolder folder)
        {
            throw new InvalidOperationException("Destination folder must be initialized the StorageProvider API.");
        }

        var isDir = this is IStorageFolder;
        var newPath = new NSUrl(System.IO.Path.Combine(folder.FilePath, Name), isDir);

        if (NSFileManager.DefaultManager.Move(folder.Url, newPath, out var error))
        {
            return isDir
                ? new IOSStorageFolder(newPath)
                : new IOSStorageFile(newPath);
        }

        if (error is not null)
        {
            throw new NSErrorException(error);
        }

        return null;
    }

    public Task ReleaseBookmarkAsync()
    {
        // no-op
        return Task.CompletedTask;
    }

    public Task<string?> SaveBookmarkAsync()
    {
        try
        {
            if (!Url.StartAccessingSecurityScopedResource())
            {
                return Task.FromResult<string?>(null);
            }

            var newBookmark = Url.CreateBookmarkData(NSUrlBookmarkCreationOptions.SuitableForBookmarkFile, Array.Empty<string>(), null, out var bookmarkError);
            if (bookmarkError is not null)
            {
                Logger.TryGet(LogEventLevel.Error, LogArea.IOSPlatform)?.
                    Log(this, "SaveBookmark returned an error: {ErrorCode} {ErrorMessage}", bookmarkError.Code, bookmarkError.LocalizedFailureReason);
                return Task.FromResult<string?>(null);
            }

            return Task.FromResult<string?>(
                newBookmark.GetBase64EncodedString(NSDataBase64EncodingOptions.None));
        }
        finally
        {
            Url.StopAccessingSecurityScopedResource();
        }
    }

    public void Dispose()
    {
    }
}

internal sealed class IOSStorageFile : IOSStorageItem, IStorageBookmarkFile
{
    public IOSStorageFile(NSUrl url) : base(url)
    {
    }
    
    public Task<Stream> OpenReadAsync()
    {
        return Task.FromResult<Stream>(new IOSSecurityScopedStream(Url, FileAccess.Read));
    }

    public Task<Stream> OpenWriteAsync()
    {
        return Task.FromResult<Stream>(new IOSSecurityScopedStream(Url, FileAccess.Write));
    }
}

internal sealed class IOSStorageFolder : IOSStorageItem, IStorageBookmarkFolder
{
    public IOSStorageFolder(NSUrl url) : base(url)
    {
    }

    public async IAsyncEnumerable<IStorageItem> GetItemsAsync()
    {
        // TODO: find out if it can be lazily enumerated.
        var tcs = new TaskCompletionSource<IReadOnlyList<IStorageItem>>();

        new NSFileCoordinator().CoordinateRead(Url,
            NSFileCoordinatorReadingOptions.WithoutChanges,
            out var error,
            uri =>
            {
                var content = NSFileManager.DefaultManager.GetDirectoryContent(uri, null, NSDirectoryEnumerationOptions.None, out var error);
                if (error is not null)
                {
                    tcs.TrySetException(new NSErrorException(error));
                }
                else
                {
                    var items = content
                        .Select(u => u.HasDirectoryPath ? (IStorageItem)new IOSStorageFolder(u) : new IOSStorageFile(u))
                        .ToArray();
                    tcs.TrySetResult(items);
                }
            });
        
        if (error is not null)
        {
            throw new NSErrorException(error);
        }

        var items = await tcs.Task;
        foreach (var item in items)
        {
            yield return item;
        }
    }

    public Task<IStorageFile?> CreateFileAsync(string name)
    {
        var path = System.IO.Path.Combine(FilePath, name);
        NSFileAttributes? attributes = null;
        if (NSFileManager.DefaultManager.CreateFile(path, null, attributes))
        {
            return Task.FromResult<IStorageFile?>(new IOSStorageFile(new NSUrl(path, false)));
        }

        return Task.FromResult<IStorageFile?>(null);
    }

    public Task<IStorageFolder?> CreateFolderAsync(string name)
    {
        var path = System.IO.Path.Combine(FilePath, name);
        NSFileAttributes? attributes = null;
        if (NSFileManager.DefaultManager.CreateDirectory(path, false, attributes, out var error))
        {
            return Task.FromResult<IStorageFolder?>(new IOSStorageFolder(new NSUrl(path, true)));
        }

        if (error is not null)
        {
            throw new NSErrorException(error);
        }
        
        return Task.FromResult<IStorageFolder?>(null);
    }
}
