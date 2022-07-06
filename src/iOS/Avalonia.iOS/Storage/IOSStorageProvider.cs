using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Logging;
using Avalonia.Platform.Storage;
using UIKit;
using Foundation;
using UniformTypeIdentifiers;
using UTTypeLegacy = MobileCoreServices.UTType;
using UTType = UniformTypeIdentifiers.UTType;

#nullable enable

namespace Avalonia.iOS.Storage;

internal class IOSStorageProvider : IStorageProvider
{
    private readonly AvaloniaView _view;
    public IOSStorageProvider(AvaloniaView view)
    {
        _view = view;
    }

    public bool CanOpen => true;

    public bool CanSave => false;

    public bool CanPickFolder => true;

    public async Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options)
    {
        UIDocumentPickerViewController documentPicker;
        if (OperatingSystem.IsIOSVersionAtLeast(14))
        {
            var allowedUtis = options.FileTypeFilter?.SelectMany(f =>
                {
                    // We check for OS version outside of the lambda, it's safe.
#pragma warning disable CA1416
                    if (f.AppleUniformTypeIdentifiers?.Any() == true)
                    {
                        return f.AppleUniformTypeIdentifiers.Select(id => UTType.CreateFromIdentifier(id));
                    }
                    if (f.MimeTypes?.Any() == true)
                    {
                        return f.MimeTypes.Select(id => UTType.CreateFromMimeType(id));
                    }
                    return Array.Empty<UTType>();
#pragma warning restore CA1416
                })
                .Where(id => id is not null)
                .ToArray() ?? new[]
            {
                UTTypes.Content,
                UTTypes.Item,
                UTTypes.Data
            };
            documentPicker = new UIDocumentPickerViewController(allowedUtis!, false);
        }
        else
        {
            var allowedUtis = options.FileTypeFilter?.SelectMany(f => f.AppleUniformTypeIdentifiers ?? Array.Empty<string>())
                .ToArray() ?? new[]
            {
                UTTypeLegacy.Content,
                UTTypeLegacy.Item,
                "public.data"
            };
            documentPicker = new UIDocumentPickerViewController(allowedUtis, UIDocumentPickerMode.Open);
        }

        using (documentPicker)
        {
            if (OperatingSystem.IsIOSVersionAtLeast(13))
            {
                documentPicker.DirectoryUrl = GetUrlFromFolder(options.SuggestedStartLocation);
            }

            if (OperatingSystem.IsIOSVersionAtLeast(11, 0))
            {
                documentPicker.AllowsMultipleSelection = options.AllowMultiple;
            }

            var urls = await ShowPicker(documentPicker);
            return urls.Select(u => new IOSStorageFile(u)).ToArray();
        }
    }

    public Task<IStorageBookmarkFile?> OpenFileBookmarkAsync(string bookmark)
    {
        return Task.FromResult<IStorageBookmarkFile?>(GetBookmarkedUrl(bookmark) is { } url
            ? new IOSStorageFile(url) : null);
    }

    public Task<IStorageBookmarkFolder?> OpenFolderBookmarkAsync(string bookmark)
    {
        return Task.FromResult<IStorageBookmarkFolder?>(GetBookmarkedUrl(bookmark) is { } url
            ? new IOSStorageFolder(url) : null);
    }

    public Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options)
    {
        return Task.FromException<IStorageFile?>(
            new PlatformNotSupportedException("Save file picker is not supported by iOS"));
    }

    public async Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
    {
        using var documentPicker = OperatingSystem.IsIOSVersionAtLeast(14) ?
            new UIDocumentPickerViewController(new[] { UTTypes.Folder }, false) :
            new UIDocumentPickerViewController(new string[] { UTTypeLegacy.Folder }, UIDocumentPickerMode.Open);

        if (OperatingSystem.IsIOSVersionAtLeast(13))
        {
            documentPicker.DirectoryUrl = GetUrlFromFolder(options.SuggestedStartLocation);
        }

        if (OperatingSystem.IsIOSVersionAtLeast(11))
        {
            documentPicker.AllowsMultipleSelection = options.AllowMultiple;
        }
        
        var urls = await ShowPicker(documentPicker);
        return urls.Select(u => new IOSStorageFolder(u)).ToArray();
    }

    private static NSUrl? GetUrlFromFolder(IStorageFolder? folder)
    {
        if (folder is IOSStorageFolder iosFolder)
        {
            return iosFolder.Url;
        }

        if (folder?.TryGetUri(out var fullPath) == true)
        {
            return fullPath;
        }

        return null;
    }

    private Task<NSUrl[]> ShowPicker(UIDocumentPickerViewController documentPicker)
    {
        var tcs = new TaskCompletionSource<NSUrl[]>();
        documentPicker.Delegate = new PickerDelegate(urls => tcs.TrySetResult(urls));

        if (documentPicker.PresentationController != null)
        {
            documentPicker.PresentationController.Delegate =
                new UIPresentationControllerDelegate(() => tcs.TrySetResult(Array.Empty<NSUrl>()));
        }

        var controller = _view.Window?.RootViewController ?? throw new InvalidOperationException("RootViewController wasn't initialized");
        controller.PresentViewController(documentPicker, true, null);
        
        return tcs.Task;
    }

    private NSUrl? GetBookmarkedUrl(string bookmark)
    {
        var url = NSUrl.FromBookmarkData(new NSData(bookmark, NSDataBase64DecodingOptions.None),
            NSUrlBookmarkResolutionOptions.WithoutUI, null, out var isStale, out var error);
        if (isStale)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.IOSPlatform)?.Log(this, "Stale bookmark detected");
        }
            
        if (error != null)
        {
            throw new NSErrorException(error);
        }
        return url;
    }
        
    private class PickerDelegate : UIDocumentPickerDelegate
    {
        private readonly Action<NSUrl[]>? _pickHandler;

        internal PickerDelegate(Action<NSUrl[]> pickHandler)
            => _pickHandler = pickHandler;

        public override void WasCancelled(UIDocumentPickerViewController controller)
            => _pickHandler?.Invoke(Array.Empty<NSUrl>());

        public override void DidPickDocument(UIDocumentPickerViewController controller, NSUrl[] urls)
            => _pickHandler?.Invoke(urls);

        public override void DidPickDocument(UIDocumentPickerViewController controller, NSUrl url)
            => _pickHandler?.Invoke(new[] { url });
    }

    private class UIPresentationControllerDelegate : UIAdaptivePresentationControllerDelegate
    {
        private Action? _dismissHandler;

        internal UIPresentationControllerDelegate(Action dismissHandler)
            => this._dismissHandler = dismissHandler;

        public override void DidDismiss(UIPresentationController presentationController)
        {
            _dismissHandler?.Invoke();
            _dismissHandler = null;
        }

        protected override void Dispose(bool disposing)
        {
            _dismissHandler?.Invoke();
            base.Dispose(disposing);
        }
    }
}
