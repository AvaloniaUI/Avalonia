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
    /// Opens a stream for read access.
    /// </summary>
    /// <exception cref="System.UnauthorizedAccessException" />
    Task<Stream> OpenReadAsync();
    
    /// <summary>
    /// Opens stream for writing to the file.
    /// </summary>
    /// <exception cref="System.UnauthorizedAccessException" />
    Task<Stream> OpenWriteAsync();
}
