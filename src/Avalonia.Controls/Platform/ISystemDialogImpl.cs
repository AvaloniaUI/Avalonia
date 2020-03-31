using System.Threading.Tasks;
using Avalonia.Platform;

namespace Avalonia.Controls.Platform
{
    /// <summary>
    /// Defines a platform-specific system dialog implementation.
    /// </summary>
    public interface ISystemDialogImpl
    {
        /// <summary>
        /// Shows a file dialog.
        /// </summary>
        /// <param name="dialog">The details of the file dialog to show.</param>
        /// <param name="parent">The parent window.</param>
        /// <returns>A task returning the selected filenames.</returns>
        Task<string[]> ShowFileDialogAsync(FileDialog dialog, IWindowImpl parent);

        Task<string> ShowFolderDialogAsync(OpenFolderDialog dialog, IWindowImpl parent);
    }
}
