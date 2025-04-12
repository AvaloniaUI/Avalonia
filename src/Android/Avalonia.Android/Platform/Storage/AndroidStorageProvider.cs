﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Provider;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;
using AndroidUri = Android.Net.Uri;
using Exception = System.Exception;
using JavaFile = Java.IO.File;

namespace Avalonia.Android.Platform.Storage;

internal class AndroidStorageProvider : IStorageProvider
{
    public static ReadOnlySpan<byte> AndroidKey => "android"u8;
    private readonly Activity _activity;

    public AndroidStorageProvider(Activity activity)
    {
        _activity = activity;
    }

    public bool CanOpen => OperatingSystem.IsAndroidVersionAtLeast(19);

    public bool CanSave => OperatingSystem.IsAndroidVersionAtLeast(19);

    public bool CanPickFolder => OperatingSystem.IsAndroidVersionAtLeast(21);

    public Task<IStorageBookmarkFolder?> OpenFolderBookmarkAsync(string bookmark)
    {
        var uri = DecodeUriFromBookmark(bookmark);
        return Task.FromResult<IStorageBookmarkFolder?>(uri is null ? null : new AndroidStorageFolder(_activity, uri, false));
    }

    public async Task<IStorageFile?> TryGetFileFromPathAsync(Uri filePath)
    {
        if (filePath is null)
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        if (filePath is not { IsAbsoluteUri: true, Scheme: "file" or "content" })
        {
            throw new ArgumentException("File path is expected to be an absolute link with \"file\" or \"content\" scheme.");
        }

        var androidUri = AndroidUri.Parse(filePath.ToString());
        if (androidUri?.Path is not {} androidUriPath)
        {
            return null;
        }

        await EnsureUriReadPermission(androidUri);

        var javaFile = new JavaFile(androidUriPath);
        if (javaFile.Exists() && javaFile.IsFile)
        {
            return null;
        }

        return new AndroidStorageFile(_activity, androidUri);
    }

    public async Task<IStorageFolder?> TryGetFolderFromPathAsync(Uri folderPath)
    {
        if (folderPath is null)
        {
            throw new ArgumentNullException(nameof(folderPath));
        }

        if (folderPath is not { IsAbsoluteUri: true, Scheme: "file" or "content" })
        {
            throw new ArgumentException("Folder path is expected to be an absolute link with \"file\" or \"content\" scheme.");
        }

        var androidUri = AndroidUri.Parse(folderPath.ToString());
        if (androidUri?.Path is not {} androidUriPath)
        {
            return null;
        }

        await EnsureUriReadPermission(androidUri);

        var javaFile = new JavaFile(androidUriPath);
        if (javaFile.Exists() && javaFile.IsDirectory)
        {
            return null;
        }

        return new AndroidStorageFolder(_activity, androidUri, false);
    }

    public Task<IStorageFolder?> TryGetWellKnownFolderAsync(WellKnownFolder wellKnownFolder)
    {
        var dirCode = wellKnownFolder switch
        {
            WellKnownFolder.Desktop => null,
            WellKnownFolder.Documents => global::Android.OS.Environment.DirectoryDocuments,
            WellKnownFolder.Downloads => global::Android.OS.Environment.DirectoryDownloads,
            WellKnownFolder.Music => global::Android.OS.Environment.DirectoryMusic,
            WellKnownFolder.Pictures => global::Android.OS.Environment.DirectoryPictures,
            WellKnownFolder.Videos => global::Android.OS.Environment.DirectoryMovies,
            _ => throw new ArgumentOutOfRangeException(nameof(wellKnownFolder), wellKnownFolder, null)
        };
        if (dirCode is null)
        {
            return Task.FromResult<IStorageFolder?>(null);
        }

        var dir = _activity.GetExternalFilesDir(dirCode);
        if (dir is null || !dir.Exists())
        {
            return Task.FromResult<IStorageFolder?>(null);
        }

        var uri = AndroidUri.FromFile(dir);
        if (uri is null)
        {
            return Task.FromResult<IStorageFolder?>(null);
        }

        // To make TryGetWellKnownFolder API easier to use, we don't check for the permissions.
        // It will work with file picker activities, but it will fail on any direct access to the folder, like getting list of children.
        // We pass "needsExternalFilesPermission" parameter here, so folder itself can check for permissions on any FS access. 
        return Task.FromResult<IStorageFolder?>(new WellKnownAndroidStorageFolder(_activity, dirCode, uri, true));
    }

    public Task<IStorageBookmarkFile?> OpenFileBookmarkAsync(string bookmark)
    {
        var uri = DecodeUriFromBookmark(bookmark);
        return Task.FromResult<IStorageBookmarkFile?>(uri is null ? null : new AndroidStorageFile(_activity, uri));
    }

    private static AndroidUri? DecodeUriFromBookmark(string bookmark)
    {
        return StorageBookmarkHelper.TryDecodeBookmark(AndroidKey, bookmark, out var bytes) switch
        {
            StorageBookmarkHelper.DecodeResult.Success => AndroidUri.Parse(Encoding.UTF8.GetString(bytes!)),
            // Attempt to decode 11.0 android bookmarks
            StorageBookmarkHelper.DecodeResult.InvalidFormat => AndroidUri.Parse(bookmark),
            _ => null
        };
    }

