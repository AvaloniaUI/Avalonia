using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Logging;
using Avalonia.Native.Interop;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;
using Avalonia.Reactive;
using MicroCom.Runtime;

namespace Avalonia.Native;

internal class StorageProviderApi(IAvnStorageProvider native, bool sandboxEnabled) : IStorageProviderFactory, IDisposable
{
    private readonly Dictionary<string, int> _openScopes = new();
    private readonly IAvnStorageProvider _native = native;

    public IStorageProvider CreateProvider(TopLevel topLevel)
    {
        return new StorageProviderImpl((TopLevelImpl)topLevel.PlatformImpl!, this);
    }

    public IStorageItem? TryGetStorageItem(Uri? itemUri, bool create = false)
    {
        if (itemUri is not null && StorageProviderHelpers.TryGetPathFromFileUri(itemUri) is { } itemPath)
        {
            if (new FileInfo(itemPath) is { } fileInfo
                && (create || fileInfo.Exists))
            {
                return sandboxEnabled
                    ? new StorageFile(this, fileInfo, itemUri, itemUri)
                    : new BclStorageFile(fileInfo);
            }
            if (new DirectoryInfo(itemPath) is { } directoryInfo
                && (create || directoryInfo.Exists))
            {
                return sandboxEnabled
                    ? new StorageFolder(this, directoryInfo, itemUri, itemUri)
                    : new BclStorageFolder(directoryInfo);
            }
        }

        return null;
    }

    public IDisposable? OpenSecurityScope(string uriString)
    {
        // Multiple entries are possible.
        // For example, user might open OpenRead stream, and read file properties before closing the file.
        // If we don't check for nested scopes, inner closing scope will break access of the outer scope.
        if (AddUse(this, uriString) == 1)
        {
            using var nsUriString = new AvnString(uriString);
            var scopeOpened = _native.OpenSecurityScope(nsUriString).FromComBool();
            if (!scopeOpened)
            {
                RemoveUse(this, uriString);
                Logger.TryGet(LogEventLevel.Information, LogArea.macOSPlatform)?
                     .Log(this, "OpenSecurityScope returned false for the {Uri}", uriString);
                return null;
            }
        }

        return Disposable.Create((api: this, uriString), static state =>
        {
            if (RemoveUse(state.api, state.uriString) == 0)
            {
                using var nsUriString = new AvnString(state.uriString);
                state.api._native.CloseSecurityScope(nsUriString);
            }
        });

        static int AddUse(StorageProviderApi api, string uriString)
        {
            lock (api)
            {
                api._openScopes.TryGetValue(uriString, out var useValue);
                api._openScopes[uriString] = ++useValue;
                return useValue;
            }
        }
        static int RemoveUse(StorageProviderApi api, string uriString)
        {
            lock (api)
            {
                api._openScopes.TryGetValue(uriString, out var useValue);
                useValue--;
                if (useValue == 0)
                    api._openScopes.Remove(uriString);
                else
                    api._openScopes[uriString] = useValue;
                return useValue;
            }
        }
    }

    // Avalonia.Native technically can be used for more than just macOS,
    // In which case we should provide different bookmark platform keys, and parse accordingly.
    private static ReadOnlySpan<byte> MacOSKey => "macOS"u8;
    public unsafe string? SaveBookmark(Uri uri)
    {
        void* error = null;
        using var uriString = new AvnString(uri.AbsoluteUri);
        using var bookmarkStr = _native.SaveBookmarkToBytes(uriString, &error);

        if (error != null)
        {
            using var errorStr = MicroComRuntime.CreateProxyOrNullFor<IAvnString>(error, true);
            Logger.TryGet(LogEventLevel.Warning, LogArea.macOSPlatform)?
                .Log(this, "SaveBookmark for {Uri} failed with an error\r\n{Error}", uri, errorStr.String);
            return null;
        }

        return StorageBookmarkHelper.EncodeBookmark(MacOSKey, bookmarkStr?.Bytes);
    }

    // Support both kinds of bookmarks when reading.
    // Since "save bookmark" implementation will be different depending on the configuration.
    public unsafe Uri? ReadBookmark(string bookmark, bool isDirectory)
    {
        if (StorageBookmarkHelper.TryDecodeBookmark(MacOSKey, bookmark, out var bytes) == StorageBookmarkHelper.DecodeResult.Success)
        {
            fixed (byte* ptr = bytes)
            {
                using var uriString = _native.ReadBookmarkFromBytes(ptr, bytes!.Length);
                return uriString is not null && Uri.TryCreate(uriString.String, UriKind.Absolute, out var uri) ?
                    uri :
                    null;
            }
        }
        if (StorageBookmarkHelper.TryDecodeBclBookmark(bookmark, out var path))
        {
            return StorageProviderHelpers.UriFromFilePath(path, isDirectory);
        }

        return null;
    }

    public void ReleaseBookmark(Uri uri)
    {
        using var uriString = new AvnString(uri.AbsoluteUri);
        _native.ReleaseBookmark(uriString);
    }

    public void Dispose()
    {
        _native.Dispose();
    }

    public async Task<IReadOnlyList<IStorageFile>> OpenFileDialog(TopLevelImpl? topLevel, FilePickerOpenOptions options)
    {
        using var fileTypes = new FilePickerFileTypesWrapper(options.FileTypeFilter, null, options.SuggestedFileType);
        var suggestedDirectory = options.SuggestedStartLocation?.Path.AbsoluteUri ?? string.Empty;

        var (items, _) = await OpenDialogAsync(events =>
        {
            _native.OpenFileDialog((IAvnWindow?)topLevel?.Native,
                events,
                options.AllowMultiple.AsComBool(),
                options.Title ?? string.Empty,
                suggestedDirectory,
                options.SuggestedFileName ?? string.Empty,
                fileTypes);
        }).ConfigureAwait(false);

        return items.OfType<IStorageFile>().ToArray();
    }

