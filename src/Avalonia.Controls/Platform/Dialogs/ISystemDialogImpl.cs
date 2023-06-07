using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Controls.Platform
{
    /// <summary>
    /// Defines a platform-specific system dialog implementation.
    /// </summary>
    [Obsolete("Use Window.StorageProvider API or TopLevel.StorageProvider API"), EditorBrowsable(EditorBrowsableState.Never)]
    [Unstable]
    public interface ISystemDialogImpl
    {
        /// <summary>
        /// Shows a file dialog.
        /// </summary>
        /// <param name="dialog">The details of the file dialog to show.</param>
        /// <param name="parent">The parent window.</param>
        /// <returns>A task returning the selected filenames.</returns>
        Task<string[]?> ShowFileDialogAsync(FileDialog dialog, Window parent);

        Task<string?> ShowFolderDialogAsync(OpenFolderDialog dialog, Window parent);
    }
}
