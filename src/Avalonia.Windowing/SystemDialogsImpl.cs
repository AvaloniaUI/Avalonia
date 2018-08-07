using System;
using System.Collections.Generic;
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
        private static extern void winit_open_file_dialog(IntPtr initalPathString, IntPtr filterString, DialogResultCallback callback);

        [DllImport("winit_wrapper")]
        private static extern void winit_free_string(IntPtr stringPtr);

        private delegate void DialogResultCallback(IntPtr result);

        private List<object> _pinnedDelegates = new List<object>();

        public Task<string[]> ShowFileDialogAsync(FileDialog dialog, IWindowImpl parent)
        {
            var completionSource = new TaskCompletionSource<string[]>();

            if (dialog is OpenFileDialog openDialog)
            {
                var initialPathPtr = Marshal.StringToHGlobalAnsi(dialog.InitialFileName);
                var filtersPtr = Marshal.StringToHGlobalAnsi("");

                DialogResultCallback del = null;

                del = resultPtr =>
                {
                    var paths = Marshal.PtrToStringAnsi(resultPtr);

                    _pinnedDelegates.Remove(del);

                    completionSource.SetResult(paths.Split(';'));
                };

                _pinnedDelegates.Add(del);
                winit_open_file_dialog(initialPathPtr, filtersPtr, del);

                Marshal.FreeHGlobal(initialPathPtr);
                Marshal.FreeHGlobal(filtersPtr);
            }
            else
            {
                // Save file dialog
            }

            return completionSource.Task;
        }

        public Task<string> ShowFolderDialogAsync(OpenFolderDialog dialog, IWindowImpl parent)
        {
            throw new NotImplementedException();
        }
    }
}
