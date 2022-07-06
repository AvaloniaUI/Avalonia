using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
            _filePath = doc.FileUrl?.Path ?? url.FilePathUrl.Path;
            Name = doc.LocalizedName ?? Path.GetFileName(_filePath) ?? url.FilePathUrl.LastPathComponent;
        }
    }

    internal NSUrl Url { get; }

    public bool CanBookmark => true;

    public string Name { get; }

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

    public Task ReleaseBookmark()
    {
        // no-op
        return Task.CompletedTask;
    }

    public Task<string?> SaveBookmark()
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

    public bool TryGetUri([NotNullWhen(true)] out Uri uri)
    {
        uri = Url;
        return uri is not null;
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

    public bool CanOpenRead => true;

    public bool CanOpenWrite => true;

    public Task<Stream> OpenRead()
    {
        return Task.FromResult<Stream>(new IOSSecurityScopedStream(Url, FileAccess.Read));
    }

    public Task<Stream> OpenWrite()
    {
        return Task.FromResult<Stream>(new IOSSecurityScopedStream(Url, FileAccess.Write));
    }
}

internal sealed class IOSStorageFolder : IOSStorageItem, IStorageBookmarkFolder
{
    public IOSStorageFolder(NSUrl url) : base(url)
    {
    }

    public Task<IReadOnlyList<IStorageItem>> GetItemsAsync()
    {
        var content = NSFileManager.DefaultManager.GetDirectoryContent(Url, null, NSDirectoryEnumerationOptions.None, out var error);
        if (error is not null)
        {
            return Task.FromException<IReadOnlyList<IStorageItem>>(new NSErrorException(error));
        }

        var items = content
            .Select(u => u.HasDirectoryPath ? (IStorageItem)new IOSStorageFolder(u) : new IOSStorageFile(u))
            .ToArray();

        return Task.FromResult<IReadOnlyList<IStorageItem>>(items);
    }
}
