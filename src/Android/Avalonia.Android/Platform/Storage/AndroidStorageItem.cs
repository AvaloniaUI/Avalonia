#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using Android.Provider;
using Avalonia.Logging;
using Avalonia.Platform.Storage;
using Java.Lang;
using AndroidUri = Android.Net.Uri;
using Exception = System.Exception;
using JavaFile = Java.IO.File;

namespace Avalonia.Android.Platform.Storage;

internal abstract class AndroidStorageItem : IStorageBookmarkItem
{
    private Context? _context;

    protected AndroidStorageItem(Context context, AndroidUri uri)
    {
        _context = context;
        Uri = uri;
    }

    internal AndroidUri Uri { get; }

    protected Context Context => _context ?? throw new ObjectDisposedException(nameof(AndroidStorageItem));

    public string Name => GetColumnValue(Context, Uri, MediaStore.IMediaColumns.DisplayName)
                          ?? Uri.PathSegments?.LastOrDefault() ?? string.Empty;

    public bool CanBookmark => true;

    public Task<string?> SaveBookmarkAsync()
    {
        Context.ContentResolver?.TakePersistableUriPermission(Uri, ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantReadUriPermission);
        return Task.FromResult(Uri.ToString());
    }

    public Task ReleaseBookmarkAsync()
    {
        Context.ContentResolver?.ReleasePersistableUriPermission(Uri, ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantReadUriPermission);
        return Task.CompletedTask;
    }

    public bool TryGetUri([NotNullWhen(true)] out Uri? uri)
    {
        uri = new Uri(Uri.ToString()!);
        return true;
    }

    public abstract Task<StorageItemProperties> GetBasicPropertiesAsync();

    protected string? GetColumnValue(Context context, AndroidUri contentUri, string column, string? selection = null, string[]? selectionArgs = null)
    {
        try
        {
            var projection = new[] { column };
            using var cursor = context.ContentResolver!.Query(contentUri, projection, selection, selectionArgs, null);
            if (cursor?.MoveToFirst() == true)
            {
                var columnIndex = cursor.GetColumnIndex(column);
                if (columnIndex != -1)
                    return cursor.GetString(columnIndex);
            }
        }
        catch (Exception ex)
        {
            Logger.TryGet(LogEventLevel.Verbose, LogArea.AndroidPlatform)?.Log(this, "File metadata reader failed: '{Exception}'", ex);
        }

        return null;
    }

    public Task<IStorageFolder?> GetParentAsync()
    {
        using var javaFile = new JavaFile(Uri.Path!);

        // Java file represents files AND directories. Don't be confused.
        if (javaFile.ParentFile is {} parentFile
            && AndroidUri.FromFile(parentFile) is {} androidUri)
        {
            return Task.FromResult<IStorageFolder?>(new AndroidStorageFolder(Context, androidUri));
        }

        return Task.FromResult<IStorageFolder?>(null);
    }

    public void Dispose()
    {
        _context = null;
    }
}

internal sealed class AndroidStorageFolder : AndroidStorageItem, IStorageBookmarkFolder
{
    public AndroidStorageFolder(Context context, AndroidUri uri) : base(context, uri)
    {
    }

    public override Task<StorageItemProperties> GetBasicPropertiesAsync()
    {
        return Task.FromResult(new StorageItemProperties());
    }

    public async Task<IReadOnlyList<IStorageItem>> GetItemsAsync()
    {
        using var javaFile = new JavaFile(Uri.Path!);

        // Java file represents files AND directories. Don't be confused.
        var files = await javaFile.ListFilesAsync().ConfigureAwait(false);
        if (files is null)
        {
            return Array.Empty<IStorageItem>();
        }

        return files
            .Select(f => (file: f, uri: AndroidUri.FromFile(f)))
            .Where(t => t.uri is not null)
            .Select(t => t.file switch
            {
                { IsFile: true } => (IStorageItem)new AndroidStorageFile(Context, t.uri!),
                { IsDirectory: true } => new AndroidStorageFolder(Context, t.uri!),
                _ => null
            })
            .Where(i => i is not null)
            .ToArray()!;
    }
}

internal sealed class AndroidStorageFile : AndroidStorageItem, IStorageBookmarkFile
{
    public AndroidStorageFile(Context context, AndroidUri uri) : base(context, uri)
    {
    }

    public bool CanOpenRead => true;

