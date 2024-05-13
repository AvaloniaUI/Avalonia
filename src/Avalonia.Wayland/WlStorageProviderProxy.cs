using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.FreeDesktop;
using Avalonia.Platform.Storage;

namespace Avalonia.Wayland
{
    internal class WlStorageProviderProxy : IStorageProvider
    {
        private readonly string _handle;

        private IStorageProvider? _storageProvider;

        public WlStorageProviderProxy(string handle)
        {
            _handle = handle;
        }

        public bool CanOpen => true;

        public bool CanSave => true;

        public bool CanPickFolder => true;

        public async Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options)
        {
            var provider = await EnsureStorageProvider();
            return await provider.OpenFilePickerAsync(options);
        }

        public async Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options)
        {
            var provider = await EnsureStorageProvider();
            return await provider.SaveFilePickerAsync(options);
        }

        public async Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
        {
            var provider = await EnsureStorageProvider();
            return await provider.OpenFolderPickerAsync(options);
        }

        public async Task<IStorageBookmarkFile?> OpenFileBookmarkAsync(string bookmark)
        {
            var provider = await EnsureStorageProvider();
            return await provider.OpenFileBookmarkAsync(bookmark);
        }

        public async Task<IStorageBookmarkFolder?> OpenFolderBookmarkAsync(string bookmark)
        {
            var provider = await EnsureStorageProvider();
            return await provider.OpenFolderBookmarkAsync(bookmark);
        }

        public async Task<IStorageFile?> TryGetFileFromPathAsync(Uri filePath)
        {
            var provider = await EnsureStorageProvider();
            return await provider.TryGetFileFromPathAsync(filePath);
        }

        public async Task<IStorageFolder?> TryGetFolderFromPathAsync(Uri folderPath)
        {
            var provider = await EnsureStorageProvider();
            return await provider.TryGetFolderFromPathAsync(folderPath);
        }

        public async Task<IStorageFolder?> TryGetWellKnownFolderAsync(WellKnownFolder wellKnownFolder)
        {
            var provider = await EnsureStorageProvider();
            return await provider.TryGetWellKnownFolderAsync(wellKnownFolder);
        }

        private async ValueTask<IStorageProvider> EnsureStorageProvider() =>
            _storageProvider ??= await DBusSystemDialog.TryCreateAsync(_handle)
                                 ?? throw new InvalidOperationException("DBus xdg-desktop-portal file picker is not available on the system");
    }
}
