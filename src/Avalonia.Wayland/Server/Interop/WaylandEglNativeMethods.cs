using System;
using System.Runtime.InteropServices;

namespace Avalonia.Wayland.Server.Interop;

internal static class WaylandEglNativeMethods
{
    private const string LibWaylandEgl = "libwayland-egl.so.1";

    /// <summary>
    /// Allocates a <c>wl_egl_window</c> wrapper around <paramref name="surface"/>
    /// (a <c>wl_surface*</c>) at the given pixel size. The returned handle is
    /// passed to <c>eglCreateWindowSurface</c> as the native window argument and
    /// is owned by the caller.
    /// </summary>
    [DllImport(LibWaylandEgl)]
    public static extern IntPtr wl_egl_window_create(IntPtr surface, int width, int height);

    [DllImport(LibWaylandEgl)]
    public static extern void wl_egl_window_destroy(IntPtr eglWindow);

    /// <summary>
    /// Resizes the <c>wl_egl_window</c>. The driver picks the new size up on the
    /// next <c>eglSwapBuffers</c>; <paramref name="dx"/>/<paramref name="dy"/>
    /// are reserved for sub-surface positioning and are 0 for our toplevel use.
    /// </summary>
    [DllImport(LibWaylandEgl)]
    public static extern void wl_egl_window_resize(IntPtr eglWindow, int width, int height, int dx, int dy);
}
