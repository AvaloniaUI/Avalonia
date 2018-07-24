using System;
using System.Runtime.InteropServices;
using Avalonia.Gpu;

namespace Avalonia.Windowing.Bindings
{
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
        private static extern LogicalSize winit_gl_window_get_size(IntPtr handle);

        [DllImport("winit_wrapper")]
        private static extern LogicalPosition winit_gl_window_get_position(IntPtr handle);

        [DllImport("winit_wrapper")]
        private static extern void winit_gl_window_present(IntPtr handle);

        [DllImport("winit_wrapper")]
        private static extern void winit_gl_window_show(IntPtr handle);

        [DllImport("winit_wrapper")]
        private static extern void winit_gl_window_hide(IntPtr handle);

        [DllImport("winit_wrapper")]
        private static extern void winit_gl_window_set_decorations(IntPtr handle, int visible);

        [DllImport("winit_wrapper")]
        private static extern IntPtr winit_gl_window_get_proc_addr(IntPtr handle, string symbol);

        [DllImport("winit_wrapper")]
        private static extern IntPtr winit_gl_window_get_id(IntPtr handle);

        [DllImport("winit_wrapper")]
        private static extern void winit_gl_window_resize_context(IntPtr handle, double width, double height);

        private IntPtr _handle;
        public IntPtr Id => winit_gl_window_get_id(_handle);
        public EventsLoop EventsLoop { get;  }

        public GlWindowWrapper(EventsLoop eventsLoop)
        {
            EventsLoop = eventsLoop;
            _handle = winit_gl_window_new(EventsLoop.Handle);
        }

        public void Dispose()
        {
            winit_gl_window_destroy(_handle);
        }

        public void SetTitle(string title)
        {
            winit_gl_window_set_title(_handle, title);
        }

        public void SetSize(double width, double height) 
        {
            winit_gl_window_set_size(_handle, width, height);   
        }

        public void Present()
        {
            winit_gl_window_present(_handle);
        }

        public IntPtr GetProcAddress(string symbol)
        {
            return winit_gl_window_get_proc_addr(_handle, symbol);
        }

        public (double, double) GetSize() {
            var size = winit_gl_window_get_size(_handle);
            return (size.Width, size.Height);
        }

        public (double, double) GetFramebufferSize()
        {
            return GetSize();
        }

        public (double, double) GetPosition()
        {
            var position = winit_gl_window_get_position(_handle);
            return (position.X, position.Y);
        }

        public void Show()
        {
            winit_gl_window_show(_handle);
        }

        public void ResizeContext(double width, double height)
        {
            winit_gl_window_resize_context(_handle, width, height);
        }

        public void ToggleDecorations(bool visible)
        {
            winit_gl_window_set_decorations(_handle, visible ? 1 : 0);
        }

        public void Hide()
        {
            winit_gl_window_hide(_handle);
        }
    }
}