    public async Task<(IStorageFile? file, FilePickerFileType? selectedType)> SaveFileDialog(TopLevelImpl? topLevel, FilePickerSaveOptions options)
    {
        using var fileTypes = new FilePickerFileTypesWrapper(options.FileTypeChoices, options.DefaultExtension, options.SuggestedFileType);
        var suggestedDirectory = options.SuggestedStartLocation?.Path.AbsoluteUri ?? string.Empty;

        var (items, selectedFilterIndex) = await OpenDialogAsync(events =>
        {
            _native.SaveFileDialog((IAvnWindow?)topLevel?.Native,
                events,
                options.Title ?? string.Empty,
                suggestedDirectory,
                options.SuggestedFileName ?? string.Empty,
                fileTypes);
        }, create: true).ConfigureAwait(false);

        var file = items.OfType<IStorageFile>().FirstOrDefault();
        FilePickerFileType? selectedType = null;
        if (selectedFilterIndex is { } index && index >= 0 && options.FileTypeChoices is { Count: > 0 } choices && index < choices.Count)
        {
            selectedType = choices[index];
        }

        return (file, selectedType);
    }

    public async Task<IReadOnlyList<IStorageFolder>> SelectFolderDialog(TopLevelImpl? topLevel, FolderPickerOpenOptions options)
    {
        var suggestedDirectory = options.SuggestedStartLocation?.Path.AbsoluteUri ?? string.Empty;

        var (items, _) = await OpenDialogAsync(events =>
        {
            _native.SelectFolderDialog((IAvnWindow?)topLevel?.Native,
                events,
                options.AllowMultiple.AsComBool(),
                options.Title ?? "",
                suggestedDirectory);
        }).ConfigureAwait(false);

        return items.OfType<IStorageFolder>().ToArray();
    }

    public async Task<(IEnumerable<IStorageItem> Items, int? SelectedFilterIndex)> OpenDialogAsync(Action<SystemDialogEvents> runDialog, bool create = false)
    {
        using var events = new SystemDialogEvents();
        runDialog(events);
        var (result, selectedFilterIndex) = await events.Task.ConfigureAwait(false);

        var items = result
            .Select(f => Uri.TryCreate(f, UriKind.Absolute, out var uri) ? TryGetStorageItem(uri, create) : null)
            .OfType<IStorageItem>()
            .ToArray();

        return (items, selectedFilterIndex);
    }

    public Uri? TryResolveFileReferenceUri(Uri uri)
    {
        using var uriString = new AvnString(uri.AbsoluteUri);
        using var resultString = _native.TryResolveFileReferenceUri(uriString);

        return Uri.TryCreate(resultString?.String, UriKind.Absolute, out var resultUri) ? resultUri : null;
    }

    internal class FilePickerFileTypesWrapper(
        IReadOnlyList<FilePickerFileType>? types,
        string? defaultExtension,
        FilePickerFileType? suggestedType)
        : NativeCallbackBase, IAvnFilePickerFileTypes
    {
        private readonly List<IDisposable> _disposables = new();

        public int Count => types?.Count ?? 0;

        public int IsDefaultType(int index)
        {
            if (types is null)
                return false.AsComBool();

            if (suggestedType is not null && ReferenceEquals(types[index], suggestedType))
                return true.AsComBool();

            return (defaultExtension is not null &&
                    types[index].TryGetExtensions()?.Any(defaultExtension.EndsWith) == true).AsComBool();
        }

        public int IsAnyType(int index) =>
            (types![index].Patterns?.Contains("*.*") == true || types[index].MimeTypes?.Contains("*.*") == true)
            .AsComBool();

        public IAvnString GetName(int index)
        {
            return EnsureDisposable(types![index].Name.ToAvnString());
        }

        public IAvnStringArray GetPatterns(int index)
        {
            return EnsureDisposable(new AvnStringArray(types![index].Patterns ?? Array.Empty<string>()));
        }

        public IAvnStringArray GetExtensions(int index)
        {
            return EnsureDisposable(new AvnStringArray(types![index].TryGetExtensions() ?? Array.Empty<string>()));
        }

        public IAvnStringArray GetMimeTypes(int index)
        {
            return EnsureDisposable(new AvnStringArray(types![index].MimeTypes ?? Array.Empty<string>()));
        }

        public IAvnStringArray GetAppleUniformTypeIdentifiers(int index)
        {
            return EnsureDisposable(new AvnStringArray(types![index].AppleUniformTypeIdentifiers ?? Array.Empty<string>()));
        }

        protected override void Destroyed()
        {
            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }
        }

        private T EnsureDisposable<T>(T input) where T : IDisposable
        {
            _disposables.Add(input);
            return input;
        }
    }

    internal class SystemDialogEvents : NativeCallbackBase, IAvnSystemDialogEvents
    {
        private readonly TaskCompletionSource<(string[] Results, int? SelectedFilterIndex)> _tcs = new();

        public Task<(string[] Results, int? SelectedFilterIndex)> Task => _tcs.Task;

        public void OnCompleted(IAvnStringArray? ppv)
        {
            Complete(ppv, null);
        }

        public void OnCompletedWithFilter(IAvnStringArray? ppv, int selectedFilterIndex)
        {
            Complete(ppv, selectedFilterIndex);
        }

        private void Complete(IAvnStringArray? ppv, int? selectedFilterIndex)
        {
            using (ppv)
            {
                var items = ppv?.ToStringArray() ?? Array.Empty<string>();
                var typeIndex = selectedFilterIndex is >= 0 ? selectedFilterIndex : null;
                _tcs.TrySetResult((items, typeIndex));
            }
        }
    }
}
