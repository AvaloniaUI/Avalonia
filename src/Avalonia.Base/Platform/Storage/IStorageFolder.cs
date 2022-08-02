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
    Task<IReadOnlyList<IStorageItem>> GetItemsAsync();
}
