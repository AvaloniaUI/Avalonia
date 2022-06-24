using System.Collections.Generic;

namespace Avalonia.Platform.Storage;

/// <summary>
/// Options class for <see cref="IStorageProvider.SaveFilePickerAsync"/> method.
/// </summary>
public class FilePickerSaveOptions
{
    /// <summary>
    /// Gets or sets the text that appears in the title bar of a file dialog.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the file name that the file save picker suggests to the user.
    /// </summary>
    public string? SuggestedFileName { get; set; }

    /// <summary>
    /// Gets or sets the default extension to be used to save the file.
    /// </summary>
    public string? DefaultExtension { get; set; }

    /// <summary>
    /// Gets or sets the initial location where the file open picker looks for files to present to the user.
    /// </summary>
    public IStorageFolder? SuggestedStartLocation { get; set; }

    /// <summary>
    /// Gets or sets the collection of valid file types that the user can choose to assign to a file.
    /// </summary>
    public IReadOnlyList<FilePickerFileType>? FileTypeChoices { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether file open picker displays a warning if the user specifies the name of a file that already exists.
    /// </summary>
    public bool? ShowOverwritePrompt { get; set; }
}
