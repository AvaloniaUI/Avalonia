using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Gtk3.Interop;
using Avalonia.Platform;
using Avalonia.Platform.Interop;

namespace Avalonia.Gtk3
{
    public class Gtk3ForeignX11SystemDialog : ISystemDialogImpl
    {
        private Task<bool> _initialized;
        private SystemDialogBase _inner = new SystemDialogBase();


        public async Task<string[]> ShowFileDialogAsync(FileDialog dialog, IWindowImpl parent)
        {
            await EnsureInitialized();
            var xid = parent.Handle.Handle;
            return await await RunOnGtkThread(
                () => _inner.ShowFileDialogAsync(dialog, GtkWindow.Null, chooser => UpdateParent(chooser, xid)));
        }

        public async Task<string> ShowFolderDialogAsync(OpenFolderDialog dialog, IWindowImpl parent)
        {
            await EnsureInitialized();
            var xid = parent.Handle.Handle;
            return await await RunOnGtkThread(
                () => _inner.ShowFolderDialogAsync(dialog, GtkWindow.Null, chooser => UpdateParent(chooser, xid)));
        }

        void UpdateParent(GtkFileChooser chooser, IntPtr xid)
        {
            Native.GtkWidgetRealize(chooser);
            var window = Native.GtkWidgetGetWindow(chooser);
            var parent = Native.GdkWindowForeignNewForDisplay(GdkDisplay, xid);
            if (window != IntPtr.Zero && parent != IntPtr.Zero)
                Native.GdkWindowSetTransientFor(window, parent);
        }
        
        async Task EnsureInitialized()
        {
            if (_initialized == null)
            {
                var tcs = new TaskCompletionSource<bool>();
                _initialized = tcs.Task;
                new Thread(() => GtkThread(tcs))
                {
                    IsBackground = true
                }.Start();
            }

            if (!(await _initialized))
                throw new Exception("Unable to initialize GTK on separate thread");

        }
        
        Task<T> RunOnGtkThread<T>(Func<T> action)
        {
            var tcs = new TaskCompletionSource<T>();
            GlibTimeout.Add(0, 0, () =>
            {

                try
                {
                    tcs.SetResult(action());
                }
                catch (Exception e)
                {
                    tcs.TrySetException(e);
                }

                return false;
            });
            return tcs.Task;
        }
        

        void GtkThread(TaskCompletionSource<bool> tcs)
        {
            try
            {
                X11.XInitThreads();
            }catch{}
            Resolver.Resolve();
            if (Native.GdkWindowForeignNewForDisplay == null)
                throw new Exception("gdk_x11_window_foreign_new_for_display is not found in your libgdk-3.so");
            using (var backends = new Utf8Buffer("x11"))
                Native.GdkSetAllowedBackends?.Invoke(backends);
            if (!Native.GtkInitCheck(0, IntPtr.Zero))
            {
                tcs.SetResult(false);
                return;
            }

            using (var utf = new Utf8Buffer($"avalonia.app.a{Guid.NewGuid().ToString("N")}"))
                App = Native.GtkApplicationNew(utf, 0);
            if (App == IntPtr.Zero)
            {
                tcs.SetResult(false);
                return;
            }
            GdkDisplay = Native.GdkGetDefaultDisplay();
            tcs.SetResult(true);
            while (true)
                Native.GtkMainIteration();
        }

        private IntPtr GdkDisplay { get; set; }
        private IntPtr App { get; set; }
    }
}
