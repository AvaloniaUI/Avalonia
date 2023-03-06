#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Android;
using Android.App;
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
    private Activity? _activity;
    private readonly bool _needsExternalFilesPermission;

    protected AndroidStorageItem(Activity activity, AndroidUri uri, bool needsExternalFilesPermission)
    {
        _activity = activity;
        _needsExternalFilesPermission = needsExternalFilesPermission;
        Uri = uri;
    }

    internal AndroidUri Uri { get; }
    
    protected Activity Activity => _activity ?? throw new ObjectDisposedException(nameof(AndroidStorageItem));

    public virtual string Name => GetColumnValue(Activity, Uri, MediaStore.IMediaColumns.DisplayName)
                          ?? Uri.PathSegments?.LastOrDefault() ?? string.Empty;

    public Uri Path => new(Uri.ToString()!);

    public bool CanBookmark => true;

    public async Task<string?> SaveBookmarkAsync()
    {
        if (!await EnsureExternalFilesPermission(false))
        {
            return null;
        }

        Activity.ContentResolver?.TakePersistableUriPermission(Uri, ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantReadUriPermission);
        return Uri.ToString();
    }

    public async Task ReleaseBookmarkAsync()
    {
        if (!await EnsureExternalFilesPermission(false))
        {
            return;
        }

        Activity.ContentResolver?.ReleasePersistableUriPermission(Uri, ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantReadUriPermission);
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

    public async Task<IStorageFolder?> GetParentAsync()
    {
        if (!await EnsureExternalFilesPermission(false))
        {
            return null;
        }

        using var javaFile = new JavaFile(Uri.Path!);

        // Java file represents files AND directories. Don't be confused.
        if (javaFile.ParentFile is {} parentFile
            && AndroidUri.FromFile(parentFile) is {} androidUri)
        {
            return new AndroidStorageFolder(Activity, androidUri, false);
        }

        return null;
    }

    protected async Task<bool> EnsureExternalFilesPermission(bool write)
    {
        if (!_needsExternalFilesPermission)
        {
            return true;
        }

        return await _activity.CheckPermission(Manifest.Permission.ReadExternalStorage);
    }
    
    public void Dispose()
    {
        _activity = null;
    }
}

internal class AndroidStorageFolder : AndroidStorageItem, IStorageBookmarkFolder
{
    public AndroidStorageFolder(Activity activity, AndroidUri uri, bool needsExternalFilesPermission) : base(activity, uri, needsExternalFilesPermission)
    {
    }

    public override Task<StorageItemProperties> GetBasicPropertiesAsync()
    {
        return Task.FromResult(new StorageItemProperties());
    }

    public async Task<IReadOnlyList<IStorageItem>> GetItemsAsync()
    {
        if (!await EnsureExternalFilesPermission(false))
        {
            return Array.Empty<IStorageItem>();
        }

        List<IStorageItem> files = new List<IStorageItem>();

        var contentResolver = Activity.ContentResolver;
        if (contentResolver == null)
        {
            return files;
        }

        var childrenUri = DocumentsContract.BuildChildDocumentsUriUsingTree(Uri!, DocumentsContract.GetTreeDocumentId(Uri));

        var projection = new[]
        {
            DocumentsContract.Document.ColumnDocumentId,
            DocumentsContract.Document.ColumnMimeType
        };
        if (childrenUri != null)
        {
            using var cursor = contentResolver.Query(childrenUri, projection, null, null, null);

            if (cursor != null)
                while (cursor.MoveToNext())
                {
                    var mime = cursor.GetString(1);
                    var id = cursor.GetString(0);
                    var uri = DocumentsContract.BuildDocumentUriUsingTree(Uri!, id);
                    if (uri == null)
                    {
                        continue;
                    }

                    files.Add(mime == DocumentsContract.Document.MimeTypeDir ? new AndroidStorageFolder(Activity, uri, false) :
                        new AndroidStorageFile(Activity, uri));
                }
        }

        return files;
    }       
}

internal sealed class WellKnownAndroidStorageFolder : AndroidStorageFolder
{
    public WellKnownAndroidStorageFolder(Activity activity, string identifier, AndroidUri uri, bool needsExternalFilesPermission)
        : base(activity, uri, needsExternalFilesPermission)
    {
        Name = identifier;
    }

    public override string Name { get; }
} 

internal sealed class AndroidStorageFile : AndroidStorageItem, IStorageBookmarkFile
{
    public AndroidStorageFile(Activity activity, AndroidUri uri) : base(activity, uri, false)
    {
    }
    
    public Task<Stream> OpenReadAsync() => Task.FromResult(OpenContentStream(Activity, Uri, false)
        ?? throw new InvalidOperationException("Failed to open content stream"));

    public Task<Stream> OpenWriteAsync() => Task.FromResult(OpenContentStream(Activity, Uri, true)
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

    private static Stream? GetVirtualFileStream(Context context, AndroidUri uri, bool isOutput)
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
            using var cursor = Activity.ContentResolver!.Query(Uri, projection, null, null, null);

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
