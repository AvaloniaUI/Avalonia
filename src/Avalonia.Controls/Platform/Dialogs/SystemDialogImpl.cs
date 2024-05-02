using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace Avalonia.Controls.Platform
{
    /// <summary>
    /// Defines a platform-specific system dialog implementation.
    /// </summary>
    [Obsolete, EditorBrowsable(EditorBrowsableState.Never)]
    internal class SystemDialogImpl : ISystemDialogImpl
    {
        public async Task<string[]?> ShowFileDialogAsync(FileDialog dialog, Window parent)
        {
            if (dialog is OpenFileDialog openDialog)
            {
                var filePicker = parent.StorageProvider;
                if (!filePicker.CanOpen)
                {
                    return null;
                }

                var options = openDialog.ToFilePickerOpenOptions();

                var files = await filePicker.OpenFilePickerAsync(options);
                return files
                    .Select(file => file.TryGetLocalPath() ?? file.Name)
                    .ToArray();
            }
            else if (dialog is SaveFileDialog saveDialog)
            {
                var filePicker = parent.StorageProvider;
                if (!filePicker.CanSave)
                {
                    return null;
                }

                var options = saveDialog.ToFilePickerSaveOptions();

                var file = await filePicker.SaveFilePickerAsync(options);
                if (file is null)
                {
                    return null;
                }

                var filePath = file.TryGetLocalPath() ?? file.Name;
                return new[] { filePath };
            }
            return null;
        }

        public async Task<string?> ShowFolderDialogAsync(OpenFolderDialog dialog, Window parent)
        {
            var filePicker = parent.StorageProvider;
            if (!filePicker.CanPickFolder)
            {
                return null;
            }

            var options = dialog.ToFolderPickerOpenOptions();

            var folders = await filePicker.OpenFolderPickerAsync(options);
            return folders
                .Select(folder => folder.TryGetLocalPath() ?? folder.Name)
                .FirstOrDefault(u => u is not null);
        }
    }
}
