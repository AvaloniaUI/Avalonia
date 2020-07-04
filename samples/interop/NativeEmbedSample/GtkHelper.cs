using System;
using System.Threading.Tasks;
using Avalonia.Controls.Platform;
using Avalonia.Platform;
using Avalonia.Platform.Interop;
using Avalonia.X11.NativeDialogs;
using static Avalonia.X11.NativeDialogs.Gtk;
using static Avalonia.X11.NativeDialogs.Glib;
namespace NativeEmbedSample
{
    public class GtkHelper
    {
        private static  Task<bool> s_gtkTask;
        class FileChooser : INativeControlHostDestroyableControlHandle
        {
            private readonly IntPtr _widget;

            public FileChooser(IntPtr widget, IntPtr xid)
            {
                _widget = widget;
                Handle = xid;
            }

            public IntPtr Handle { get; }
            public string HandleDescriptor => "XID";
            public void Destroy()
            {
                RunOnGlibThread(() =>
                {
                    gtk_widget_destroy(_widget);
                    return 0;
                }).Wait();
            }
        }

        
        
        public static IPlatformHandle CreateGtkFileChooser(IntPtr parentXid)
        {
            if (s_gtkTask == null)
                s_gtkTask = StartGtk();
            if (!s_gtkTask.Result)
                return null;
            return RunOnGlibThread(() =>
            {
                using (var title = new Utf8Buffer("Embedded"))
                {
                    var widget = gtk_file_chooser_dialog_new(title, IntPtr.Zero, GtkFileChooserAction.SelectFolder,
                        IntPtr.Zero);
                    gtk_widget_realize(widget);
                    var xid = gdk_x11_window_get_xid(gtk_widget_get_window(widget));
                    gtk_window_present(widget);
                    return new FileChooser(widget,  xid);
                }
            }).Result;
        }
    }
}
