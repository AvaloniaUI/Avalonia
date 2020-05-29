using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Native.Interop;

namespace Avalonia.Native
{
    public class SystemDialogs : ISystemDialogImpl
    {
        IAvnSystemDialogs _native;

        public SystemDialogs(IAvnSystemDialogs native)
        {
            _native = native;
        }

        public Task<string[]> ShowFileDialogAsync(FileDialog dialog, Window parent)
        {
            var events = new SystemDialogEvents();

            var nativeParent = GetNativeWindow(parent);

            if (dialog is OpenFileDialog ofd)
            {
                _native.OpenFileDialog(nativeParent,
                                        events, ofd.AllowMultiple,
                                        ofd.Title ?? "",
                                        ofd.Directory ?? "",
                                        ofd.InitialFileName ?? "",
                                        string.Join(";", dialog.Filters.SelectMany(f => f.Extensions)));
            }
            else
            {
                _native.SaveFileDialog(nativeParent,
                                        events,
                                        dialog.Title ?? "",
                                        dialog.Directory ?? "",
                                        dialog.InitialFileName ?? "",
                                        string.Join(";", dialog.Filters.SelectMany(f => f.Extensions)));
            }

            return events.Task.ContinueWith(t => { events.Dispose(); return t.Result; });
        }

        public Task<string> ShowFolderDialogAsync(OpenFolderDialog dialog, Window parent)
        {
            var events = new SystemDialogEvents();

            var nativeParent = GetNativeWindow(parent);

            _native.SelectFolderDialog(nativeParent, events, dialog.Title ?? "", dialog.Directory ?? "");

            return events.Task.ContinueWith(t => { events.Dispose(); return t.Result.FirstOrDefault(); });
        }

        private IAvnWindow GetNativeWindow(Window window)
        {
            return (window?.PlatformImpl as WindowImpl)?.Native;
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
