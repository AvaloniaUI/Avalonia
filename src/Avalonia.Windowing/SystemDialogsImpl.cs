using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Platform;

namespace Avalonia.Windowing
{
    public class SystemDialogsImpl : ISystemDialogImpl
    {
        [DllImport("winit_wrapper")]
        private static extern IntPtr winit_open_file_dialog(IntPtr initalPathString, IntPtr filterString);

        [DllImport("winit_wrapper")]
        private static extern void winit_free_string(IntPtr stringPtr);

        public Task<string[]> ShowFileDialogAsync(FileDialog dialog, IWindowImpl parent)
        {
            //return Task.Run(() =>
            //{
                if (dialog is OpenFileDialog openDialog)
                {
                    var initialPathPtr = Marshal.StringToHGlobalAnsi(dialog.InitialFileName);
                    var filtersPtr = Marshal.StringToHGlobalAnsi("");

                    var result = winit_open_file_dialog(initialPathPtr, filtersPtr);

                    Marshal.FreeHGlobal(initialPathPtr);
                    Marshal.FreeHGlobal(filtersPtr);
                }
                else
                {
                    // Save file dialog
                }

            return Task.FromResult(new string[0]);
            //});
        }

        public Task<string> ShowFolderDialogAsync(OpenFolderDialog dialog, IWindowImpl parent)
        {
            throw new NotImplementedException();
        }
    }
}
