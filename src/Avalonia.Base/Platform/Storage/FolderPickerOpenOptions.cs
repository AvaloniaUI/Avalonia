namespace Avalonia.Platform.Storage;

/// <summary>
/// Options class for <see cref="IStorageProvider.OpenFolderPickerAsync"/> method.
/// </summary>
public class FolderPickerOpenOptions
{
    /// <summary>
    /// Gets or sets the text that appears in the title bar of a folder dialog.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets an option indicating whether open picker allows users to select multiple folders.
    /// </summary>
    public bool AllowMultiple { get; set; }

    /// <summary>
    /// Gets or sets the initial location where the file open picker looks for files to present to the user.
    /// </summary>
    public IStorageFolder? SuggestedStartLocation { get; set; }
}
