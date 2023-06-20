using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Platform.Storage;

/// <summary>
/// Manipulates storage items (files and folders) and their contents, and provides information about them
/// </summary>
/// <remarks>
/// This interface inherits <see cref="IDisposable"/> . It's recommended to dispose <see cref="IStorageItem"/> when it's not used anymore.
/// </remarks>
[NotClientImplementable]
public interface IStorageItem : IDisposable
{
    /// <summary>
    /// Gets the name of the item including the file name extension if there is one.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the file-system path of the item.
    /// </summary>
    /// <remarks>
    /// Android backend might return file path with "content:" scheme.
    /// Browser and iOS backends might return relative uris.
    /// </remarks>
    Uri Path { get; }

    /// <summary>
    /// Gets the basic properties of the current item.
    /// </summary>
    Task<StorageItemProperties> GetBasicPropertiesAsync();

    /// <summary>
    /// Returns true is item can be bookmarked and reused later.
    /// </summary>
    bool CanBookmark { get; }

    /// <summary>
    /// Saves items to a bookmark.
    /// </summary>
    /// <returns>
    /// Returns identifier of a bookmark. Can be null if OS denied request.
    /// </returns>
    Task<string?> SaveBookmarkAsync();

    /// <summary>
    /// Gets the parent folder of the current storage item.
    /// </summary>
    Task<IStorageFolder?> GetParentAsync();

    /// <summary>
    /// Deletes the current storage item and it's contents
    /// </summary>
    /// <returns></returns>
    Task DeleteAsync();

    /// <summary>
    /// Moves the current storage item and it's contents to a <see cref="IStorageFolder"/>
    /// </summary>
    /// <param name="destination">The <see cref="IStorageFolder"/> to move the item into</param>
    /// <returns></returns>
    Task<IStorageItem?> MoveAsync(IStorageFolder destination);
}
