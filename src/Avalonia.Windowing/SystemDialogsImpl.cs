using System;
using System.Collections.Generic;
using System.IO;
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
        private static extern void winit_open_file_dialog(byte allowMultiple, IntPtr title, IntPtr initalPathString, IntPtr filterString, DialogResultCallback callback);

        [DllImport("winit_wrapper")]
        private static extern void winit_open_folder_dialog(IntPtr title, IntPtr initalPathString, IntPtr filterString, DialogResultCallback callback);

        [DllImport("winit_wrapper")]
        private static extern void winit_save_file_dialog(IntPtr title, IntPtr initalPathString, IntPtr initialFileString, IntPtr filterString, DialogResultCallback callback);


        [DllImport("winit_wrapper")]
        private static extern void winit_free_string(IntPtr stringPtr);

        private delegate void DialogResultCallback(IntPtr result);

        private List<object> _pinnedDelegates = new List<object>();

        public Task<string[]> ShowFileDialogAsync(FileDialog dialog, IWindowImpl parent)
        {
            var completionSource = new TaskCompletionSource<string[]>();

            DialogResultCallback del = null;

            del = resultPtr =>
            {
                var paths = Marshal.PtrToStringAnsi(resultPtr);

                _pinnedDelegates.Remove(del);

                completionSource.SetResult(paths?.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries));
            };

            _pinnedDelegates.Add(del);

            IntPtr initialPathPtr;
            
            var filtersPtr = Marshal.StringToHGlobalAnsi("");
            var titlePtr = Marshal.StringToHGlobalAnsi(dialog.Title);

            if (dialog is OpenFileDialog openDialog)
            {
                initialPathPtr = Marshal.StringToHGlobalAnsi(Path.Combine(string.IsNullOrEmpty(dialog.InitialDirectory) ? "" : dialog.InitialDirectory,
                                                                          string.IsNullOrEmpty(dialog.InitialFileName) ? "" : dialog.InitialFileName));
                winit_open_file_dialog((byte)(openDialog.AllowMultiple ? 1 : 0), titlePtr, initialPathPtr, filtersPtr, del);
            }
            else
            {
                var initialFileNamePtr = Marshal.StringToHGlobalAnsi(dialog.InitialFileName);
                initialPathPtr = Marshal.StringToHGlobalAnsi(dialog.InitialDirectory);
                winit_save_file_dialog(titlePtr, initialPathPtr, initialFileNamePtr, filtersPtr, del);
                Marshal.FreeHGlobal(initialFileNamePtr);
            }

            Marshal.FreeHGlobal(initialPathPtr);
            Marshal.FreeHGlobal(filtersPtr);
            Marshal.FreeHGlobal(titlePtr);

            return completionSource.Task;
        }

        public Task<string> ShowFolderDialogAsync(OpenFolderDialog dialog, IWindowImpl parent)
        {
            var completionSource = new TaskCompletionSource<string>();
                  
            DialogResultCallback del = null;

            del = resultPtr =>
            {
                var paths = Marshal.PtrToStringAnsi(resultPtr);

                _pinnedDelegates.Remove(del);

                completionSource.SetResult(paths?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            };

            _pinnedDelegates.Add(del);

            var initialPathPtr = Marshal.StringToHGlobalAnsi(dialog.InitialDirectory);
            var titlePtr = Marshal.StringToHGlobalAnsi(dialog.Title);
            var filtersPtr = Marshal.StringToHGlobalAnsi("");

            winit_open_folder_dialog(titlePtr, initialPathPtr, filtersPtr, del);

            Marshal.FreeHGlobal(initialPathPtr);
            Marshal.FreeHGlobal(filtersPtr);
            Marshal.FreeHGlobal(titlePtr);

            return completionSource.Task;
        }
    }
}
