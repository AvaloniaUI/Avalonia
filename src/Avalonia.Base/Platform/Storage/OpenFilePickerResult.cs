using System.Collections.Generic;

namespace Avalonia.Platform.Storage;

/// <summary>
/// Represents the result of the <see cref="IStorageProvider.OpenFilePickerWithResultAsync"/> operation.
/// </summary>
public readonly record struct OpenFilePickerResult
{
    /// <summary>
    /// Gets the list of files selected by the user, or empty if the user canceled the dialog.
    /// </summary>
    public IReadOnlyList<IStorageFile> Files
    {
        get => field ?? [];
        init;
    }

    /// <summary>
    /// Gets the file type selected by the user, or null if the platform does not support this feature.
    /// </summary>
    public FilePickerFileType? SelectedFileType { get; init; }
}
