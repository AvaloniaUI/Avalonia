using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Interop;
using JetBrains.Annotations;

// ReSharper disable IdentifierTypo
namespace Avalonia.X11.NativeDialogs
{

    static unsafe class Glib
    {
        private const string GlibName = "libglib-2.0.so.0";
        private const string GObjectName = "libgobject-2.0.so.0";

        [DllImport(GlibName)]
        public static extern void g_slist_free(GSList* data);

        [DllImport(GObjectName)]
        private static extern void g_object_ref(IntPtr instance);

        [DllImport(GObjectName)]
        private static extern ulong g_signal_connect_object(IntPtr instance, Utf8Buffer signal,
            IntPtr handler, IntPtr userData, int flags);

        [DllImport(GObjectName)]
        private static extern void g_object_unref(IntPtr instance);

        [DllImport(GObjectName)]
        private static extern ulong g_signal_handler_disconnect(IntPtr instance, ulong connectionId);

        private delegate bool timeout_callback(IntPtr data);

        [DllImport(GlibName)]
        private static extern ulong g_timeout_add_full(int prio, uint interval, timeout_callback callback, IntPtr data,
            IntPtr destroy);


        class ConnectedSignal : IDisposable
        {
            private readonly IntPtr _instance;
            private GCHandle _handle;
            private readonly ulong _id;

            public ConnectedSignal(IntPtr instance, GCHandle handle, ulong id)
            {
                _instance = instance;
                g_object_ref(instance);
                _handle = handle;
                _id = id;
            }

            public void Dispose()
            {
                if (_handle.IsAllocated)
                {
                    g_signal_handler_disconnect(_instance, _id);
                    g_object_unref(_instance);
                    _handle.Free();
                }
            }
        }

        public static IDisposable ConnectSignal<T>(IntPtr obj, string name, T handler)
        {
            var handle = GCHandle.Alloc(handler);
            var ptr = Marshal.GetFunctionPointerForDelegate<T>(handler);
            using (var utf = new Utf8Buffer(name))
            {
                var id = g_signal_connect_object(obj, utf, ptr, IntPtr.Zero, 0);
                if (id == 0)
                    throw new ArgumentException("Unable to connect to signal " + name);
                return new ConnectedSignal(obj, handle, id);
            }
        }


        static bool TimeoutHandler(IntPtr data)
        {
            var handle = GCHandle.FromIntPtr(data);
            var cb = (Func<bool>)handle.Target;
            if (!cb())
            {
                handle.Free();
                return false;
            }

            return true;
        }

        private static readonly timeout_callback s_pinnedHandler;

        static Glib()
        {
            s_pinnedHandler = TimeoutHandler;
        }

        static void AddTimeout(int priority, uint interval, Func<bool> callback)
        {
            var handle = GCHandle.Alloc(callback);
            g_timeout_add_full(priority, interval, s_pinnedHandler, GCHandle.ToIntPtr(handle), IntPtr.Zero);
        }

