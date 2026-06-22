namespace Avalonia.Platform.Storage;

/// <summary>
/// Extended result of the <see cref="IStorageProvider.SaveFilePickerWithResultAsync(FilePickerSaveOptions)"/> operation.
/// </summary>
public readonly record struct SaveFilePickerResult
{
    /// <summary>
    /// Saved <see cref="IStorageFile"/> or null if user canceled the dialog.
    /// </summary>
    public IStorageFile? File { get; init; }

    /// <summary>
    /// Selected file type or null if not supported.
    /// </summary>
    public FilePickerFileType? SelectedFileType { get; init; }
}
