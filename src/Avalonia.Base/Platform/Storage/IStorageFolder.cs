using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Platform.Storage;

/// <summary>
/// Manipulates folders and their contents, and provides information about them.
/// </summary>
[NotClientImplementable]
public interface IStorageFolder : IStorageItem
{
    /// <summary>
    /// Gets the files and subfolders in the current folder.
    /// </summary>
    /// <returns>
    /// When this method completes successfully, it returns a list of the files and folders in the current folder. Each item in the list is represented by an <see cref="IStorageItem"/> implementation object.
    /// </returns>
    IAsyncEnumerable<IStorageItem> GetItemsAsync();

    /// <summary>
    /// Creates a file with specified name as a child of the current storage folder
    /// </summary>
    /// <param name="name">The display name</param>
    /// <returns>A new <see cref="IStorageFile"/> pointing to the moved file. If not null, the current storage item becomes invalid</returns>
    Task<IStorageFile?> CreateFileAsync(string name);

    /// <summary>
    /// Creates a folder with specified name as a child of the current storage folder
    /// </summary>
    /// <param name="name">The display name</param>
    /// <returns>A new <see cref="IStorageFolder"/> pointing to the moved file. If not null, the current storage item becomes invalid</returns>
    Task<IStorageFolder?> CreateFolderAsync(string name);
}
