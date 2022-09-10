using System.Diagnostics.CodeAnalysis;

using Avalonia.Platform.Storage;

using Microsoft.JSInterop;

namespace Avalonia.Web.Blazor.Interop.Storage
{
    internal record FilePickerAcceptType(string Description, IReadOnlyDictionary<string, IReadOnlyList<string>> Accept);

    internal record FileProperties(ulong Size, long LastModified, string? Type);

    internal class StorageProviderInterop : JSModuleInterop, IStorageProvider
    {
        private const string JsFilename = "./_content/Avalonia.Web.Blazor/Storage.js";
        private const string PickerCancelMessage = "The user aborted a request";

        public static async Task<StorageProviderInterop> ImportAsync(IJSRuntime js)
        {
            var interop = new StorageProviderInterop(js);
            await interop.ImportAsync();
            return interop;
        }

        public StorageProviderInterop(IJSRuntime js)
            : base(js, JsFilename)
        {
        }

        public bool CanOpen => Invoke<bool>("StorageProvider.canOpen");
        public bool CanSave => Invoke<bool>("StorageProvider.canSave");
        public bool CanPickFolder => Invoke<bool>("StorageProvider.canPickFolder");

        public async Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options)
        {
            try
            {
                var startIn = (options.SuggestedStartLocation as JSStorageItem)?.FileHandle;

                var (types, exludeAll) = ConvertFileTypes(options.FileTypeFilter);
                var items = await InvokeAsync<IJSInProcessObjectReference>("StorageProvider.openFileDialog", startIn, options.AllowMultiple, types, exludeAll);
                var count = items.Invoke<int>("count");

                return Enumerable.Range(0, count)
                    .Select(index => new JSStorageFile(items.Invoke<IJSInProcessObjectReference>("at", index)))
                    .ToArray();
            }
            catch (JSException ex) when (ex.Message.Contains(PickerCancelMessage, StringComparison.Ordinal))
            {
                return Array.Empty<IStorageFile>();
            }
        }

