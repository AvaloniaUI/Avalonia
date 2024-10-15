using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Interop;
using Avalonia.Threading;
using Avalonia.X11.Dispatching;
using Avalonia.X11.Interop;

// ReSharper disable IdentifierTypo
namespace Avalonia.X11.NativeDialogs
{
    internal enum GtkFileChooserAction
    {
        Open,
        Save,
        SelectFolder,
    }

    // ReSharper disable UnusedMember.Global
    internal enum GtkResponseType
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

    internal static unsafe class Gtk
    {
        private static IntPtr s_display;
        private const string GdkName = "libgdk-3.so.0";
        private const string GtkName = "libgtk-3.so.0";

        [DllImport(GtkName)]
        private static extern void gtk_main_iteration();


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
        public static extern void gtk_file_chooser_set_do_overwrite_confirmation(IntPtr chooser, bool do_overwrite_confirmation);

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
        public static extern void gtk_file_chooser_set_current_folder(IntPtr chooser, Utf8Buffer file);

        [DllImport(GtkName)]
        public static extern IntPtr gtk_file_filter_new();
        
        [DllImport(GtkName)]
        public static extern IntPtr gtk_file_filter_set_name(IntPtr filter, Utf8Buffer name);
        
        [DllImport(GtkName)]
        public static extern IntPtr gtk_file_filter_add_pattern(IntPtr filter, Utf8Buffer pattern);
        
        [DllImport(GtkName)]
        public static extern IntPtr gtk_file_filter_add_mime_type (IntPtr filter, Utf8Buffer mimeType);
        
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
        private static extern bool gtk_init_check(int argc, IntPtr argv);

        [DllImport(GdkName)]
        private static extern IntPtr gdk_x11_window_foreign_new_for_display(IntPtr display, IntPtr xid);
        
        [DllImport(GdkName)]
        public static extern IntPtr gdk_x11_window_get_xid(IntPtr window);


        [DllImport(GtkName)]
        public static extern IntPtr gtk_container_add(IntPtr container, IntPtr widget);

        [DllImport(GdkName)]
        private static extern IntPtr gdk_set_allowed_backends(Utf8Buffer backends);

        [DllImport(GdkName)]
        private static extern IntPtr gdk_display_get_default();
        
        [DllImport(GdkName)]
        private static extern IntPtr gdk_x11_display_get_xdisplay(IntPtr display);

        [DllImport(GtkName)]
        private static extern IntPtr gtk_application_new(Utf8Buffer appId, int flags);

        [DllImport(GdkName)]
        public static extern void gdk_window_set_transient_for(IntPtr window, IntPtr parent);

        public static IntPtr GetForeignWindow(IntPtr xid) => gdk_x11_window_foreign_new_for_display(s_display, xid);

        static object s_startGtkLock = new();
        static Task<bool> s_startGtkTask;

        public static Task<bool> StartGtk()
        {
            lock (s_startGtkLock)
                return s_startGtkTask ??= StartGtkCore();
        }

        static bool InitializeGtk()
        {
            try
            {
                // Check if GTK was already initialized
                var existingDisplay = gdk_display_get_default();
                if (existingDisplay != IntPtr.Zero)
                {
                    if (gdk_x11_display_get_xdisplay(existingDisplay) == IntPtr.Zero)
                        return false;
                    s_display = existingDisplay;
                    return true;

                }
                
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
                    return false;
                }

                IntPtr app;
                using (var utf = new Utf8Buffer($"avalonia.app.a{Guid.NewGuid():N}"))
                    app = gtk_application_new(utf, 0);
                if (app == IntPtr.Zero)
                {
                    return false;
                }

                s_display = gdk_display_get_default();
            }
            catch
            {
                return false;
            }

            return true;
        }
        
        private static void GtkThread(TaskCompletionSource<bool> tcs)
        {
            try
            {
                if (!InitializeGtk())
                {
                    tcs.SetResult(false);
                    return;
                }
                
                tcs.SetResult(true);
                while (true)
                    gtk_main_iteration();
            }
            catch
            {
                tcs.TrySetResult(false);
            }
        }
        
        private static Task<bool> StartGtkCore()
        {
            if (AvaloniaLocator.Current.GetService<X11PlatformOptions>()?.UseGLibMainLoop == true)
            {
                return Task.FromResult(InitializeGtk());
            }
            else
            {
                var tcs = new TaskCompletionSource<bool>();
                new Thread(() => GtkThread(tcs)) { Name = "GTK3THREAD", IsBackground = true }.Start();
                return tcs.Task;
            }
        }
    }
}
