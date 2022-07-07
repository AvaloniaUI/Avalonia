using System;

namespace Avalonia.Platform.Storage;

/// <summary>
/// Provides access to the content-related properties of an item (like a file or folder).
/// </summary>
public class StorageItemProperties
{
    public StorageItemProperties(
        ulong? size = null,
        DateTimeOffset? dateCreated = null,
        DateTimeOffset? dateModified = null)
    {
        Size = size;
        DateCreated = dateCreated;
        DateModified = dateModified;
    }

    /// <summary>
    /// Gets the size of the file in bytes.
    /// </summary>
    /// <remarks>
    /// Can be null if property is not available.
    /// </remarks>
    public ulong? Size { get; }

    /// <summary>
    /// Gets the date and time that the current folder was created.
    /// </summary>
    /// <remarks>
    /// Can be null if property is not available.
    /// </remarks>
    public DateTimeOffset? DateCreated { get; }

    /// <summary>
    /// Gets the date and time of the last time the file was modified.
    /// </summary>
    /// <remarks>
    /// Can be null if property is not available.
    /// </remarks>
    public DateTimeOffset? DateModified { get; }
}
