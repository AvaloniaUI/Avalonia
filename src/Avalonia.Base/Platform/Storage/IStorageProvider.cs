using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Platform.Storage;

[NotClientImplementable]
public interface IStorageProvider
{
    /// <summary>
    /// Returns true if it's possible to open file picker on the current platform. 
    /// </summary>
    bool CanOpen { get; }

    /// <summary>
    /// Opens file picker dialog.
    /// </summary>
    /// <returns>Array of selected <see cref="IStorageFile"/> or empty collection if user canceled the dialog.</returns>
    Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options);

    /// <summary>
    /// Returns true if it's possible to open save file picker on the current platform. 
    /// </summary>
    bool CanSave { get; }

    /// <summary>
    /// Opens save file picker dialog.
    /// </summary>
    /// <returns>Saved <see cref="IStorageFile"/> or null if user canceled the dialog.</returns>
    Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options);

    /// <summary>
    /// Returns true if it's possible to open folder picker on the current platform. 
    /// </summary>
    bool CanPickFolder { get; }

    /// <summary>
    /// Opens folder picker dialog.
    /// </summary>
    /// <returns>Array of selected <see cref="IStorageFolder"/> or empty collection if user canceled the dialog.</returns>
    Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options);

    /// <summary>
    /// Open <see cref="IStorageBookmarkFile"/> from the bookmark ID.
    /// </summary>
    /// <param name="bookmark">Bookmark ID.</param>
    /// <returns>Bookmarked file or null if OS denied request.</returns>
    Task<IStorageBookmarkFile?> OpenFileBookmarkAsync(string bookmark);

    /// <summary>
    /// Open <see cref="IStorageBookmarkFolder"/> from the bookmark ID.
    /// </summary>
    /// <param name="bookmark">Bookmark ID.</param>
    /// <returns>Bookmarked folder or null if OS denied request.</returns>
    Task<IStorageBookmarkFolder?> OpenFolderBookmarkAsync(string bookmark);
}
