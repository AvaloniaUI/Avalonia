using System;
using System.Runtime.InteropServices;

namespace Avalonia.Wayland
{
    internal static class LibWaylandEgl
    {
        private const string WaylandEgl = "libwayland-egl.so.1";

        [DllImport(WaylandEgl)]
        internal static extern IntPtr wl_egl_window_create(IntPtr surface, int width, int height);

        [DllImport(WaylandEgl)]
        internal static extern IntPtr wl_egl_window_resize(IntPtr window, int width, int height, int dx, int dy);

        [DllImport(WaylandEgl)]
        internal static extern void wl_egl_window_destroy(IntPtr window);
    }
}
