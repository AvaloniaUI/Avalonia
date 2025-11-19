using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Avalonia.Platform.Storage;
#pragma warning disable CA1823

internal class FallbackStorageProvider : IStorageProvider
{
    private readonly Func<Task<IStorageProvider?>>[] _factories;
    private readonly List<IStorageProvider> _providers = new();
    private int _nextProviderFactory = 0;
    
    public FallbackStorageProvider(Func<Task<IStorageProvider?>>[] factories)
    {
        _factories = factories;
    }
    
    async IAsyncEnumerable<IStorageProvider> GetProviders()
    {
        foreach (var p in _providers)
            yield return p;
        for (;_nextProviderFactory < _factories.Length;)
        {
            var p = await _factories[_nextProviderFactory]();
            _nextProviderFactory++;
            if (p != null)
            {
                _providers.Add(p);
                yield return p;
            }
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
