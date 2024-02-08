using System.Collections.Generic;

namespace Avalonia.Platform.Storage;

/// <summary>
/// Options class for <see cref="IStorageProvider.SaveFilePickerAsync"/> method.
/// </summary>
public class FilePickerSaveOptions : PickerOptions
{
    /// <summary>
    /// Gets or sets the default extension to be used to save the file.
    /// </summary>
    public string? DefaultExtension { get; set; }

    /// <summary>
    /// Gets or sets the collection of valid file types that the user can choose to assign to a file.
    /// </summary>
    public IReadOnlyList<FilePickerFileType>? FileTypeChoices { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether file open picker displays a warning if the user specifies the name of a file that already exists.
    /// </summary>
    public bool? ShowOverwritePrompt { get; set; }
}
