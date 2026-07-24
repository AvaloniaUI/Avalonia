using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Platform.Storage;

[NotClientImplementable]
public interface IStorageProvider
{
    /// <summary>
    /// Returns true if it's possible to open the file picker on the current platform.
    /// </summary>
    bool CanOpen { get; }

    /// <summary>
    /// Opens the file picker dialog.
    /// </summary>
    /// <returns>Array of selected <see cref="IStorageFile"/> or empty collection if user canceled the dialog.</returns>
    Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options);

    /// <summary>
    /// Opens the file picker dialog and returns additional information about the result.
    /// </summary>
    /// <returns><see cref="OpenFilePickerResult"/> with selected files and additional dialog information such as selected file type.</returns>
    Task<OpenFilePickerResult> OpenFilePickerWithResultAsync(FilePickerOpenOptions options);

    /// <summary>
    /// Returns true if it's possible to open the save file picker on the current platform.
    /// </summary>
    bool CanSave { get; }

    /// <summary>
    /// Opens the save file picker dialog.
    /// </summary>
    /// <returns>Saved <see cref="IStorageFile"/> or null if user canceled the dialog.</returns>
    Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options);

    /// <summary>
    /// Opens the save file picker dialog and returns additional information about the result.
    /// </summary>
    /// <returns><see cref="SaveFilePickerResult"/> with saved file and additional dialog information such as selected file type.</returns>
    Task<SaveFilePickerResult> SaveFilePickerWithResultAsync(FilePickerSaveOptions options);

    /// <summary>
    /// Returns true if it's possible to open the folder picker on the current platform.
    /// </summary>
    bool CanPickFolder { get; }

    /// <summary>
    /// Opens the folder picker dialog.
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

    /// <summary>
    /// Attempts to read file from the file-system by its path.
    /// </summary>
    /// <param name="filePath">The path of the item to retrieve in Uri format.</param>
    /// <remarks>
    /// Uri path is usually expected to be an absolute path with "file" scheme.
    /// But it can be an uri with "content" scheme on the Android.
    /// It also might ask user for the permission, and throw an exception if it was denied.
    /// </remarks>
    /// <returns>File or null if it doesn't exist.</returns>
    Task<IStorageFile?> TryGetFileFromPathAsync(Uri filePath);
    
    /// <summary>
    /// Attempts to read folder from the file-system by its path.
    /// </summary>
    /// <param name="folderPath">The path of the folder to retrieve in Uri format.</param>
    /// <remarks>
    /// Uri path is usually expected to be an absolute path with "file" scheme.
    /// But it can be an uri with "content" scheme on the Android. 
    /// It also might ask user for the permission, and throw an exception if it was denied.
    /// </remarks>
    /// <returns>Folder or null if it doesn't exist.</returns>
    Task<IStorageFolder?> TryGetFolderFromPathAsync(Uri folderPath);
    
    /// <summary>
    /// Attempts to read folder from the file-system by its path
    /// </summary>
    /// <param name="wellKnownFolder">Well known folder identifier.</param>
    /// <returns>Folder or null if it doesn't exist.</returns>
    Task<IStorageFolder?> TryGetWellKnownFolderAsync(WellKnownFolder wellKnownFolder);
}
