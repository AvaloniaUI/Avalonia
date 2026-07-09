namespace Avalonia.Platform.Storage;

/// <summary>
/// Represents the result of the <see cref="IStorageProvider.SaveFilePickerWithResultAsync"/> operation.
/// </summary>
public readonly record struct SaveFilePickerResult
{
    /// <summary>
    /// Gets the file selected by the user, or null if the user canceled the dialog.
    /// </summary>
    public IStorageFile? File { get; init; }

    /// <summary>
    /// Gets the file type selected by the user, or null if the platform does not support this feature.
    /// </summary>
    public FilePickerFileType? SelectedFileType { get; init; }
}
