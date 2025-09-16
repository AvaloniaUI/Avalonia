namespace Avalonia.Platform.Storage;

/// <summary>
/// Extended result of the <see cref="IStorageProvider.SaveFilePickerWithResultAsync(FilePickerSaveOptions)"/> operation.
/// </summary>
public readonly struct SaveFilePickerResult
{
    internal SaveFilePickerResult(IStorageFile? file)
    {
        File = file;
    }

    /// <summary>
    /// Saved <see cref="IStorageFile"/> or null if user canceled the dialog.
    /// </summary>
    public IStorageFile? File { get; }

    /// <summary>
    /// Selected file type or null if not supported.
    /// </summary>
    public FilePickerFileType? SelectedFileType { get; init; }
}
