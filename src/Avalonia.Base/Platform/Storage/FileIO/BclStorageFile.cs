using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security;
using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Platform.Storage.FileIO;

[Unstable]
public class BclStorageFile : IStorageBookmarkFile
{
    private readonly FileInfo _fileInfo;

    public BclStorageFile(FileInfo fileInfo)
    {
        _fileInfo = fileInfo ?? throw new ArgumentNullException(nameof(fileInfo));
    }

    public bool CanOpenRead => true;

    public bool CanOpenWrite => true;

    public string Name => _fileInfo.Name;

    public virtual bool CanBookmark => true;

    public Task<StorageItemProperties> GetBasicPropertiesAsync()
    {
        var props = new StorageItemProperties();
        if (_fileInfo.Exists)
        {
            props = new StorageItemProperties(
                (ulong)_fileInfo.Length,
                _fileInfo.CreationTimeUtc,
                _fileInfo.LastAccessTimeUtc);
        }
        return Task.FromResult(props);
    }

    public Task<IStorageFolder?> GetParentAsync()
    {
        if (_fileInfo.Directory is { } directory)
        {
            return Task.FromResult<IStorageFolder?>(new BclStorageFolder(directory));
        }
        return Task.FromResult<IStorageFolder?>(null);
    }

    public Task<Stream> OpenReadAsync()
    {
        return Task.FromResult<Stream>(_fileInfo.OpenRead());
    }

    public Task<Stream> OpenWriteAsync()
    {
        return Task.FromResult<Stream>(_fileInfo.OpenWrite());
    }

    public virtual Task<string?> SaveBookmarkAsync()
    {
        return Task.FromResult<string?>(_fileInfo.FullName);
    }

    public Task ReleaseBookmarkAsync()
    {
        // No-op
        return Task.CompletedTask;
    }

    public bool TryGetUri([NotNullWhen(true)] out Uri? uri)
    {
        try
        {
            if (_fileInfo.Directory is not null)
            {
                uri = Path.IsPathRooted(_fileInfo.FullName) ?
                    new Uri(new Uri("file://"), _fileInfo.FullName) :
                    new Uri(_fileInfo.FullName, UriKind.Relative);
                return true;
            }

            uri = null;
            return false;
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

    ~BclStorageFile()
    {
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
