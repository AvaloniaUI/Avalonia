﻿namespace Avalonia.Platform.Storage;

/// <summary>
/// Common options for <see cref="IStorageProvider.OpenFolderPickerAsync"/>, <see cref="IStorageProvider.OpenFilePickerAsync"/> and <see cref="IStorageProvider.SaveFilePickerAsync"/> methods. 
/// </summary>
public class PickerOptions
{
    /// <summary>
    /// Gets or sets the text that appears in the title bar of a folder dialog.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the initial location where the file open picker looks for files to present to the user.
    /// Can be obtained from previously picked folder or using <see cref="IStorageProvider.TryGetFolderFromPathAsync"/>
    /// or <see cref="IStorageProvider.TryGetWellKnownFolderAsync"/>.
    /// </summary>
    public IStorageFolder? SuggestedStartLocation { get; set; }
}
