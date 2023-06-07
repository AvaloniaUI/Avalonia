using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;

namespace Avalonia.Controls
{
    /// <summary>
    /// Base class for system file dialogs.
    /// </summary>
    [Obsolete("Use Window.StorageProvider API or TopLevel.StorageProvider API"), EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class FileDialog : FileSystemDialog
    {
        /// <summary>
        /// Gets or sets a collection of filters which determine the types of files displayed in an
        /// <see cref="OpenFileDialog"/> or an <see cref="SaveFileDialog"/>.
        /// </summary>
        public List<FileDialogFilter> Filters { get; set; } = new List<FileDialogFilter>();

        /// <summary>
        /// Gets or sets initial file name that is displayed when the dialog is opened.
        /// </summary>
        public string? InitialFileName { get; set; }        
    }

    /// <summary>
    /// Base class for system file and directory dialogs.
    /// </summary>
    [Obsolete("Use Window.StorageProvider API or TopLevel.StorageProvider API"), EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class FileSystemDialog : SystemDialog
    {
        /// <summary>
        /// Gets or sets the initial directory that will be displayed when the file system dialog
        /// is opened.
        /// </summary>
        public string? Directory { get; set; }
    }

    /// <summary>
    /// Represents a system dialog that prompts the user to select a location for saving a file.
    /// </summary>
    [Obsolete("Use Window.StorageProvider API or TopLevel.StorageProvider API"), EditorBrowsable(EditorBrowsableState.Never)]
    public class SaveFileDialog : FileDialog
    {
        /// <summary>
        /// Gets or sets the default extension to be used to save the file (including the period ".").
        /// </summary>
        public string? DefaultExtension { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to display a warning if the user specifies the name of a file that already exists.
        /// </summary>
        public bool? ShowOverwritePrompt { get; set; }

        /// <summary>
        /// Shows the save file dialog.
        /// </summary>
        /// <param name="parent">The parent window.</param>
        /// <returns>
        /// A task that on completion contains the full path of the save location, or null if the
        /// dialog was canceled.
        /// </returns>
        public async Task<string?> ShowAsync(Window parent)
        {
            if(parent == null)
                throw new ArgumentNullException(nameof(parent));
            var service = AvaloniaLocator.Current.GetRequiredService<ISystemDialogImpl>();
            return (await service.ShowFileDialogAsync(this, parent) ??
             Array.Empty<string>()).FirstOrDefault();
        }

        public FilePickerSaveOptions ToFilePickerSaveOptions()
        {
            return new FilePickerSaveOptions
            {
                SuggestedFileName = InitialFileName,
                DefaultExtension = DefaultExtension,
                FileTypeChoices = Filters?.Select(f => new FilePickerFileType(f.Name!) { Patterns = f.Extensions.Select(e => $"*.{e}").ToArray() }).ToArray(),
                Title = Title,
                SuggestedStartLocation = Directory is { } directory
                        ? new BclStorageFolder(new System.IO.DirectoryInfo(directory))
                        : null,
                ShowOverwritePrompt = ShowOverwritePrompt
            };
        }
    }

    /// <summary>
    /// Represents a system dialog that allows the user to select one or more files to open.
    /// </summary>
    [Obsolete("Use Window.StorageProvider API or TopLevel.StorageProvider API"), EditorBrowsable(EditorBrowsableState.Never)]
    public class OpenFileDialog : FileDialog
    {
        /// <summary>
        /// Gets or sets a value indicating whether the user can select multiple files.
        /// </summary>
        public bool AllowMultiple { get; set; }

        /// <summary>
        /// Shows the open file dialog.
        /// </summary>
        /// <param name="parent">The parent window.</param>
        /// <returns>
        /// A task that on completion returns an array containing the full path to the selected
        /// files, or null if the dialog was canceled.
        /// </returns>
        public Task<string[]?> ShowAsync(Window parent)
        {
            if(parent == null)
                throw new ArgumentNullException(nameof(parent));
            var service = AvaloniaLocator.Current.GetRequiredService<ISystemDialogImpl>();
            return service.ShowFileDialogAsync(this, parent);
        }

        public FilePickerOpenOptions ToFilePickerOpenOptions()
        {
            return new FilePickerOpenOptions
            {
                AllowMultiple = AllowMultiple,
                FileTypeFilter = Filters?.Select(f => new FilePickerFileType(f.Name!) { Patterns = f.Extensions.Select(e => $"*.{e}").ToArray() }).ToArray(),
                Title = Title,
                SuggestedStartLocation = Directory is { } directory
                    ? new BclStorageFolder(new System.IO.DirectoryInfo(directory))
                    : null
            };
        }
    }

    /// <summary>
    /// Represents a system dialog that allows the user to select a directory.
    /// </summary>
    [Obsolete("Use Window.StorageProvider API or TopLevel.StorageProvider API"), EditorBrowsable(EditorBrowsableState.Never)]
    public class OpenFolderDialog : FileSystemDialog
    {
        /// <summary>
        /// Shows the open folder dialog.
        /// </summary>
        /// <param name="parent">The parent window.</param>
        /// <returns>
        /// A task that on completion returns the full path of the selected directory, or null if the
        /// dialog was canceled.
        /// </returns>
        public Task<string?> ShowAsync(Window parent)
        {
            if(parent == null)
                throw new ArgumentNullException(nameof(parent));
            var service = AvaloniaLocator.Current.GetRequiredService<ISystemDialogImpl>();
            return service.ShowFolderDialogAsync(this, parent);
        }

        public FolderPickerOpenOptions ToFolderPickerOpenOptions()
        {
            return new FolderPickerOpenOptions
            {
                Title = Title,
                SuggestedStartLocation = Directory is { } directory
                    ? new BclStorageFolder(new System.IO.DirectoryInfo(directory))
                    : null
            };
        }
    }


    /// <summary>
    /// Base class for system dialogs.
    /// </summary>
    [Obsolete("Use Window.StorageProvider API or TopLevel.StorageProvider API"), EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class SystemDialog
    {
        static SystemDialog()
        {
            if (AvaloniaLocator.Current.GetService<ISystemDialogImpl>() is null)
            {
                // Register default implementation.
                AvaloniaLocator.CurrentMutable.Bind<ISystemDialogImpl>().ToSingleton<SystemDialogImpl>();
            }
        }
        
        /// <summary>
        /// Gets or sets the dialog title.
        /// </summary>
        public string? Title { get; set; }
    }

    /// <summary>
    /// Represents a filter in an <see cref="OpenFileDialog"/> or an <see cref="SaveFileDialog"/>.
    /// </summary>
    [Obsolete("Use Window.StorageProvider API or TopLevel.StorageProvider API"), EditorBrowsable(EditorBrowsableState.Never)]
    public class FileDialogFilter
    {
        /// <summary>
        /// Gets or sets the name of the filter, e.g. ("Text files (.txt)").
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets a list of file extensions matched by the filter (e.g. "txt" or "*" for all
        /// files).
        /// </summary>
        public List<string> Extensions { get; set; } = new List<string>();
    }
}
