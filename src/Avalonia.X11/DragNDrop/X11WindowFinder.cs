using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace Avalonia.X11
{
    /// <summary>
    /// Helper class for searching and processing window under cursor.
    /// </summary>
    internal class X11WindowFinder
    {
        private readonly IntPtr _display;
        private readonly X11Atoms _atoms;

        public X11WindowFinder(IntPtr display, X11Atoms atoms)
        {
            _display = display;
            _atoms = atoms;
        }

        public unsafe IntPtr FindTopWindowUnderCursor(IntPtr root, out int rootX, out int rootY)
        {
            if (XLib.XQueryPointer(_display, root, out _, out _,
                                    out rootX, out rootY, out _, out _, out _))
            {
                return FindTopWindowUnderCursor(root, rootX, rootY);
            }

            return IntPtr.Zero;
        }

        public unsafe IntPtr FindTopWindowUnderCursor(IntPtr root, int cursorX, int cursorY)
        {
            var windowsChildren = GetWindowChildren(root);

            IntPtr target = IntPtr.Zero;
            var targets = new List<IntPtr>();

            for (int i = windowsChildren.Count - 1; i >= 0; i--)
            {
                IntPtr child = windowsChildren[i];
                XWindowAttributes attrs = new();
                if (XLib.XGetWindowAttributes(_display, child, ref attrs) != 0)
                {
                    if (attrs.map_state != MapState.IsViewable)
                        continue;

                    if (cursorX >= attrs.x && cursorX < attrs.x + attrs.width &&
                        cursorY >= attrs.y && cursorY < attrs.y + attrs.height )
                    {
                        targets.Add(child);
                       //// break;
                    }
                }
            }

            return targets.FirstOrDefault();
        }

        private unsafe List<IntPtr> GetWindowChildren(IntPtr window, bool recursive = true)
        {
            var windowsChildren = new List<IntPtr>();

            int errcode = XLib.XQueryTree(_display, window, out _, out _, out IntPtr childrenPtr, out int childrenCount);

            if (errcode != 0)
            {
                if (childrenPtr != IntPtr.Zero)
                {
                    var children = (IntPtr*)childrenPtr;
                    for (var i = 0; i < childrenCount; i++)
                    {
                        windowsChildren.Add(children[i]);
                        if(recursive)
                        {
                            windowsChildren.AddRange(GetWindowChildren(children[i]));
                        }
                    }
                    XLib.XFree(childrenPtr);
                }
            }

            errcode = 0;

            return windowsChildren;
        }

        public unsafe IntPtr FindRealWindow(IntPtr window, int x, int y, int maxDepth, bool ignoreNonXdndAware)
        {
            if (maxDepth <= 0)
                return IntPtr.Zero;

            XWindowAttributes attrs = new();
            if (XLib.XGetWindowAttributes(_display, window, ref attrs) != 0)
            {
                if (attrs.map_state != MapState.IsViewable)
                    return IntPtr.Zero;

                if (!XLib.XGetGeometry(_display, window, out _, out var wx, out var wy,
                      out var width, out var height, out _, out _))
                    return IntPtr.Zero;

                var windowRect = new Rect(wx, wy, width, height);
                if (windowRect.Contains(new Point(x, y)))
                {
                    if (ignoreNonXdndAware || CheckXdndSupport(window))
                    {
                        return window;
                    }
                }
            }

            var windowsChildren = GetWindowChildren( window);

            if (windowsChildren.Count > 0)
            {
                for (int i = 0; i < windowsChildren.Count; i++)
                {
                    IntPtr found = FindRealWindow(windowsChildren[i], x - attrs.x, y - attrs.y, maxDepth - 1, ignoreNonXdndAware);
                    if (found != IntPtr.Zero)
                        return found;
                }
            }

            return IntPtr.Zero;
        }

        public IntPtr FindXdndProxy(IntPtr window)
        {
            IntPtr proxy = IntPtr.Zero;

            if (XLib.XGetWindowProperty(_display, window, _atoms.XdndProxy,
                                       IntPtr.Zero, new IntPtr(1), false, _atoms.XA_WINDOW,
                                       out _, out _, out _, out _, out var data) != 0 
                                       && data != IntPtr.Zero)
            {
                try
                {
                    proxy = Marshal.ReadIntPtr(data);
                }
                finally
                {
                    XLib.XFree(data);
                }
            }

            if (proxy == IntPtr.Zero)
                return proxy;

            if (XLib.XGetWindowProperty(_display, proxy, _atoms.XdndProxy,
                                       IntPtr.Zero, new IntPtr(1), false, _atoms.XA_WINDOW,
                                       out _, out _, out _, out _, out data) != 0 
                                       && data != IntPtr.Zero)
            {
                try
                {
                    var p = Marshal.ReadIntPtr(data);
                    if (proxy != p)
                        proxy = IntPtr.Zero;
                }
                finally
                {
                    XLib.XFree(data);
                }
            }
            else
            {
                proxy = IntPtr.Zero;
            }

            return proxy;
        }

        public IntPtr FindXdndAwareParent(IntPtr window)
        {
            while (window != IntPtr.Zero)
            {
                if (CheckXdndSupport(window))
                    return window;

                if (XLib.XQueryTree(_display, window, out var root, out var parent, out _, out _) == 0)
                    break;

                if (window == root)
                    break;

                window = parent;
            }
            return IntPtr.Zero;
        }

        public bool CheckXdndSupport(IntPtr window)
        {

            int res = XLib.XGetWindowProperty(_display, window, _atoms.XdndAware, IntPtr.Zero,
                                    new IntPtr(1), false, _atoms.XA_ATOM,
                                    out var actualType, out var actualFormat,
                                    out var nitems, out var bytesAfter,
                                    out var data);

            string msg = res + "|" + data + "|" + nitems;
            Debug.Write(msg);

            if (res == 0 && data != IntPtr.Zero)
            {
                try
                {
                    int version = Marshal.ReadInt32(data);
                    return version >= 1;
                }
                finally
                {
                    XLib.XFree(data);
                }
            }

            return false;
        }
    }
}