        public static Task<T> RunOnGlibThread<T>(Func<T> action)
        {
            var tcs = new TaskCompletionSource<T>();
            AddTimeout(0, 0, () =>
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
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct GSList
    {
        public readonly IntPtr Data;
        public readonly GSList* Next;
    }

    enum GtkFileChooserAction
    {
        Open,
        Save,
        SelectFolder,
    }

    // ReSharper disable UnusedMember.Global
    enum GtkResponseType
    {
        Help = -11,
        Apply = -10,
        No = -9,
        Yes = -8,
        Close = -7,
        Cancel = -6,
        Ok = -5,
        DeleteEvent = -4,
        Accept = -3,
        Reject = -2,
        None = -1,
    }
    // ReSharper restore UnusedMember.Global

    static unsafe class Gtk
    {
        private static IntPtr s_display;
        private const string GdkName = "libgdk-3.so.0";
        private const string GtkName = "libgtk-3.so.0";

        [DllImport(GtkName)]
        static extern void gtk_main_iteration();


        [DllImport(GtkName)]
        public static extern void gtk_window_set_modal(IntPtr window, bool modal);

        [DllImport(GtkName)]
        public static extern void gtk_window_present(IntPtr gtkWindow);


        public delegate bool signal_generic(IntPtr gtkWidget, IntPtr userData);

        public delegate bool signal_dialog_response(IntPtr gtkWidget, GtkResponseType response, IntPtr userData);

        [DllImport(GtkName)]
        public static extern IntPtr gtk_file_chooser_dialog_new(Utf8Buffer title, IntPtr parent,
            GtkFileChooserAction action, IntPtr ignore);

        [DllImport(GtkName)]
        public static extern void gtk_file_chooser_set_select_multiple(IntPtr chooser, bool allow);

        [DllImport(GtkName)]
        public static extern void
            gtk_dialog_add_button(IntPtr raw, Utf8Buffer button_text, GtkResponseType response_id);

        [DllImport(GtkName)]
        public static extern GSList* gtk_file_chooser_get_filenames(IntPtr chooser);

        [DllImport(GtkName)]
        public static extern void gtk_file_chooser_set_filename(IntPtr chooser, Utf8Buffer file);

        [DllImport(GtkName)]
        public static extern void gtk_file_chooser_set_current_name(IntPtr chooser, Utf8Buffer file);
        
        [DllImport(GtkName)]
        public static extern IntPtr gtk_file_filter_new();
        
        [DllImport(GtkName)]
        public static extern IntPtr gtk_file_filter_set_name(IntPtr filter, Utf8Buffer name);
        
        [DllImport(GtkName)]
        public static extern IntPtr gtk_file_filter_add_pattern(IntPtr filter, Utf8Buffer pattern);
        
        [DllImport(GtkName)]
        public static extern IntPtr gtk_file_chooser_add_filter(IntPtr chooser, IntPtr filter);

        [DllImport(GtkName)]
        public static extern IntPtr gtk_file_chooser_get_filter(IntPtr chooser);
        
        [DllImport(GtkName)]
        public static extern void gtk_widget_realize(IntPtr gtkWidget);
        
        [DllImport(GtkName)]
        public static extern void gtk_widget_destroy(IntPtr gtkWidget);

        [DllImport(GtkName)]
        public static extern IntPtr gtk_widget_get_window(IntPtr gtkWidget);

        [DllImport(GtkName)]
        public static extern void gtk_widget_hide(IntPtr gtkWidget);

        [DllImport(GtkName)]
        static extern bool gtk_init_check(int argc, IntPtr argv);

        [DllImport(GdkName)]
        static extern IntPtr gdk_x11_window_foreign_new_for_display(IntPtr display, IntPtr xid);
        
        [DllImport(GdkName)]
        public static extern IntPtr gdk_x11_window_get_xid(IntPtr window);


        [DllImport(GtkName)]
        public static extern IntPtr gtk_container_add(IntPtr container, IntPtr widget);

        [DllImport(GdkName)]
        static extern IntPtr gdk_set_allowed_backends(Utf8Buffer backends);

        [DllImport(GdkName)]
        static extern IntPtr gdk_display_get_default();

        [DllImport(GtkName)]
        static extern IntPtr gtk_application_new(Utf8Buffer appId, int flags);

        [DllImport(GdkName)]
        public static extern void gdk_window_set_transient_for(IntPtr window, IntPtr parent);

        public static IntPtr GetForeignWindow(IntPtr xid) => gdk_x11_window_foreign_new_for_display(s_display, xid);

        static object s_startGtkLock = new();
        static Task<bool> s_startGtkTask;

        public static Task<bool> StartGtk()
        {
            return StartGtkCore();
            lock (s_startGtkLock)
                return s_startGtkTask ??= StartGtkCore();
        }

        private static void GtkThread(TaskCompletionSource<bool> tcs)
        {
            try
            {
                try
                {
                    using (var backends = new Utf8Buffer("x11"))
                        gdk_set_allowed_backends(backends);
                }
                catch
                {
                    //Ignore
                }

                Environment.SetEnvironmentVariable("WAYLAND_DISPLAY",
                    "/proc/fake-display-to-prevent-wayland-initialization-by-gtk3");

                if (!gtk_init_check(0, IntPtr.Zero))
                {
                    tcs.SetResult(false);
                    return;
                }

                IntPtr app;
                using (var utf = new Utf8Buffer($"avalonia.app.a{Guid.NewGuid():N}"))
                    app = gtk_application_new(utf, 0);
                if (app == IntPtr.Zero)
                {
                    tcs.SetResult(false);
                    return;
                }

                s_display = gdk_display_get_default();
                tcs.SetResult(true);
                while (true)
                    gtk_main_iteration();
            }
            catch
            {
                tcs.SetResult(false);
            }
        }
        
        private static Task<bool> StartGtkCore()
        {
            var tcs = new TaskCompletionSource<bool>();
            new Thread(() => GtkThread(tcs)) {Name = "GTK3THREAD", IsBackground = true}.Start();
            return tcs.Task;
        }
    }
}
