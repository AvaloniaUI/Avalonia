#pragma warning disable 649
using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using gdouble = System.Double;
using gint = System.Int32;
using gint16 = System.Int16;
using gint8 = System.Byte;
using guint = System.UInt32;
using guint16 = System.UInt16;
using guint32 = System.UInt32;

namespace Avalonia.Gtk3.Interop
{
    static class Native
    {
        public static class D
        {
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gdk)]
            public delegate gint16 gdk_display_get_n_screens(IntPtr display);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gdk)]
            public delegate UnownedGdkScreen gdk_display_get_screen(IntPtr display, gint16 num);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gdk)]
            public delegate UnownedGdkScreen gdk_display_get_default_screen (IntPtr display);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gdk)]
            public delegate gint16 gdk_screen_get_n_monitors(GdkScreen screen);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gdk)]
            public delegate gint16 gdk_screen_get_primary_monitor(GdkScreen screen);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gdk)]
            public delegate void gdk_screen_get_monitor_geometry(GdkScreen screen, gint16 num, ref GdkRectangle rect);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gdk)]
            public delegate void gdk_screen_get_monitor_workarea(GdkScreen screen, gint16 num, ref GdkRectangle rect);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate IntPtr gtk_application_new(Utf8Buffer appId, int flags);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_main_iteration();

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate GtkWindow gtk_window_new(GtkWindowType windowType);           
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate IntPtr gtk_init(int argc, IntPtr argv);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gdk, optional: true)]
            public delegate IntPtr gdk_set_allowed_backends (Utf8Buffer backends);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_window_present(GtkWindow gtkWindow);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_widget_hide(GtkWidget gtkWidget);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_widget_show(GtkWidget gtkWidget);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_window_set_icon(GtkWindow window, Pixbuf pixbuf);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_window_set_modal(GtkWindow window, bool modal);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)] //No manual import
            public delegate IntPtr gdk_get_native_handle(IntPtr gdkWindow);


            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate IntPtr gtk_widget_get_window(GtkWidget gtkWidget);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk, optional: true)]
            public delegate uint gtk_widget_get_scale_factor(GtkWidget gtkWidget);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate IntPtr gtk_widget_get_screen(GtkWidget gtkWidget);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate IntPtr gtk_widget_set_double_buffered(GtkWidget gtkWidget, bool value);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate IntPtr gtk_widget_set_events(GtkWidget gtkWidget, uint flags);


            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gdk)]
            public delegate int gdk_screen_get_height(IntPtr screen);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gdk)]
            public delegate int gdk_screen_get_width(IntPtr screen);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gdk)]
            public delegate IntPtr gdk_display_get_default();

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gdk)]
            public delegate int gdk_window_get_origin(IntPtr gdkWindow, out int x, out int y);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gdk)]
            public delegate void gdk_window_resize(IntPtr gtkWindow, int width, int height);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gdk)]
            public delegate void gdk_window_set_override_redirect(IntPtr gdkWindow, bool value);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_widget_realize(GtkWidget gtkWidget);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_window_set_title(GtkWindow gtkWindow, Utf8Buffer title);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_window_set_resizable(GtkWindow gtkWindow, bool resizable);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_window_set_decorated(GtkWindow gtkWindow, bool decorated);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_window_set_skip_taskbar_hint(GtkWindow gtkWindow, bool setting); 
            
             [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate bool gtk_window_get_skip_taskbar_hint(GtkWindow gtkWindow);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_window_set_skip_pager_hint(GtkWindow gtkWindow, bool setting); 
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate bool gtk_window_get_skip_pager_hint(GtkWindow gtkWindow); 

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_window_get_size(GtkWindow gtkWindow, out int width, out int height);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_window_resize(GtkWindow gtkWindow, int width, int height);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_widget_set_size_request(GtkWidget widget, int width, int height);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_window_set_default_size(GtkWindow gtkWindow, int width, int height);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_window_get_position(GtkWindow gtkWindow, out int x, out int y);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_window_move(GtkWindow gtkWindow, int x, int y);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate GtkFileChooser gtk_file_chooser_dialog_new(Utf8Buffer title, GtkWindow parent, GtkFileChooserAction action, IntPtr ignore);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public unsafe delegate GSList* gtk_file_chooser_get_filenames(GtkFileChooser chooser);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_file_chooser_set_select_multiple(GtkFileChooser chooser, bool allow);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_file_chooser_set_filename(GtkFileChooser chooser, Utf8Buffer file);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_dialog_add_button(GtkDialog raw, Utf8Buffer button_text, GtkResponseType response_id);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Cairo)]
            public delegate CairoSurface cairo_image_surface_create(int format, int width, int height);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Cairo)]
            public delegate CairoSurface cairo_image_surface_create_for_data(IntPtr data, int format, int width, int height, int stride);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Cairo)]
            public delegate IntPtr cairo_image_surface_get_data(CairoSurface surface);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Cairo)]
            public delegate int cairo_image_surface_get_stride(CairoSurface surface);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Cairo)]
            public delegate void cairo_surface_mark_dirty(CairoSurface surface);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Cairo)]
            public delegate void cairo_surface_write_to_png(CairoSurface surface, Utf8Buffer path);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Cairo)]
            public delegate void cairo_surface_flush(CairoSurface surface);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Cairo)]
            public delegate void cairo_surface_destroy(IntPtr surface);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Cairo)]
            public delegate void cairo_set_source_surface(IntPtr cr, CairoSurface surface, double x, double y);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Cairo)]
            public delegate void cairo_set_source_rgba(IntPtr cr, double r, double g, double b, double a);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Cairo)]
            public delegate void cairo_scale(IntPtr context, double sx, double sy);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Cairo)]
            public delegate void cairo_paint(IntPtr context);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Cairo)]
            public delegate void cairo_show_text(IntPtr context, Utf8Buffer text);   
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Cairo)]
            public delegate void cairo_set_font_size(IntPtr context, double size);  
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Cairo)]
            public delegate void cairo_select_font_face(IntPtr context, Utf8Buffer face, int slant, int weight);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Cairo)]
            public delegate void cairo_move_to(IntPtr context, double x, double y);   
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Cairo)]
            public delegate void cairo_destroy(IntPtr context);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_widget_queue_draw_area(GtkWidget widget, int x, int y, int width, int height);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate uint gtk_widget_add_tick_callback(GtkWidget widget, TickCallback callback, IntPtr userData, IntPtr destroy);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate uint gtk_widget_remove_tick_callback(GtkWidget widget, uint id);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate GtkImContext gtk_im_multicontext_new();

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate IntPtr gtk_im_context_set_client_window(GtkImContext context, IntPtr window);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate bool gtk_im_context_filter_keypress(GtkImContext context, IntPtr ev);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_widget_activate(GtkWidget widget);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gdk)]
            public delegate IntPtr gdk_screen_get_root_window(IntPtr screen);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gdk)]
            public delegate IntPtr gdk_cursor_new(GdkCursorType type);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gdk)]
            public delegate IntPtr gdk_window_get_pointer(IntPtr raw, out int x, out int y, out int mask);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gdk)]
            public delegate GdkWindowState gdk_window_get_state(IntPtr window);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_window_iconify(GtkWindow window);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_window_deiconify(GtkWindow window);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_window_maximize(GtkWindow window);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_window_unmaximize(GtkWindow window);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_window_close(GtkWindow window);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_window_set_keep_above(GtkWindow gtkWindow, bool setting);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_window_set_geometry_hints(GtkWindow window, IntPtr geometry_widget, ref GdkGeometry geometry, GdkWindowHints geom_mask);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gdk)]
            public delegate void gdk_window_invalidate_rect(IntPtr window, ref GdkRectangle rect, bool invalidate_children);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gdk)]
            public delegate void gdk_window_begin_move_drag(IntPtr window, gint button, gint root_x, gint root_y, guint32 timestamp);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gdk)]
            public delegate void gdk_window_begin_resize_drag(IntPtr window, WindowEdge edge, gint button, gint root_x, gint root_y, guint32 timestamp);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gdk)]
            public delegate void gdk_window_process_updates(IntPtr window, bool updateChildren);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gdk)]
            public delegate void gdk_window_begin_paint_rect(IntPtr window, ref GdkRectangle rect);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gdk)]
            public delegate void gdk_window_end_paint(IntPtr window);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gdk)]
            public delegate void gdk_event_request_motions(IntPtr ev);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate IntPtr gtk_clipboard_get_for_display(IntPtr display, IntPtr atom);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_clipboard_request_text(IntPtr clipboard, GtkClipboardTextReceivedFunc callback, IntPtr user_data);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_clipboard_set_text(IntPtr clipboard, Utf8Buffer text, int len);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate void gtk_clipboard_clear(IntPtr clipboard);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.GdkPixBuf)]
            public delegate IntPtr gdk_pixbuf_new_from_file(Utf8Buffer filename, out IntPtr error);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate IntPtr gtk_icon_theme_get_default();

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gtk)]
            public delegate IntPtr gtk_icon_theme_load_icon(IntPtr icon_theme, Utf8Buffer icon_name, gint size, int flags,out IntPtr error);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gdk)]
            public delegate IntPtr gdk_cursor_new_from_pixbuf(IntPtr disp, IntPtr pixbuf, int x, int y);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gdk)]
            public delegate IntPtr gdk_window_set_cursor(IntPtr window, IntPtr cursor);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.GdkPixBuf)]
            public delegate IntPtr gdk_pixbuf_new_from_stream(GInputStream stream, IntPtr cancel, out IntPtr error);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.GdkPixBuf)]
            public delegate bool gdk_pixbuf_save_to_bufferv(Pixbuf pixbuf, out IntPtr buffer, out IntPtr buffer_size,
                            Utf8Buffer type, IntPtr option_keys, IntPtr option_values, out IntPtr error);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gdk)]
            public delegate IntPtr gdk_cairo_create(IntPtr window);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gobject)]
            public delegate void g_object_unref(IntPtr instance);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gobject)]
            public delegate void g_object_ref(GObject instance);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gobject)]
            public delegate IntPtr g_type_name(IntPtr instance);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gobject)]
            public delegate ulong g_signal_connect_object(GObject instance, Utf8Buffer signal, IntPtr handler, IntPtr userData, int flags);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gobject)]
            public delegate ulong g_signal_handler_disconnect(GObject instance, ulong connectionId);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Glib)]
            public delegate ulong g_timeout_add(uint interval, timeout_callback callback, IntPtr data);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Glib)]
            public delegate ulong g_timeout_add_full(int prio, uint interval, timeout_callback callback, IntPtr data, IntPtr destroy);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Glib)]
            public delegate ulong g_free(IntPtr data);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gobject)]
            public delegate bool g_type_check_instance_is_fundamentally_a(IntPtr instance, IntPtr type);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Glib)]
            public unsafe delegate void g_slist_free(GSList* data);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl), GtkImport(GtkDll.Gio)]
            public delegate GInputStream g_memory_input_stream_new_from_data(IntPtr ptr, IntPtr len, IntPtr destroyCallback);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate bool signal_widget_draw(IntPtr gtkWidget, IntPtr cairoContext, IntPtr userData);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate bool signal_generic(IntPtr gtkWidget, IntPtr userData);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate bool signal_dialog_response(IntPtr gtkWidget, GtkResponseType response, IntPtr userData);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate bool signal_onevent(IntPtr gtkWidget, IntPtr ev, IntPtr userData);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void monitors_changed(IntPtr screen, IntPtr userData);
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate bool signal_commit(IntPtr gtkWidget, IntPtr utf8string, IntPtr userData);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate bool timeout_callback(IntPtr data);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void GtkClipboardTextReceivedFunc(IntPtr clipboard, IntPtr utf8string, IntPtr userdata);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate bool TickCallback(IntPtr widget, IntPtr clock, IntPtr userdata);


        }

        public static D.gdk_display_get_n_screens GdkDisplayGetNScreens;
        public static D.gdk_display_get_screen GdkDisplayGetScreen;
        public static D.gdk_display_get_default_screen GdkDisplayGetDefaultScreen;
        public static D.gdk_screen_get_n_monitors GdkScreenGetNMonitors;
        public static D.gdk_screen_get_primary_monitor GdkScreenGetPrimaryMonitor;
        public static D.gdk_screen_get_monitor_geometry GdkScreenGetMonitorGeometry;
        public static D.gdk_screen_get_monitor_workarea GdkScreenGetMonitorWorkarea;
        public static D.gtk_window_set_decorated GtkWindowSetDecorated;
        public static D.gtk_window_set_resizable GtkWindowSetResizable;
        public static D.gtk_window_set_skip_taskbar_hint GtkWindowSetSkipTaskbarHint;
        public static D.gtk_window_get_skip_taskbar_hint GtkWindowGetSkipTaskbarHint;
        public static D.gtk_window_set_skip_pager_hint GtkWindowSetSkipPagerHint;
        public static D.gtk_window_get_skip_pager_hint GtkWindowGetSkipPagerHint;
        public static D.gtk_window_set_title GtkWindowSetTitle;
        public static D.gtk_application_new GtkApplicationNew;
        public static D.gtk_main_iteration GtkMainIteration;
        public static D.gtk_window_new GtkWindowNew;
        public static D.gtk_window_set_icon GtkWindowSetIcon;
        public static D.gtk_window_set_modal GtkWindowSetModal;
        public static D.gdk_set_allowed_backends GdkSetAllowedBackends;
        public static D.gtk_init GtkInit;
        public static D.gtk_window_present GtkWindowPresent;
        public static D.gtk_widget_hide GtkWidgetHide;
        public static D.gtk_widget_show GtkWidgetShow;
        public static D.gdk_get_native_handle GetNativeGdkWindowHandle;
        public static D.gtk_widget_get_window GtkWidgetGetWindow;
        public static D.gtk_widget_get_scale_factor GtkWidgetGetScaleFactor;
        public static D.gtk_widget_get_screen GtkWidgetGetScreen;
        public static D.gtk_widget_realize GtkWidgetRealize;
        public static D.gtk_window_get_size GtkWindowGetSize;
        public static D.gtk_window_resize GtkWindowResize;
        public static D.gdk_window_resize GdkWindowResize;
        public static D.gdk_window_set_override_redirect GdkWindowSetOverrideRedirect;
        public static D.gtk_widget_set_size_request GtkWindowSetSizeRequest;
        public static D.gtk_window_set_default_size GtkWindowSetDefaultSize;
        public static D.gtk_window_set_geometry_hints GtkWindowSetGeometryHints;
        public static D.gtk_window_get_position GtkWindowGetPosition;
        public static D.gtk_window_move GtkWindowMove;
        public static D.gtk_file_chooser_dialog_new GtkFileChooserDialogNew;
        public static D.gtk_file_chooser_set_select_multiple GtkFileChooserSetSelectMultiple;
        public static D.gtk_file_chooser_set_filename GtkFileChooserSetFilename;
        public static D.gtk_file_chooser_get_filenames GtkFileChooserGetFilenames;
        public static D.gtk_dialog_add_button GtkDialogAddButton;
        public static D.g_object_unref GObjectUnref;
        public static D.g_object_ref GObjectRef;
        public static D.g_type_name GTypeName;
        public static D.g_signal_connect_object GSignalConnectObject;
        public static D.g_signal_handler_disconnect GSignalHandlerDisconnect;
        public static D.g_timeout_add GTimeoutAdd;
        public static D.g_timeout_add_full GTimeoutAddFull;
        public static D.g_free GFree;
        public static D.g_type_check_instance_is_fundamentally_a GTypeCheckInstanceIsFundamentallyA;
        public static D.g_slist_free GSlistFree;
        public static D.g_memory_input_stream_new_from_data GMemoryInputStreamNewFromData;
        public static D.gtk_widget_set_double_buffered GtkWidgetSetDoubleBuffered;
        public static D.gtk_widget_set_events GtkWidgetSetEvents;
        public static D.gdk_window_invalidate_rect GdkWindowInvalidateRect;
        public static D.gtk_widget_queue_draw_area GtkWidgetQueueDrawArea;
        public static D.gtk_widget_add_tick_callback GtkWidgetAddTickCallback;
        public static D.gtk_widget_remove_tick_callback GtkWidgetRemoveTickCallback;
        public static D.gtk_widget_activate GtkWidgetActivate;
        public static D.gtk_clipboard_get_for_display GtkClipboardGetForDisplay;
        public static D.gtk_clipboard_request_text GtkClipboardRequestText;
        public static D.gtk_clipboard_set_text GtkClipboardSetText;
        public static D.gtk_clipboard_clear GtkClipboardRequestClear;
        
        public static D.gtk_im_multicontext_new GtkImMulticontextNew;
        public static D.gtk_im_context_filter_keypress GtkImContextFilterKeypress;
        public static D.gtk_im_context_set_client_window GtkImContextSetClientWindow;

        public static D.gdk_screen_get_height GdkScreenGetHeight;
        public static D.gdk_display_get_default GdkGetDefaultDisplay;
        public static D.gdk_screen_get_width GdkScreenGetWidth;
        public static D.gdk_screen_get_root_window GdkScreenGetRootWindow;
        public static D.gdk_cursor_new GdkCursorNew;
        public static D.gdk_window_get_origin GdkWindowGetOrigin;
        public static D.gdk_window_get_pointer GdkWindowGetPointer;
        public static D.gdk_window_get_state GdkWindowGetState;
        public static D.gtk_window_iconify GtkWindowIconify;
        public static D.gtk_window_deiconify GtkWindowDeiconify;
        public static D.gtk_window_maximize GtkWindowMaximize;
        public static D.gtk_window_unmaximize GtkWindowUnmaximize;
        public static D.gtk_window_close GtkWindowClose;
        public static D.gtk_window_set_keep_above GtkWindowSetKeepAbove;
        public static D.gdk_window_begin_move_drag GdkWindowBeginMoveDrag;
        public static D.gdk_window_begin_resize_drag GdkWindowBeginResizeDrag;
        public static D.gdk_event_request_motions GdkEventRequestMotions;
        public static D.gdk_window_process_updates GdkWindowProcessUpdates;
        public static D.gdk_window_begin_paint_rect GdkWindowBeginPaintRect;
        public static D.gdk_window_end_paint GdkWindowEndPaint;
        

        public static D.gdk_pixbuf_new_from_file GdkPixbufNewFromFile;
        public static D.gtk_icon_theme_get_default GtkIconThemeGetDefault;
        public static D.gtk_icon_theme_load_icon GtkIconThemeLoadIcon;
        public static D.gdk_cursor_new_from_pixbuf GdkCursorNewFromPixbuf;
        public static D.gdk_window_set_cursor GdkWindowSetCursor;
        public static D.gdk_pixbuf_new_from_stream GdkPixbufNewFromStream;
        public static D.gdk_pixbuf_save_to_bufferv GdkPixbufSaveToBufferv;
        public static D.gdk_cairo_create GdkCairoCreate;
        
        public static D.cairo_image_surface_create CairoImageSurfaceCreate;
        public static D.cairo_image_surface_create_for_data CairoImageSurfaceCreateForData;
        public static D.cairo_image_surface_get_data CairoImageSurfaceGetData;
        public static D.cairo_image_surface_get_stride CairoImageSurfaceGetStride;
        public static D.cairo_surface_mark_dirty CairoSurfaceMarkDirty;
        public static D.cairo_surface_write_to_png CairoSurfaceWriteToPng;
        public static D.cairo_surface_flush CairoSurfaceFlush;
        public static D.cairo_surface_destroy CairoSurfaceDestroy;
        public static D.cairo_set_source_surface CairoSetSourceSurface;
        public static D.cairo_set_source_rgba CairoSetSourceRgba;
        public static D.cairo_scale CairoScale;
        public static D.cairo_paint CairoPaint;
        public static D.cairo_show_text CairoShowText;
        public static D.cairo_select_font_face CairoSelectFontFace;
        public static D.cairo_set_font_size CairoSetFontSize;
        public static D.cairo_move_to CairoMoveTo;
        public static D.cairo_destroy CairoDestroy;

        public const int G_TYPE_OBJECT = 80;
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

    enum GdkEventType
    {
        Nothing = -1,
        Delete = 0,
        Destroy = 1,
        Expose = 2,
        MotionNotify = 3,
        ButtonPress = 4,
        TwoButtonPress = 5,
        ThreeButtonPress = 6,
        ButtonRelease = 7,
        KeyPress = 8,
        KeyRelease = 9,
        EnterNotify = 10,
        LeaveNotify = 11,
        FocusChange = 12,
        Configure = 13,
        Map = 14,
        Unmap = 15,
        PropertyNotify = 16,
        SelectionClear = 17,
        SelectionRequest = 18,
        SelectionNotify = 19,
        ProximityIn = 20,
        ProximityOut = 21,
        DragEnter = 22,
        DragLeave = 23,
        DragMotion = 24,
        DragStatus = 25,
        DropStart = 26,
        DropFinished = 27,
        ClientEvent = 28,
        VisibilityNotify = 29,
        NoExpose = 30,
        Scroll = 31,
        WindowState = 32,
        Setting = 33,
        OwnerChange = 34,
        GrabBroken = 35,
    }

    enum GdkModifierType
    {
        ShiftMask = 1,
        LockMask = 2,
        ControlMask = 4,
        Mod1Mask = 8,
        Mod2Mask = 16,
        Mod3Mask = 32,
        Mod4Mask = 64,
        Mod5Mask = 128,
        Button1Mask = 256,
        Button2Mask = 512,
        Button3Mask = 1024,
        Button4Mask = 2048,
        Button5Mask = 4096,
        SuperMask = 67108864,
        HyperMask = 134217728,
        MetaMask = 268435456,
        ReleaseMask = 1073741824,
        ModifierMask = ReleaseMask | Button5Mask | Button4Mask | Button3Mask | Button2Mask | Button1Mask | Mod5Mask | Mod4Mask | Mod3Mask | Mod2Mask | Mod1Mask | ControlMask | LockMask | ShiftMask,
        None = 0,
    }

    enum GdkScrollDirection
    {
        Up,
        Down,
        Left,
        Right,
        Smooth
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct GdkEventButton
    {
        public GdkEventType type;
        public IntPtr window;
        public gint8 send_event;
        public guint32 time;
        public gdouble x;
        public gdouble y;
        public gdouble* axes;
        public GdkModifierType state;
        public guint button;
        public IntPtr device;
        public gdouble x_root, y_root;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct GdkEventMotion
    {
        public GdkEventType type;
        public IntPtr window;
        public gint8 send_event;
        public guint32 time;
        public gdouble x;
        public gdouble y;
        public gdouble* axes;
        public GdkModifierType state;
        public gint16 is_hint;
        public IntPtr device;
        public gdouble x_root, y_root;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe  struct GdkEventScroll
    {
        public GdkEventType type;
        public IntPtr window;
        public gint8 send_event;
        public guint32 time;
        public gdouble x;
        public gdouble y;
        public GdkModifierType state;
        public GdkScrollDirection direction;
        public IntPtr device;
        public gdouble x_root, y_root;
        public gdouble delta_x;
        public gdouble delta_y;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe  struct GdkEventCrossing 
    {
        public GdkEventType type;
        public IntPtr window;
        public gint8 send_event;
        public IntPtr subwindow;
        public guint32 time;
        public gdouble x;
        public gdouble y;
        public gdouble x_root;
        public gdouble y_root;
        public int mode;
        public int detail;
        public bool focus;
        public GdkModifierType state;
    };
    
    [StructLayout(LayoutKind.Sequential)]
    unsafe struct GdkEventWindowState
    {
        public GdkEventType type;
        public IntPtr window;
        gint8 send_event;
        public GdkWindowState changed_mask;
        public GdkWindowState new_window_state;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct GdkEventKey
    {
        public GdkEventType type;
        public IntPtr window;
        public gint8 send_event;
        public guint32 time;
        public guint state;
        public guint keyval;
        public gint length;
        public IntPtr pstring;
        public guint16 hardware_keycode;
        public byte group;
        public guint is_modifier;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct GSList
    {
        public IntPtr Data;
        public GSList* Next;
    }

    [Flags]
    public enum GdkWindowState
    {
        Withdrawn = 1,
        Iconified = 2,
        Maximized = 4,
        Sticky = 8,
        Fullscreen = 16,
        Above = 32,
        Below = 64,
        Focused = 128,
        Ttiled = 256
    }

    public enum GtkResponseType
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

    public enum GtkFileChooserAction
    {
        Open,
        Save,
        SelectFolder,
        CreateFolder,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct GdkGeometry
    {
        public gint min_width;
        public gint min_height;
        public gint max_width;
        public gint max_height;
        public gint base_width;
        public gint base_height;
        public gint width_inc;
        public gint height_inc;
        public gdouble min_aspect;
        public gdouble max_aspect;
        public gint win_gravity;
    }

    enum GdkWindowHints
    {
        GDK_HINT_POS = 1 << 0,
        GDK_HINT_MIN_SIZE = 1 << 1,
        GDK_HINT_MAX_SIZE = 1 << 2,
        GDK_HINT_BASE_SIZE = 1 << 3,
        GDK_HINT_ASPECT = 1 << 4,
        GDK_HINT_RESIZE_INC = 1 << 5,
        GDK_HINT_WIN_GRAVITY = 1 << 6,
        GDK_HINT_USER_POS = 1 << 7,
        GDK_HINT_USER_SIZE = 1 << 8
    }
}
