using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Avalonia.Platform.Storage;
#pragma warning disable CA1823

internal class FallbackStorageProvider : IStorageProvider
{
    private readonly object _lock = new();
    private Func<Task<IStorageProvider?>>[] _factories;
    private Task<IStorageProvider?>?[] _providers;
    
    public FallbackStorageProvider(Func<Task<IStorageProvider?>>[] factories)
    {
        _factories = factories;
        _providers = new Task<IStorageProvider?>?[factories.Length];
    }

    /// <summary>
    /// Discards any cached providers and replaces the factory list. Useful when the
    /// underlying platform state has changed (e.g. wayland compositor reconnect) and
    /// previously-selected providers may no longer be appropriate.
    /// </summary>
    public void Reset(Func<Task<IStorageProvider?>>[] factories)
    {
        lock (_lock)
        {
            _factories = factories;
            _providers = new Task<IStorageProvider?>?[factories.Length];
        }
    }
    
    async IAsyncEnumerable<IStorageProvider> GetProviders()
    {
        Func<Task<IStorageProvider?>>[] factories;
        Task<IStorageProvider?>?[] providers;

        // Capture both arrays so that a Reset during enumeration doesn't mix up two factory lists.
        lock (_lock)
        {
            factories = _factories;
            providers = _providers;
        }

        for (var i = 0; i < factories.Length; i++)
        {
            Task<IStorageProvider?> task;

            // Each factory is only invoked once: concurrent callers await the same task instead of
            // invoking the factory again and skipping over the providers that come after it.
            lock (_lock)
                task = providers[i] ??= factories[i]();

            IStorageProvider? p;

            try
            {
                p = await task;
            }
            catch
            {
                // Let the next call retry a factory that failed.
                lock (_lock)
                {
                    if (providers[i] == task)
                        providers[i] = null;
                }

                throw;
            }

            if (p != null)
                yield return p;
        }
    }
    
    async Task<IStorageProvider> GetFor(Func<IStorageProvider, bool> filter)
    {
        await foreach (var p in GetProviders())
            if (filter(p))
                return p;
        throw new IOException("Unable to select a suitable storage provider");
    }
    

    // Those should _really_ have been asynchronous,
    // but this class is expected to fall back to the managed implementation anyway
    public bool CanOpen => true;
    public bool CanSave => true;
    public bool CanPickFolder => true;
    
    public async Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options)
    {
        return await (await GetFor(p => p.CanOpen)).OpenFilePickerAsync(options);
    }

    public async Task<OpenFilePickerResult> OpenFilePickerWithResultAsync(FilePickerOpenOptions options)
    {
        return await (await GetFor(p => p.CanOpen)).OpenFilePickerWithResultAsync(options);
    }

    public async Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options)
    {
        return await (await GetFor(p => p.CanSave)).SaveFilePickerAsync(options);
    }

    public async Task<SaveFilePickerResult> SaveFilePickerWithResultAsync(FilePickerSaveOptions options)
    {
        return await (await GetFor(p => p.CanSave)).SaveFilePickerWithResultAsync(options);
    }

    public async Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
    {
        return await (await GetFor(p => p.CanPickFolder)).OpenFolderPickerAsync(options);
    }

    async Task<TResult?> FirstNotNull<TArg, TResult>(TArg arg, Func<IStorageProvider, TArg, Task<TResult?>> cb)
        where TResult : class
    {
        await foreach (var p in GetProviders())
        {
            var res = await cb(p, arg);
            if (res != null)
                return res;
        }

        return null;
    }

    public Task<IStorageBookmarkFile?> OpenFileBookmarkAsync(string bookmark) => 
        FirstNotNull(bookmark, (p, a) => p.OpenFileBookmarkAsync(a));

    public Task<IStorageBookmarkFolder?> OpenFolderBookmarkAsync(string bookmark) =>
        FirstNotNull(bookmark, (p, a) => p.OpenFolderBookmarkAsync(a));

    public Task<IStorageFile?> TryGetFileFromPathAsync(Uri filePath) =>
        FirstNotNull(filePath, (p, a) => p.TryGetFileFromPathAsync(filePath));

    public Task<IStorageFolder?> TryGetFolderFromPathAsync(Uri folderPath)
        => FirstNotNull(folderPath, (p, a) => p.TryGetFolderFromPathAsync(a));

    public Task<IStorageFolder?> TryGetWellKnownFolderAsync(WellKnownFolder wellKnownFolder) =>
        FirstNotNull(wellKnownFolder, (p, a) => p.TryGetWellKnownFolderAsync(a));
    
}
