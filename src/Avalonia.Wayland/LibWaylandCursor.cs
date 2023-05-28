using System;
using System.Runtime.InteropServices;

namespace Avalonia.Wayland
{
    internal static class LibWaylandCursor
    {
        private const string WaylandCursor = "libwayland-cursor.so.0";

        [DllImport(WaylandCursor)]
        internal static extern IntPtr wl_cursor_theme_load(string? name, int size, IntPtr shm);

        [DllImport(WaylandCursor)]
        internal static extern void wl_cursor_theme_destroy(IntPtr theme);

        [DllImport(WaylandCursor)]
        internal static extern unsafe wl_cursor* wl_cursor_theme_get_cursor(IntPtr theme, string? name);

        [DllImport(WaylandCursor)]
        internal static extern unsafe IntPtr wl_cursor_image_get_buffer(wl_cursor_image* image);

        [StructLayout(LayoutKind.Sequential)]
        internal struct wl_cursor_image
        {
            public uint width;
            public uint height;
            public uint hotspot_x;
            public uint hotspot_y;
            public uint delay;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct wl_cursor
        {
            public uint image_count;
            public wl_cursor_image** images;
            public char* name;
        }
    }
}
