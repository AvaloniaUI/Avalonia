using System;
using System.Threading.Tasks;
using Android.Content;
using Avalonia.Android.Platform.Storage;
using Avalonia.Platform.Storage;
using AndroidUri = Android.Net.Uri;

namespace Avalonia.Android.Platform;

internal class AndroidLauncher : ILauncher
{
    private readonly Context _context;

    public AndroidLauncher(Context context)
    {
        _context = context;
    }
    
    public Task<bool> LaunchUriAsync(Uri uri)
    {
        _ = uri ?? throw new ArgumentNullException(nameof(uri));
        if (uri.IsAbsoluteUri && _context.PackageManager is { } packageManager)
        {
            var intent = new Intent(Intent.ActionView, AndroidUri.Parse(uri.OriginalString));
            if (intent.ResolveActivity(packageManager) is not null)
            {
                var flags = ActivityFlags.ClearTop | ActivityFlags.NewTask;
                intent.SetFlags(flags);
                _context.StartActivity(intent);
            }
        }
        return Task.FromResult(false);
    }

    public Task<bool> LaunchFileAsync(IStorageItem storageItem)
    {
        _ = storageItem ?? throw new ArgumentNullException(nameof(storageItem));
        var androidUri = (storageItem as AndroidStorageItem)?.Uri
            ?? (storageItem.TryGetLocalPath() is { } localPath ? AndroidUri.Parse(localPath) : null);

        if (androidUri is not null && _context.PackageManager is { } packageManager)
        {
            var intent = new Intent(Intent.ActionView, androidUri);
            // intent.SetDataAndType(contentUri, request.File.ContentType);
            intent.SetFlags(ActivityFlags.GrantReadUriPermission);
            if (intent.ResolveActivity(packageManager) is not null
                && Intent.CreateChooser(intent, string.Empty) is { } chooserIntent)
            {
                var flags = ActivityFlags.ClearTop | ActivityFlags.NewTask;
                chooserIntent.SetFlags(flags);
                _context.StartActivity(chooserIntent);
            }
        }
        return Task.FromResult(false);
    }
}
