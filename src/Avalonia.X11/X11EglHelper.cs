using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Logging;
using Avalonia.OpenGL.Egl;
using static Avalonia.OpenGL.Egl.EglConsts;
using static Avalonia.X11.XLib;

namespace Avalonia.X11
{
    internal static class X11EglHelper
    {
        /// <summary>
        /// Resolves the <see cref="XVisualInfo"/> that matches the EGL config's native visual id.
        /// nvidia's driver requires the X11 window visual to match the config used to create the surface,
        /// otherwise eglCreateWindowSurface fails. Returns null when the config doesn't advertise a visual id
        /// (e.g. mesa, where any visual works).
        /// </summary>
        public static XVisualInfo? GetVisualInfo(X11Info x11, EglInterface egl, IntPtr display, IntPtr config)
        {
            if (!egl.GetConfigAttrib(display, config, EGL_NATIVE_VISUAL_ID, out var visualId) || visualId == 0)
                return null;
            return XGetVisualInfoById(x11.DeferredDisplay, new IntPtr(visualId));
        }

        public static XVisualInfo? GetVisualInfo(X11Info x11, EglDisplay display) =>
            GetVisualInfo(x11, display.EglInterface, display.Handle, display.Config);

        /// <summary>
        /// Picks the EGL config to use out of the ones matched by eglChooseConfig. Each candidate is verified by
        /// creating a throwaway window with the config's native visual and attempting to create an EGL window
        /// surface on it: nvidia exposes multiple identical-looking configs where only a subset actually works,
        /// so the broken ones are discarded here. A 32-bit (transparent-capable) X11 visual is preferred,
        /// mirroring the GLX backend which selects a 32-bit visual; mesa lists those configs after the opaque
        /// ones. We resolve the (cheap) native visuals up-front and probe in preference order, so the expensive
        /// window-surface creation is attempted as few times as possible.
        /// </summary>
        public static IntPtr? ChooseConfig(X11Info x11, EglInterface egl, IntPtr display, IntPtr[] configs)
        {
            var candidates = new List<(IntPtr config, XVisualInfo visual)>(configs.Length);
            foreach (var config in configs)
            {
                if (GetVisualInfo(x11, egl, display, config) is { } visual)
                    candidates.Add((config, visual));
                else
                    Logger.TryGet(LogEventLevel.Verbose, "OpenGL")
                        ?.Log(null, "EGL config {Config} has no native visual id", config);
            }

            // OrderByDescending is stable, so configs keep eglChooseConfig's relative order within each group.
            foreach (var (config, visual) in candidates.OrderByDescending(c => c.visual.depth == 32))
            {
                if (ProbeConfig(x11, egl, display, config, visual))
                    return config;
            }

            return null;
        }

        private static bool ProbeConfig(X11Info x11, EglInterface egl, IntPtr display, IntPtr config,
            XVisualInfo vi)
        {
            var colormap = XCreateColormap(x11.DeferredDisplay, x11.RootWindow, vi.visual, 0);
            var attr = new XSetWindowAttributes
            {
                colormap = colormap,
                border_pixel = IntPtr.Zero
            };

            var window = XCreateWindow(x11.DeferredDisplay, x11.RootWindow, 0, 0, 1, 1, 0,
                (int)vi.depth, (int)CreateWindowArgs.InputOutput, vi.visual,
                new UIntPtr((uint)(SetWindowValuemask.ColorMap | SetWindowValuemask.BorderPixel)), ref attr);

            if (window == IntPtr.Zero)
            {
                XFreeColormap(x11.DeferredDisplay, colormap);
                return false;
            }

            XFlush(x11.DeferredDisplay);

            var surface = egl.CreateWindowSurface(display, config, window, new[] { EGL_NONE, EGL_NONE });
            var success = surface != IntPtr.Zero;
            if (success)
                egl.DestroySurface(display, surface);

            XDestroyWindow(x11.DeferredDisplay, window);
            XFreeColormap(x11.DeferredDisplay, colormap);

            Logger.TryGet(LogEventLevel.Verbose, "OpenGL")?.Log(null,
                "EGL config {Config}: visualId={VisualId} depth={Depth} usable={Usable}",
                config, vi.visualid, vi.depth, success);

            return success;
        }
    }
}