    public async Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options)
    {
        var mimeTypes = options.FileTypeFilter?.Where(t => t != FilePickerFileTypes.All)
            .SelectMany(f => f.MimeTypes ?? Array.Empty<string>()).Distinct().ToArray() ?? Array.Empty<string>();

        var intent = new Intent(Intent.ActionOpenDocument)
            .AddCategory(Intent.CategoryOpenable)
            .PutExtra(Intent.ExtraAllowMultiple, options.AllowMultiple)
            .SetType(FilePickerFileTypes.All.MimeTypes![0]);
        if (mimeTypes.Length > 0)
        {
            intent = intent.PutExtra(Intent.ExtraMimeTypes, mimeTypes);
        }

        intent = TryAddExtraInitialUri(intent, options.SuggestedStartLocation);

        var pickerIntent = Intent.CreateChooser(intent, options.Title ?? "Select file");

        var uris = await StartActivity(pickerIntent, false);
        return uris.Select(u => new AndroidStorageFile(_activity, u)).ToArray();
    }

    public async Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options)
    {
        var mimeTypes = options.FileTypeChoices?.Where(t => t != FilePickerFileTypes.All)
            .SelectMany(f => f.MimeTypes ?? Array.Empty<string>()).Distinct().ToArray() ?? Array.Empty<string>();

        var intent = new Intent(Intent.ActionCreateDocument)
            .AddCategory(Intent.CategoryOpenable)
            .SetType(FilePickerFileTypes.All.MimeTypes![0]);
        if (mimeTypes.Length > 0)
        {
            intent = intent.PutExtra(Intent.ExtraMimeTypes, mimeTypes);
        }

        if (options.SuggestedFileName is { } fileName)
        {
            if (options.DefaultExtension is { } ext)
            {
                fileName += ext.StartsWith('.') ? ext : "." + ext;
            }
            intent = intent.PutExtra(Intent.ExtraTitle, fileName);
        }

        intent = TryAddExtraInitialUri(intent, options.SuggestedStartLocation);

        var pickerIntent = Intent.CreateChooser(intent, options.Title ?? "Save file");

        var uris = await StartActivity(pickerIntent, true);
        return uris.Select(u => new AndroidStorageFile(_activity, u)).FirstOrDefault();
    }

    public async Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
    {
        var intent = new Intent(Intent.ActionOpenDocumentTree)
            .PutExtra(Intent.ExtraAllowMultiple, options.AllowMultiple);

        intent = TryAddExtraInitialUri(intent, options.SuggestedStartLocation);

        var pickerIntent = Intent.CreateChooser(intent, options.Title ?? "Select folder");

        var uris = await StartActivity(pickerIntent, false);
        return uris.Select(u => new AndroidStorageFolder(_activity, u, false)).ToArray();
    }

    private async Task<List<AndroidUri>> StartActivity(Intent? pickerIntent, bool singleResult)
    {
        var resultList = new List<AndroidUri>(1);
        var tcs = new TaskCompletionSource<Intent?>();
        var currentRequestCode = PlatformSupport.GetNextRequestCode();

        if (!(_activity is IActivityResultHandler mainActivity))
        {
            throw new InvalidOperationException("Main activity must implement IActivityResultHandler interface.");
        }

        mainActivity.ActivityResult += OnActivityResult;
        _activity.StartActivityForResult(pickerIntent, currentRequestCode);

        var result = await tcs.Task;

        if (result != null)
        {
            // ClipData first to avoid issue with multiple files selection.
            if (!singleResult && result.ClipData is { } clipData)
            {
                for (var i = 0; i < clipData.ItemCount; i++)
                {
                    var uri = clipData.GetItemAt(i)?.Uri;
                    if (uri != null)
                    {
                        resultList.Add(uri);
                    }
                }
            }
            else if (result.Data is { } uri)
            {
                resultList.Add(uri);
            }
        }

        if (result?.HasExtra("error") == true)
        {
            throw new Exception(result.GetStringExtra("error"));
        }

        return resultList;

        void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            if (currentRequestCode != requestCode)
            {
                return;
            }

            mainActivity.ActivityResult -= OnActivityResult;

            _ = tcs.TrySetResult(resultCode == Result.Ok ? data : null);
        }
    }

    private static Intent TryAddExtraInitialUri(Intent intent, IStorageFolder? folder)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(26)
            && (folder as AndroidStorageItem)?.Uri is { } uri)
        {
            return intent.PutExtra(DocumentsContract.ExtraInitialUri, uri);
        }

        return intent;
    }

    private async Task EnsureUriReadPermission(AndroidUri androidUri)
    {
        bool hasPerms = false;
        Exception? innerEx = null;
        try
        {
            hasPerms = _activity.CheckUriPermission(androidUri,
                global::Android.OS.Process.MyPid(),
                global::Android.OS.Process.MyUid(),
                ActivityFlags.GrantReadUriPermission)
                == global::Android.Content.PM.Permission.Granted;

            // https://developer.android.com/reference/android/Manifest.permission#READ_EXTERNAL_STORAGE
            if (!OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                // TODO: call RequestPermission or add proper permissions API, something like in Browser File API.
                hasPerms = hasPerms || await _activity.CheckPermission(Manifest.Permission.ReadExternalStorage);
            }
        }
        catch (Exception ex)
        {
            innerEx = ex;
        }

        if (!hasPerms)
        {
            throw new InvalidOperationException("Application doesn't have READ_EXTERNAL_STORAGE permission. Make sure android manifest has this permission defined and user allowed it.", innerEx);
        }
    }
}
