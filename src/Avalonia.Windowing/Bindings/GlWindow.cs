using System;
using System.Runtime.InteropServices;

namespace Avalonia.Windowing.Bindings
{
    /// <summary>
    /// A window and GL context pair.
    /// Due to platform specific quirks, the ordering of window and context creation must be controlled by winit.
    /// </summary>
    public class GlWindowWrapper : IWindowWrapper
    {
        [DllImport("winit_wrapper")]
        private static extern IntPtr winit_gl_window_new(IntPtr eventsLoopHandle);

        [DllImport("winit_wrapper")]
        private static extern IntPtr winit_gl_window_destroy(IntPtr handle);

        [DllImport("winit_wrapper")]
        private static extern IntPtr winit_gl_window_set_title(IntPtr handle, string title);

        private IntPtr _handle;
        public GlWindowWrapper(EventsLoop eventsLoop)
        {
            _handle = winit_gl_window_new(eventsLoop.Handle);
        }

        public void Dispose()
        {
            winit_gl_window_destroy(_handle);
        }

        public void SetTitle(string title)
        {
            winit_gl_window_set_title(_handle, title);
        }
    }
}