        public async Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options)
        {
            try
            {
                var startIn = (options.SuggestedStartLocation as JSStorageItem)?.FileHandle;

                var (types, exludeAll) = ConvertFileTypes(options.FileTypeChoices);
                var item = await InvokeAsync<IJSInProcessObjectReference>("StorageProvider.saveFileDialog", startIn, options.SuggestedFileName, types, exludeAll);

                return item is not null ? new JSStorageFile(item) : null;
            }
            catch (JSException ex) when (ex.Message.Contains(PickerCancelMessage, StringComparison.Ordinal))
            {
                return null;
            }
        }

        public async Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
        {
            try
            {
                var startIn = (options.SuggestedStartLocation as JSStorageItem)?.FileHandle;

                var item = await InvokeAsync<IJSInProcessObjectReference>("StorageProvider.selectFolderDialog", startIn);

                return item is not null ? new[] { new JSStorageFolder(item) } : Array.Empty<IStorageFolder>();
            }
            catch (JSException ex) when (ex.Message.Contains(PickerCancelMessage, StringComparison.Ordinal))
            {
                return Array.Empty<IStorageFolder>();
            }
        }

        public async Task<IStorageBookmarkFile?> OpenFileBookmarkAsync(string bookmark)
        {
            var item = await InvokeAsync<IJSInProcessObjectReference>("StorageProvider.openBookmark", bookmark);
            return item is not null ? new JSStorageFile(item) : null;
        }

        public async Task<IStorageBookmarkFolder?> OpenFolderBookmarkAsync(string bookmark)
        {
            var item = await InvokeAsync<IJSInProcessObjectReference>("StorageProvider.openBookmark", bookmark);
            return item is not null ? new JSStorageFolder(item) : null;
        }

        private static (FilePickerAcceptType[]? types, bool excludeAllOption) ConvertFileTypes(IEnumerable<FilePickerFileType>? input)
        {
            var types = input?
                .Where(t => t.MimeTypes?.Any() == true && t != FilePickerFileTypes.All)
                .Select(t => new FilePickerAcceptType(t.Name, t.MimeTypes!
                    .ToDictionary(m => m, _ => (IReadOnlyList<string>)Array.Empty<string>())))
                .ToArray();
            if (types?.Length == 0)
            {
                types = null;
            }

            var inlcudeAll = input?.Contains(FilePickerFileTypes.All) == true || types is null;

            return (types, !inlcudeAll);
        }
    }

    internal abstract class JSStorageItem : IStorageBookmarkItem
    {
        internal IJSInProcessObjectReference? _fileHandle;

        protected JSStorageItem(IJSInProcessObjectReference fileHandle)
        {
            _fileHandle = fileHandle ?? throw new ArgumentNullException(nameof(fileHandle));
        }

        internal IJSInProcessObjectReference FileHandle => _fileHandle ?? throw new ObjectDisposedException(nameof(JSStorageItem));

        public string Name => FileHandle.Invoke<string>("getName");

        public bool TryGetUri([NotNullWhen(true)] out Uri? uri)
        {
            uri = new Uri(Name, UriKind.Relative);
            return false;
        }

        public async Task<StorageItemProperties> GetBasicPropertiesAsync()
        {
            var properties = await FileHandle.InvokeAsync<FileProperties?>("getProperties");

            return new StorageItemProperties(
                properties?.Size,
                dateCreated: null,
                dateModified: properties?.LastModified > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(properties.LastModified) : null);
        }

        public bool CanBookmark => true;

        public Task<string?> SaveBookmarkAsync()
        {
            return FileHandle.InvokeAsync<string?>("saveBookmark").AsTask();
        }

        public Task<IStorageFolder?> GetParentAsync()
        {
            return Task.FromResult<IStorageFolder?>(null);
        }

        public Task ReleaseBookmarkAsync()
        {
            return FileHandle.InvokeAsync<string?>("deleteBookmark").AsTask();
        }

        public void Dispose()
        {
            _fileHandle?.Dispose();
            _fileHandle = null;
        }
    }

    internal class JSStorageFile : JSStorageItem, IStorageBookmarkFile
    {
        public JSStorageFile(IJSInProcessObjectReference fileHandle) : base(fileHandle)
        {
        }

        public bool CanOpenRead => true;
        public async Task<Stream> OpenReadAsync()
        {
            var stream = await FileHandle.InvokeAsync<IJSStreamReference>("openRead");
            // Remove maxAllowedSize limit, as developer can decide if they read only small part or everything.
            return await stream.OpenReadStreamAsync(long.MaxValue, CancellationToken.None);
        }

        public bool CanOpenWrite => true;
        public async Task<Stream> OpenWriteAsync()
        {
            var properties = await FileHandle.InvokeAsync<FileProperties?>("getProperties");
            var streamWriter = await FileHandle.InvokeAsync<IJSInProcessObjectReference>("openWrite");

            return new JSWriteableStream(streamWriter, (long)(properties?.Size ?? 0));
        }
    }

    internal class JSStorageFolder : JSStorageItem, IStorageBookmarkFolder
    {
        public JSStorageFolder(IJSInProcessObjectReference fileHandle) : base(fileHandle)
        {
        }

        public async Task<IReadOnlyList<IStorageItem>> GetItemsAsync()
        {
            var items = await FileHandle.InvokeAsync<IJSInProcessObjectReference?>("getItems");
            if (items is null)
            {
                return Array.Empty<IStorageItem>();
            }

            var count = items.Invoke<int>("count");

            return Enumerable.Range(0, count)
                .Select(index =>
                {
                    var reference = items.Invoke<IJSInProcessObjectReference>("at", index);
                    return reference.Invoke<string>("getKind") switch
                    {
                        "directory" => (IStorageItem)new JSStorageFolder(reference),
                        "file" => new JSStorageFile(reference),
                        _ => null
                    };
                })
                .Where(i => i is not null)
                .ToArray()!;
        }
    }
}
