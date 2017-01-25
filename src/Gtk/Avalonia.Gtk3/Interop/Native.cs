using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Gtk3.Interop
{
    static class Native
    {
        public static class D
        {
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate IntPtr gtk_application_new([MarshalAs(UnmanagedType.AnsiBStr)] string appId, int flags);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_main_iteration();

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate IntPtr gtk_window_new(GtkWindowType windowType);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate IntPtr gtk_init(int argc, IntPtr argv);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_window_present(IntPtr gtkWindow);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_widget_hide(IntPtr gtkWidget);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)] //No manual import
            public delegate IntPtr gdk_get_native_handle(IntPtr gdkWindow);


            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate IntPtr gtk_widget_get_window(IntPtr gtkWidget);
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate IntPtr gtk_widget_get_screen(IntPtr gtkWidget);
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate IntPtr gtk_widget_set_double_buffered(IntPtr gtkWidget, bool value);
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate IntPtr gtk_widget_set_events(IntPtr gtkWidget, uint flags);


            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gdk)]
            public delegate int gdk_screen_get_height(IntPtr screen);
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gdk)]
            public delegate int gdk_screen_get_width(IntPtr screen);



            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_widget_realize(IntPtr gtkWidget);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_window_set_title(IntPtr gtkWindow, Utf8Buffer title);


            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_window_set_decorated(IntPtr gtkWindow, bool decorated);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_window_get_size(IntPtr gtkWindow, out int width, out int height);
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_window_resize(IntPtr gtkWindow, int width, int height);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_widget_queue_draw_area(IntPtr gtkWindow, int x, int y, int width, int height);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gdk)]
            public delegate void gdk_window_invalidate_rect(IntPtr window, ref GdkRectangle rect, bool invalidate_children);
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gobject)]
            public delegate ulong g_signal_connect_object(IntPtr instance, [MarshalAs(UnmanagedType.AnsiBStr)]string signal, IntPtr handler, IntPtr userData, int flags);
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gobject)]
            public delegate ulong g_signal_handler_disconnect(IntPtr instance, ulong connectionId);
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Glib)]
            public delegate ulong g_timeout_add(uint interval, timeout_callback callback, IntPtr data);
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate bool signal_widget_draw(IntPtr gtkWidget, IntPtr cairoContext, IntPtr userData);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate bool signal_onevent(IntPtr gtkWidget, IntPtr ev, IntPtr userData);


            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate bool timeout_callback(IntPtr data);
        }


        public static D.gtk_window_set_decorated GtkWindowSetDecorated;
        public static D.gtk_window_set_title GtkWindowSetTitle;
        public static D.gtk_application_new GtkApplicationNew;
        public static D.gtk_main_iteration GtkMainIteration;
        public static D.gtk_window_new GtkWindowNew;
        public static D.gtk_init GtkInit;
        public static D.gtk_window_present GtkWindowPresent;
        public static D.gtk_widget_hide GtkWidgetHide;
        public static D.gdk_get_native_handle GetNativeGdkWindowHandle;
        public static D.gtk_widget_get_window GtkWidgetGetWindow;
        public static D.gtk_widget_get_screen GtkWidgetGetScreen;
        public static D.gtk_widget_realize GtkWidgetRealize;
        public static D.gtk_window_get_size GtkWindowGetSize;
        public static D.gtk_window_resize GtkWindowResize;
        public static D.g_signal_connect_object GSignalConnectObject;
        public static D.g_signal_handler_disconnect GSignalHandlerDisconnect;
        public static D.g_timeout_add GTimeoutAdd;
        public static D.gtk_widget_set_double_buffered GtkWidgetSetDoubleBuffered;
        public static D.gtk_widget_set_events GtkWidgetSetEvents;
        public static D.gdk_window_invalidate_rect GdkWindowInvalidateRect;
        public static D.gtk_widget_queue_draw_area GtkWidgetQueueDrawArea;
        public static D.gdk_screen_get_height GdkScreenGetHeight;
        public static D.gdk_screen_get_width GdkScreenGetWidth;


    }

    public enum GtkWindowType
    {
        TopLevel,
        Popup
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct GdkRectangle
    {
        public int X, Y, Width, Height;

        public static GdkRectangle FromRect(Rect rect)
        {
            return new GdkRectangle
            {
                X = (int) rect.X,
                Y = (int) rect.Y,
                Width = (int) rect.Width,
                Height = (int) rect.Height
            };
        }
    }
}