    public bool CanOpenWrite => true;

    public Task<Stream> OpenReadAsync() => Task.FromResult(OpenContentStream(Context, Uri, false)
        ?? throw new InvalidOperationException("Failed to open content stream"));

    public Task<Stream> OpenWriteAsync() => Task.FromResult(OpenContentStream(Context, Uri, true)
        ?? throw new InvalidOperationException("Failed to open content stream"));

    private Stream? OpenContentStream(Context context, AndroidUri uri, bool isOutput)
    {
        var isVirtual = IsVirtualFile(context, uri);
        if (isVirtual)
        {
            Logger.TryGet(LogEventLevel.Verbose, LogArea.AndroidPlatform)?.Log(this, "Content URI was virtual: '{Uri}'", uri);
            return GetVirtualFileStream(context, uri, isOutput);
        }

        return isOutput
            ? context.ContentResolver?.OpenOutputStream(uri)
            : context.ContentResolver?.OpenInputStream(uri);
    }

    private bool IsVirtualFile(Context context, AndroidUri uri)
    {
        if (!DocumentsContract.IsDocumentUri(context, uri))
            return false;

        var value = GetColumnValue(context, uri, DocumentsContract.Document.ColumnFlags);
        if (!string.IsNullOrEmpty(value) && int.TryParse(value, out var flagsInt))
        {
            var flags = (DocumentContractFlags)flagsInt;
            return flags.HasFlag(DocumentContractFlags.VirtualDocument);
        }

        return false;
    }

    private Stream? GetVirtualFileStream(Context context, AndroidUri uri, bool isOutput)
    {
        var mimeTypes = context.ContentResolver?.GetStreamTypes(uri, FilePickerFileTypes.All.MimeTypes![0]);
        if (mimeTypes?.Length >= 1)
        {
            var mimeType = mimeTypes[0];
            var asset = context.ContentResolver!
                .OpenTypedAssetFileDescriptor(uri, mimeType, null);

            var stream = isOutput
                ? asset?.CreateOutputStream()
                : asset?.CreateInputStream();

            return stream;
        }

        return null;
    }

    public override Task<StorageItemProperties> GetBasicPropertiesAsync()
    {
        ulong? size = null;
        DateTimeOffset? itemDate = null;
        DateTimeOffset? dateModified = null;

        try
        {
            var projection = new[]
            {
                MediaStore.IMediaColumns.Size, MediaStore.IMediaColumns.DateAdded,
                MediaStore.IMediaColumns.DateModified
            };
            using var cursor = Context.ContentResolver!.Query(Uri, projection, null, null, null);

            if (cursor?.MoveToFirst() == true)
            {
                try
                {
                    var columnIndex = cursor.GetColumnIndex(MediaStore.IMediaColumns.Size);
                    if (columnIndex != -1)
                    {
                        size = (ulong)cursor.GetLong(columnIndex);
                    }
                }
                catch (Exception ex)
                {
                    Logger.TryGet(LogEventLevel.Verbose, LogArea.AndroidPlatform)?
                        .Log(this, "File Size metadata reader failed: '{Exception}'", ex);
                }

                try
                {
                    var columnIndex = cursor.GetColumnIndex(MediaStore.IMediaColumns.DateAdded);
                    if (columnIndex != -1)
                    {
                        var longValue = cursor.GetLong(columnIndex);
                        itemDate = longValue > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(longValue) : null;
                    }
                }
                catch (Exception ex)
                {
                    Logger.TryGet(LogEventLevel.Verbose, LogArea.AndroidPlatform)?
                        .Log(this, "File DateAdded metadata reader failed: '{Exception}'", ex);
                }

                try
                {
                    var columnIndex = cursor.GetColumnIndex(MediaStore.IMediaColumns.DateModified);
                    if (columnIndex != -1)
                    {
                        var longValue = cursor.GetLong(columnIndex);
                        dateModified = longValue > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(longValue) : null;
                    }
                }
                catch (Exception ex)
                {
                    Logger.TryGet(LogEventLevel.Verbose, LogArea.AndroidPlatform)?
                        .Log(this, "File DateAdded metadata reader failed: '{Exception}'", ex);
                }
            }
        }
        catch (UnsupportedOperationException)
        {
            // It's not possible to get parameters of some files/folders.
        }

        return Task.FromResult(new StorageItemProperties(size, itemDate, dateModified));
    }
}
