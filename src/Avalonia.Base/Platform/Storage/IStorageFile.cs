using System.IO;
using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Platform.Storage;

/// <summary>
/// Represents a file. Provides information about the file and its contents, and ways to manipulate them.
/// </summary>
[NotClientImplementable]
public interface IStorageFile : IStorageItem
{
    /// <summary>
    /// Returns true, if file is readable.
    /// </summary>
    bool CanOpenRead { get; }

    /// <summary>
    /// Opens a stream for read access.
    /// </summary>
    Task<Stream> OpenReadAsync();

    /// <summary>
    /// Returns true, if file is writeable. 
    /// </summary>
    bool CanOpenWrite { get; }

    /// <summary>
    /// Opens stream for writing to the file.
    /// </summary>
    Task<Stream> OpenWriteAsync();
}
