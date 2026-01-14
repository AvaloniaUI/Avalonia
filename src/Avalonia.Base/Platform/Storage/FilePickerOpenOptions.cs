using System.Collections.Generic;

namespace Avalonia.Platform.Storage;

/// <summary>
/// Options class for <see cref="IStorageProvider.OpenFilePickerAsync"/> method.
/// </summary>
public class FilePickerOpenOptions : PickerOptions
{
    /// <summary>
    /// Gets or sets the file type that should be preselected when the dialog is opened.
    /// </summary>
    /// <remarks>
    /// This value should reference one of the items in <see cref="FileTypeFilter"/>.
    /// If not set, the first file type in <see cref="FileTypeFilter"/> may be selected by default.
    /// </remarks>
    public FilePickerFileType? SuggestedFileType { get; set; }

    /// <summary>
    /// Gets or sets an option indicating whether open picker allows users to select multiple files.
    /// </summary>
    public bool AllowMultiple { get; set; }

    /// <summary>
    /// Gets or sets the collection of file types that the file open picker displays.
    /// </summary>
    public IReadOnlyList<FilePickerFileType>? FileTypeFilter { get; set; }
}
