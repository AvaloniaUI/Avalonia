using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Platform.Storage.FileIO;

[Unstable]
public class BclStorageFolder : IStorageBookmarkFolder
{
    private readonly DirectoryInfo _directoryInfo;

    public BclStorageFolder(DirectoryInfo directoryInfo)
    {
        _directoryInfo = directoryInfo ?? throw new ArgumentNullException(nameof(directoryInfo));
        if (!_directoryInfo.Exists)
        {
            throw new ArgumentException("Directory must exist", nameof(directoryInfo));
        }
    }

    public string Name => _directoryInfo.Name;

    public bool CanBookmark => true;

    public Task<StorageItemProperties> GetBasicPropertiesAsync()
    {
        var props = new StorageItemProperties(
            null,
            _directoryInfo.CreationTimeUtc,
            _directoryInfo.LastAccessTimeUtc);
        return Task.FromResult(props);
    }

    public Task<IStorageFolder?> GetParentAsync()
    {
        if (_directoryInfo.Parent is { } directory)
        {
            return Task.FromResult<IStorageFolder?>(new BclStorageFolder(directory));
        }
        return Task.FromResult<IStorageFolder?>(null);
    }

    public Task<IReadOnlyList<IStorageItem>> GetItemsAsync()
    {
         var items = _directoryInfo.GetDirectories()
            .Select(d => (IStorageItem)new BclStorageFolder(d))
            .Concat(_directoryInfo.GetFiles().Select(f => new BclStorageFile(f)))
            .ToArray();

         return Task.FromResult<IReadOnlyList<IStorageItem>>(items);
    }

    public virtual Task<string?> SaveBookmark()
    {
        return Task.FromResult<string?>(_directoryInfo.FullName);
    }
    
    public Task ReleaseBookmark()
    {
        // No-op
        return Task.CompletedTask;
    }

    public bool TryGetUri([NotNullWhen(true)] out Uri? uri)
    {
        try
        {
            uri = Path.IsPathRooted(_directoryInfo.FullName) ?
                new Uri(new Uri("file://"), _directoryInfo.FullName) :
                new Uri(_directoryInfo.FullName, UriKind.Relative);

            return true;
        }
        catch (SecurityException)
        {
            uri = null;
            return false;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
    }

    ~BclStorageFolder()
    {
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
