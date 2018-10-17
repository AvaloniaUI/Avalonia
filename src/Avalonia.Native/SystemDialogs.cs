// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Native.Interop;
using Avalonia.Platform;

namespace Avalonia.Native
{
    public class SystemDialogs : ISystemDialogImpl
    {
        IAvnSystemDialogs _native;

        public SystemDialogs(IAvnSystemDialogs native)
        {
            _native = native;
        }

        public Task<string[]> ShowFileDialogAsync(FileDialog dialog, IWindowImpl parent)
        {
            var events = new SystemDialogEvents();

            if (dialog is OpenFileDialog ofd)
            {
                _native.OpenFileDialog((parent as WindowImpl)?.Native,
                                        events, ofd.AllowMultiple,
                                        ofd.Title ?? "",
                                        ofd.InitialDirectory ?? "",
                                        ofd.InitialFileName ?? "",
                                        string.Join(";", dialog.Filters.SelectMany(f => f.Extensions)));
            }
            else
            {
                _native.SaveFileDialog((parent as WindowImpl)?.Native,
                                        events,
                                        dialog.Title ?? "",
                                        dialog.InitialDirectory ?? "",
                                        dialog.InitialFileName ?? "",
                                        string.Join(";", dialog.Filters.SelectMany(f => f.Extensions)));
            }

            return events.Task.ContinueWith(t => { events.Dispose(); return t.Result; });
        }

        public Task<string> ShowFolderDialogAsync(OpenFolderDialog dialog, IWindowImpl parent)
        {
            var events = new SystemDialogEvents();

            _native.SelectFolderDialog((parent as WindowImpl)?.Native, events, dialog.Title ?? "", dialog.InitialDirectory ?? "");

            return events.Task.ContinueWith(t => { events.Dispose(); return t.Result.FirstOrDefault(); });
        }
    }

    public class SystemDialogEvents : CallbackBase, IAvnSystemDialogEvents
    {
        private TaskCompletionSource<string[]> _tcs;

        public SystemDialogEvents()
        {
            _tcs = new TaskCompletionSource<string[]>();
        }

        public Task<string[]> Task => _tcs.Task;

        public void OnCompleted(int numResults, IntPtr trFirstResultRef)
        {
            string[] results = new string[numResults];

            unsafe
            {
                var ptr = (IntPtr*)trFirstResultRef.ToPointer();

                for (int i = 0; i < numResults; i++)
                {
                    results[i] = Marshal.PtrToStringAnsi(*ptr);

                    ptr++;
                }
            }

            _tcs.SetResult(results);
        }
    }
}
