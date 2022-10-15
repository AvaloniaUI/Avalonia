#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Provider;
using Avalonia.Platform.Storage;
using AndroidUri = Android.Net.Uri;

namespace Avalonia.Android.Platform.Storage;

internal class AndroidStorageProvider : IStorageProvider
{
    private readonly AvaloniaMainActivity _activity;
    private int _lastRequestCode = 20000;

    public AndroidStorageProvider(AvaloniaMainActivity activity)
    {
        _activity = activity;
    }

    public bool CanOpen => OperatingSystem.IsAndroidVersionAtLeast(19);

    public bool CanSave => OperatingSystem.IsAndroidVersionAtLeast(19);

    public bool CanPickFolder => OperatingSystem.IsAndroidVersionAtLeast(21);

    public Task<IStorageBookmarkFolder?> OpenFolderBookmarkAsync(string bookmark)
    {
        var uri = AndroidUri.Parse(bookmark) ?? throw new ArgumentException("Couldn't parse Bookmark value", nameof(bookmark));
        return Task.FromResult<IStorageBookmarkFolder?>(new AndroidStorageFolder(_activity, uri));
    }

    public Task<IStorageBookmarkFile?> OpenFileBookmarkAsync(string bookmark)
    {
        var uri = AndroidUri.Parse(bookmark) ?? throw new ArgumentException("Couldn't parse Bookmark value", nameof(bookmark));
        return Task.FromResult<IStorageBookmarkFile?>(new AndroidStorageFile(_activity, uri));
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

        if (TryGetInitialUri(options.SuggestedStartLocation) is { } initialUri)
        {
            intent = intent.PutExtra(DocumentsContract.ExtraInitialUri, initialUri);
        }

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

        if (TryGetInitialUri(options.SuggestedStartLocation) is { } initialUri)
        {
            intent = intent.PutExtra(DocumentsContract.ExtraInitialUri, initialUri);
        }

        var pickerIntent = Intent.CreateChooser(intent, options.Title ?? "Save file");

        var uris = await StartActivity(pickerIntent, true);
        return uris.Select(u => new AndroidStorageFile(_activity, u)).FirstOrDefault();
    }

    public async Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
    {
        var intent = new Intent(Intent.ActionOpenDocumentTree)
            .PutExtra(Intent.ExtraAllowMultiple, options.AllowMultiple);
        if (TryGetInitialUri(options.SuggestedStartLocation) is { } initialUri)
        {
            intent = intent.PutExtra(DocumentsContract.ExtraInitialUri, initialUri);
        }

        var pickerIntent = Intent.CreateChooser(intent, options.Title ?? "Select folder");

        var uris = await StartActivity(pickerIntent, false);
        return uris.Select(u => new AndroidStorageFolder(_activity, u)).ToArray();
    }

    private async Task<List<AndroidUri>> StartActivity(Intent? pickerIntent, bool singleResult)
    {
        var resultList = new List<AndroidUri>(1);
        var tcs = new TaskCompletionSource<Intent?>();
        var currentRequestCode = _lastRequestCode++;

        _activity.ActivityResult += OnActivityResult;
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

        void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if (currentRequestCode != requestCode)
            {
                return;
            }

            _activity.ActivityResult -= OnActivityResult;

            _ = tcs.TrySetResult(resultCode == Result.Ok ? data : null);
        }
    }

    private static AndroidUri? TryGetInitialUri(IStorageFolder? folder)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(26)
            && (folder as AndroidStorageItem)?.Uri is { } uri)
        {
            return uri;
        }

        return null;
    }
}
