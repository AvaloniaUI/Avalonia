using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Logging;
using Avalonia.Platform.Storage;
using UIKit;
using Foundation;
using UniformTypeIdentifiers;
using UTTypeLegacy = MobileCoreServices.UTType;
using UTType = UniformTypeIdentifiers.UTType;
using System.Runtime.Versioning;
using Avalonia.Platform.Storage.FileIO;

namespace Avalonia.iOS.Storage;

internal class IOSStorageProvider : IStorageProvider
{
    public static ReadOnlySpan<byte> PlatformKey => "ios"u8; 

    private readonly AvaloniaView _view;
    public IOSStorageProvider(AvaloniaView view)
    {
        _view = view;
    }

    public bool CanOpen => true;

    public bool CanSave => true;

    public bool CanPickFolder => true;

    public Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options)
    {
        if (!options.AllowMultiple
            && options.SuggestedStartLocation is IOSStorageFolder { WellKnownFolder: WellKnownFolder.Pictures })
        {
            return OpenImagePickerAsync(options);
        }
        else
        {
            return OpenDocumentsPickerAsync(options);
        }
    }

    private async Task<IReadOnlyList<IStorageFile>> OpenImagePickerAsync(FilePickerOpenOptions options)
    {
#pragma warning disable CA1422 // Validate platform compatibility - we can't use PHImagePicker here.
        using var imagePicker = new UIImagePickerController();
        imagePicker.SourceType = UIImagePickerControllerSourceType.PhotoLibrary;
        imagePicker.MediaTypes = new string[] { UTTypeLegacy.Image };
        imagePicker.AllowsEditing = false;
        imagePicker.Title = options.Title;
#pragma warning restore CA1422 // Validate platform compatibility

        var tcs = new TaskCompletionSource<NSUrl[]>();
        imagePicker.Delegate = new ImageOpenPickerDelegate(urls => tcs.TrySetResult(urls));
        var urls = await ShowPicker(imagePicker, tcs);

        return urls.Select(u => new IOSStorageFile(u)).ToArray();
    }

    private async Task<IReadOnlyList<IStorageFile>> OpenDocumentsPickerAsync(FilePickerOpenOptions options)
    {
        UIDocumentPickerViewController documentPicker;
        if (OperatingSystem.IsIOSVersionAtLeast(14))
        {
            var allowedTypes = FileTypesToUTType(options.FileTypeFilter);
            documentPicker = new UIDocumentPickerViewController(allowedTypes!, false);
        }
        else
        {
            var allowedTypes = FileTypesToUTTypeLegacy(options.FileTypeFilter);
            documentPicker = new UIDocumentPickerViewController(allowedTypes, UIDocumentPickerMode.Open);
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

            documentPicker.Title = options.Title;

            var urls = await ShowDocumentPicker(documentPicker);
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

    public Task<IStorageFile?> TryGetFileFromPathAsync(Uri filePath)
    {
        var fileUrl = (NSUrl?)filePath;
        var isDirectory = false;
        var file = fileUrl is not null
                   && fileUrl.Path is { } path
                   && NSFileManager.DefaultManager.FileExists(path, ref isDirectory)
                   && !isDirectory
                   && NSFileManager.DefaultManager.IsReadableFile(path) ?
            new IOSStorageFile(fileUrl) :
            null;

        return Task.FromResult<IStorageFile?>(file);
    }

    public Task<IStorageFolder?> TryGetFolderFromPathAsync(Uri folderPath)
    {
        var folderUrl = (NSUrl?)folderPath;
        var isDirectory = false;
        var folder = folderUrl is not null
                   && folderUrl.Path is { } path
                   && NSFileManager.DefaultManager.FileExists(path, ref isDirectory)
                   && isDirectory ?
            new IOSStorageFolder(folderUrl) :
            null;

        return Task.FromResult<IStorageFolder?>(folder);
    }

    public Task<IStorageFolder?> TryGetWellKnownFolderAsync(WellKnownFolder wellKnownFolder)
    {
        var directoryType = wellKnownFolder switch
        {
            WellKnownFolder.Desktop => NSSearchPathDirectory.DesktopDirectory,
            WellKnownFolder.Documents => NSSearchPathDirectory.DocumentDirectory,
            WellKnownFolder.Downloads => NSSearchPathDirectory.DownloadsDirectory,
            WellKnownFolder.Music => NSSearchPathDirectory.MusicDirectory,
            WellKnownFolder.Pictures => NSSearchPathDirectory.PicturesDirectory,
            WellKnownFolder.Videos => NSSearchPathDirectory.MoviesDirectory,
            _ => throw new ArgumentOutOfRangeException(nameof(wellKnownFolder), wellKnownFolder, null)
        };

        var uri = NSFileManager.DefaultManager.GetUrl(directoryType, NSSearchPathDomain.User, null, true, out var error);
        if (error != null)
        {
            throw new NSErrorException(error);
        }

        if (uri is null)
        {
            return Task.FromResult<IStorageFolder?>(null);
        }

        return Task.FromResult<IStorageFolder?>(new IOSStorageFolder(uri, wellKnownFolder));
    }

    public async Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options)
    {
        /* 
            This requires a bit of dialog here...
            To save a file, we need to present the user with a document picker
            This requires a temp file to be created and used to "export" the file to.
            When the user picks the file location and name, UIDocumentPickerViewController
            will give back the URI to the real file location, which we can then use 
            to give back as an IStorageFile.
            https://developer.apple.com/documentation/uikit/uidocumentpickerviewcontroller
            Yes, it is weird, but without the temp file it will explode.
        */

        // Create a temporary file to use with the document picker
        var tempFileName = StorageProviderHelpers.NameWithExtension(
            options.SuggestedFileName ?? "document", 
            options.DefaultExtension, 
            options.FileTypeChoices?.FirstOrDefault());
        
        var tempDir = NSFileManager.DefaultManager.GetTemporaryDirectory().Append(Guid.NewGuid().ToString(), true);
        if (tempDir == null)
        {
            throw new InvalidOperationException("Failed to get temporary directory for save file picker");
        }

        var isDirectoryCreated = NSFileManager.DefaultManager.CreateDirectory(tempDir, true, null, out var error);
        if (!isDirectoryCreated)
        {
            throw new InvalidOperationException("Failed to create temporary directory for save file picker");
        }
        
        var tempFileUrl = tempDir.Append(tempFileName, false);
        
        // Create an empty file at the temp location
        NSData.FromBytes(0, 0).Save(tempFileUrl, false);
        
        UIDocumentPickerViewController documentPicker;
        if (OperatingSystem.IsIOSVersionAtLeast(14))
        {
            documentPicker = new UIDocumentPickerViewController(new[] { tempFileUrl }, asCopy: true);
        }
        else
        {
#pragma warning disable CA1422
            documentPicker = new UIDocumentPickerViewController(tempFileUrl, UIDocumentPickerMode.ExportToService);
#pragma warning restore CA1422
        }
        
        using (documentPicker)
        {
            if (OperatingSystem.IsIOSVersionAtLeast(13))
            {
                documentPicker.DirectoryUrl = GetUrlFromFolder(options.SuggestedStartLocation);
            }
            
            documentPicker.Title = options.Title;
            
            var tcs = new TaskCompletionSource<NSUrl[]>();
            documentPicker.Delegate = new PickerDelegate(urls => tcs.TrySetResult(urls));
            var urls = await ShowPicker(documentPicker, tcs);

            // Clean up the temporary directory
            NSFileManager.DefaultManager.Remove(tempDir, out _);
            
            return urls.FirstOrDefault() is { } url ? new IOSStorageFile(url) : null;
        }
    }

    public async Task<SaveFilePickerResult> SaveFilePickerWithResultAsync(FilePickerSaveOptions options)
    {
        var file = await SaveFilePickerAsync(options).ConfigureAwait(false);
        return new SaveFilePickerResult { File = file };
    }

    public async Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
    {
        using var documentPicker = OperatingSystem.IsIOSVersionAtLeast(14) ?
            new UIDocumentPickerViewController(new[] { UTTypes.Folder }, false) :
#pragma warning disable CA1422
            new UIDocumentPickerViewController(new string[] { UTTypeLegacy.Folder }, UIDocumentPickerMode.Open);
#pragma warning restore CA1422

        if (OperatingSystem.IsIOSVersionAtLeast(13))
        {
            documentPicker.DirectoryUrl = GetUrlFromFolder(options.SuggestedStartLocation);
        }

        if (OperatingSystem.IsIOSVersionAtLeast(11))
        {
            documentPicker.AllowsMultipleSelection = options.AllowMultiple;
        }
        
        var urls = await ShowDocumentPicker(documentPicker);
        return urls.Select(u => new IOSStorageFolder(u)).ToArray();
    }

    private static NSUrl? GetUrlFromFolder(IStorageFolder? folder)
    {
        return folder switch
        {
            IOSStorageFolder iosFolder => iosFolder.Url,
            null => null,
            _ => folder.Path
        };
    }

    private Task<NSUrl[]> ShowDocumentPicker(UIDocumentPickerViewController documentPicker)
    {
        var tcs = new TaskCompletionSource<NSUrl[]>();
        documentPicker.Delegate = new PickerDelegate(urls => tcs.TrySetResult(urls));
        return ShowPicker(documentPicker, tcs);
    }

    private Task<NSUrl[]> ShowPicker(UIViewController picker, TaskCompletionSource<NSUrl[]> tcs)
    {
        if (picker.PresentationController != null)
        {
            picker.PresentationController.Delegate =
                new UIPresentationControllerDelegate(() => tcs.TrySetResult(Array.Empty<NSUrl>()));
        }

        var controller = _view.Window?.RootViewController ?? throw new InvalidOperationException("RootViewController wasn't initialized");
        controller.PresentViewController(picker, true, null);

        return tcs.Task;
    }

    private unsafe NSUrl? GetBookmarkedUrl(string bookmark)
    {
        return StorageBookmarkHelper.TryDecodeBookmark(PlatformKey, bookmark, out var bytes) switch
        {
            StorageBookmarkHelper.DecodeResult.Success => DecodeFromBytes(bytes!),
            // Attempt to decode 11.0 ios bookmarks
            StorageBookmarkHelper.DecodeResult.InvalidFormat => DecodeFromNSData(new NSData(bookmark, NSDataBase64DecodingOptions.None)),
            _ => null
        };

        NSUrl DecodeFromBytes(byte[] bytes)
        {
            fixed (byte* ptr = bytes)
            {
                using var data = new NSData(new IntPtr(ptr), new UIntPtr((uint)bytes.Length), null);
                return DecodeFromNSData(data);
            }
        }

        NSUrl DecodeFromNSData(NSData nsData)
        {
            var url = NSUrl.FromBookmarkData(nsData,
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
    }

    private static string[] FileTypesToUTTypeLegacy(IReadOnlyList<FilePickerFileType>? filePickerFileTypes)
    {
        return filePickerFileTypes?
            .SelectMany(f => f.AppleUniformTypeIdentifiers ?? Array.Empty<string>())
            .ToArray() ?? new[]
            {
#pragma warning disable CA1422
                UTTypeLegacy.Content,
                UTTypeLegacy.Item,
#pragma warning restore CA1422
                "public.data"
            };
    }

    [SupportedOSPlatform("ios14.0")]
    private static UTType[] FileTypesToUTType(IReadOnlyList<FilePickerFileType>? filePickerFileTypes)
    {
        return filePickerFileTypes?.SelectMany(f =>
        {
            if (f.TryGetExtensions() is { } extensions && extensions.Any())
            {
                return extensions.Select(UTType.CreateFromExtension);
            }
            if (f.AppleUniformTypeIdentifiers?.Any() == true)
            {
                return f.AppleUniformTypeIdentifiers.Select(UTType.CreateFromIdentifier);
            }
            if (f.MimeTypes?.Any() == true)
            {
                return f.MimeTypes.Select(UTType.CreateFromMimeType);
            }
            return Array.Empty<UTType>();
        })
        .Where(id => id is not null)
        .Cast<UTType>()
        .ToArray() ?? new[]
        {
            UTTypes.Content,
            UTTypes.Item,
            UTTypes.Data
        };
    }

    private class ImageOpenPickerDelegate : UIImagePickerControllerDelegate
    {
        private readonly Action<NSUrl[]>? _pickHandler;

        internal ImageOpenPickerDelegate(Action<NSUrl[]> pickHandler)
            => _pickHandler = pickHandler;

        public override void Canceled(UIImagePickerController picker)
            => _pickHandler?.Invoke(Array.Empty<NSUrl>());

        public override void FinishedPickingMedia(UIImagePickerController picker, NSDictionary info)
        {
            if (info.ValueForKey(new NSString("UIImagePickerControllerImageURL")) is NSUrl nSUrl)
            {
                _pickHandler?.Invoke(new[] { nSUrl });
            }
            else
            {
                _pickHandler?.Invoke(Array.Empty<NSUrl>());
            }
        }
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
