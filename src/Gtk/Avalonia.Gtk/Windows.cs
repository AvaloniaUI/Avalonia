using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform;
using Gdk;
using Gtk;
using Window = Gtk.Window;
using WindowType = Gtk.WindowType;

namespace Avalonia.Gtk
{
    class PlatformHandleAwareWindow : Window, IPlatformHandle
    {
        public PlatformHandleAwareWindow(WindowType type) : base(type)
        {
            Events = EventMask.AllEventsMask;
        }
        
        IntPtr IPlatformHandle.Handle => GetNativeWindow();
        public string HandleDescriptor => "HWND";


        [DllImport("libgdk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr gdk_win32_drawable_get_handle(IntPtr gdkWindow);

        [DllImport("libgtk-x11-2.0.so.0", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr gdk_x11_drawable_get_xid(IntPtr gdkWindow);

        [DllImport("libgdk-quartz-2.0-0.dylib", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr gdk_quartz_window_get_nswindow(IntPtr gdkWindow);

        IntPtr _nativeWindow;

        IntPtr GetNativeWindow()
        {
            if (_nativeWindow != IntPtr.Zero)
                return _nativeWindow;
            return _nativeWindow = GetNativeWindow(GdkWindow);
        }

        public static IntPtr GetNativeWindow(Gdk.Window window)
        {
            IntPtr h = window.Handle;
            
            //Try whatever backend that works
            try
            {
                return gdk_quartz_window_get_nswindow(h);
            }
            catch
            {
            }
            try
            {
                return gdk_x11_drawable_get_xid(h);
            }
            catch
            {
            }
            return gdk_win32_drawable_get_handle(h);
        }

        protected override bool OnConfigureEvent(EventConfigure evnt)
        {
            base.OnConfigureEvent(evnt);
            return false;
        }
    }

    class PlatformHandleAwareDrawingArea : DrawingArea, IPlatformHandle
    {

        

        IntPtr IPlatformHandle.Handle => GetNativeWindow();
        public string HandleDescriptor => "HWND";
        IntPtr _nativeWindow;

        IntPtr GetNativeWindow()
        {
            
            if (_nativeWindow != IntPtr.Zero)
                return _nativeWindow;
            Realize();
            return _nativeWindow = PlatformHandleAwareWindow.GetNativeWindow(GdkWindow);
        }
    }
}
