using System;
using System.Diagnostics;
using static Avalonia.X11.XLib;

namespace Avalonia.X11
{
    /// <summary>
    ///     An invisible X window that owns the input focus and forwards events to the owner window. 
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is a known Linux technique for an auxiliary invisible window to hold the input focus 
    ///         for the main window. It is required by XEmbed protocol, but it also works for regular cases 
    ///         that don't imply embedded windows.
    ///     </para>
    /// </remarks>
    /// 
    /// <see href="https://specifications.freedesktop.org/xembed-spec/xembed-spec-latest.html" />
    /// <see href="https://gitlab.gnome.org/GNOME/gtk/-/blob/3.22.30/gdk/x11/gdkwindow-x11.c#L823 />
    internal class X11FocusProxy
    {
        private const int InvisibleBorder = 0;
        private const int DepthCopyFromParent = 0;
        private readonly IntPtr VisualCopyFromParent = IntPtr.Zero;
        private readonly (int X, int Y) OutOfScreen = (-1, -1);
        private readonly (int Width, int Height) Smallest = (1, 1);

        internal readonly IntPtr handle;
        private readonly X11PlatformThreading.EventHandler ownerEventHandler;

        /// <summary>
        ///     Initializes instance and creates the underlying X window.
        /// </summary>
        /// 
        /// <param name="platform">The X11 platform.</param>
        /// <param name="parent">The parent window to proxy the focus for.</param>
        /// <param name="eventHandler">An event handler that will handle X events that come to the proxy.</param>
        public X11FocusProxy(AvaloniaX11Platform platform, IntPtr parent, X11PlatformThreading.EventHandler eventHandler)
        {
            handle = PrepareXWindow(platform.Info.Display, parent);
            ownerEventHandler = eventHandler;
            platform.Windows[this.handle] = OnEvent;
        }

        private void OnEvent(ref XEvent ev)
        {
            if (ev.type == XEventName.FocusIn || ev.type == XEventName.FocusOut)
            {
                this.ownerEventHandler(ref ev);
            }
            if (ev.type == XEventName.KeyPress || ev.type == XEventName.KeyRelease)
            {
                this.ownerEventHandler(ref ev);
            }
        }

        private IntPtr PrepareXWindow(IntPtr display, IntPtr parent)
        {
            var valueMask = default(EventMask)
                            | EventMask.FocusChangeMask
                            | EventMask.KeyPressMask
                            | EventMask.KeyReleaseMask;
            var attrs = new XSetWindowAttributes();
            var handle = XCreateWindow(display, parent,
                                 OutOfScreen.X, OutOfScreen.Y,
                                 Smallest.Width, Smallest.Height,
                                 InvisibleBorder,
                                 DepthCopyFromParent,
                                 (int)CreateWindowArgs.InputOutput,
                                 VisualCopyFromParent,
                                 new UIntPtr((uint)valueMask),
                                 ref attrs);
            XMapWindow(display, handle);
            XSelectInput(display, handle, new IntPtr((uint)valueMask));
            return handle;
        }
    }
}
