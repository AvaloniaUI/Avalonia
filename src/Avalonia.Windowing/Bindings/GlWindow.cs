using System;
using System.Runtime.InteropServices;
using Avalonia.Gpu;

namespace Avalonia.Windowing.Bindings
{
    [StructLayout(LayoutKind.Sequential)]
    public struct WindowId
    {
        public long Value { get; set; }
    }

    /// <summary>
    /// A window and GL context pair.
    /// Due to platform specific quirks, the ordering of window and context creation must be controlled by winit.
    /// </summary>
    public class GlWindowWrapper : IWindowWrapper, IGpuContext
    {
        [DllImport("winit_wrapper")]
        private static extern IntPtr winit_gl_window_new(IntPtr eventsLoopHandle);

        [DllImport("winit_wrapper")]
        private static extern void winit_gl_window_destroy(IntPtr handle);

        [DllImport("winit_wrapper")]
        private static extern void winit_gl_window_set_title(IntPtr handle, string title);

        [DllImport("winit_wrapper")]
        private static extern void winit_gl_window_set_size(IntPtr handle, double width, double height);

        [DllImport("winit_wrapper")]
        private static extern void winit_gl_window_set_position(IntPtr handle, LogicalPosition position);

        [DllImport("winit_wrapper")]
        private static extern LogicalSize winit_gl_window_get_size(IntPtr handle);

        [DllImport("winit_wrapper")]
        private static extern double winit_gl_window_get_dpi_scale_factor(IntPtr handle);

        [DllImport("winit_wrapper")]
        private static extern LogicalPosition winit_gl_window_get_position(IntPtr handle);

        [DllImport("winit_wrapper")]
        private static extern void winit_gl_window_present(IntPtr handle);

        [DllImport("winit_wrapper")]
        private static extern void winit_gl_window_make_current(IntPtr handle);

        [DllImport("winit_wrapper")]
        private static extern void winit_gl_window_show(IntPtr handle);

        [DllImport("winit_wrapper")]
        private static extern void winit_gl_window_hide(IntPtr handle);

        [DllImport("winit_wrapper")]
        private static extern void winit_gl_window_set_decorations(IntPtr handle, int visible);

        [DllImport("winit_wrapper")]
        private static extern IntPtr winit_gl_window_get_proc_addr(IntPtr handle, string symbol);

        [DllImport("winit_wrapper")]
        private static extern WindowId winit_gl_window_get_id(IntPtr handle);

        [DllImport("winit_wrapper")]
        private static extern void winit_gl_window_resize_context(IntPtr handle, double width, double height);

        [DllImport("winit_wrapper")]
        private static extern IntPtr winit_gl_window_get_nsview(IntPtr handle);

        [DllImport("winit_wrapper")]
        private static extern void Test(IntPtr nsview);

        private IntPtr _handle;
        public WindowId Id => winit_gl_window_get_id(_handle);
        public EventsLoop EventsLoop { get;  }

        public GlWindowWrapper(EventsLoop eventsLoop)
        {
            EventsLoop = eventsLoop;
            _handle = winit_gl_window_new(EventsLoop.Handle);
        }

        private bool disposed = false;
        public void Dispose()
        {
            if (!disposed)
            {
                winit_gl_window_destroy(_handle);
                disposed = true;
            }
        }

        public IntPtr NSView => winit_gl_window_get_nsview(_handle);

        public void Test ()
        {
            Test(NSView);
        }

        public void SetTitle(string title)
        {
            Contract.Requires<InvalidOperationException>(disposed != true);
            winit_gl_window_set_title(_handle, title);
        }

        public void SetSize(double width, double height) 
        {
            Contract.Requires<InvalidOperationException>(disposed != true);
            winit_gl_window_set_size(_handle, width, height);   
        }

        public void SetPosition(double x, double y)
        {
            Contract.Requires<InvalidOperationException>(disposed != true);
            var position = new LogicalPosition { X = x, Y = y };
            winit_gl_window_set_position(_handle, position);
        }

        public void Present()
        {
            Contract.Requires<InvalidOperationException>(disposed != true);
            winit_gl_window_present(_handle);
        }

        public IntPtr GetProcAddress(string symbol)
        {
            Contract.Requires<InvalidOperationException>(disposed != true);
            return winit_gl_window_get_proc_addr(_handle, symbol);
        }

        public (double, double) GetSize() {
            Contract.Requires<InvalidOperationException>(disposed != true);
            var size = winit_gl_window_get_size(_handle);
            return (size.Width, size.Height);
        }

        public double GetScaleFactor ()
        {
            Contract.Requires<InvalidOperationException>(disposed != true);
            return winit_gl_window_get_dpi_scale_factor(_handle);
        }

        public (double, double) GetFramebufferSize()
        {
            return GetSize();
        }

        public (double, double) GetPosition()
        {
            Contract.Requires<InvalidOperationException>(disposed != true);
            var position = winit_gl_window_get_position(_handle);
            return (position.X, position.Y);
        }

        public void Show()
        {
            Contract.Requires<InvalidOperationException>(disposed != true);
            winit_gl_window_show(_handle);
        }

        public void ResizeContext(double width, double height)
        {
            Contract.Requires<InvalidOperationException>(disposed != true);
            winit_gl_window_resize_context(_handle, width, height);
        }

        public void ToggleDecorations(bool visible)
        {
            Contract.Requires<InvalidOperationException>(disposed != true);
            winit_gl_window_set_decorations(_handle, visible ? 1 : 0);
        }

        public void Hide()
        {
            Contract.Requires<InvalidOperationException>(disposed != true);
            winit_gl_window_hide(_handle);
        }

        public (double, double) GetDpi()
        {
            var scaleFactor = GetScaleFactor();
            return (scaleFactor, scaleFactor);
        }

        public void MakeCurrent()
        {
            winit_gl_window_make_current(_handle);
        }
    }
}
