using System;
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
    /// <see href="https://gitlab.gnome.org/GNOME/gtk/-/blob/3.22.30/gdk/x11/gdkwindow-x11.c#L823" />
    internal class X11FocusProxy
    {
        private const int InvisibleBorder = 0;
        private const int DepthCopyFromParent = 0;
        private readonly IntPtr _visualCopyFromParent = IntPtr.Zero;
        private readonly (int X, int Y) _outOfScreen = (-1, -1);
        private readonly (int Width, int Height) _smallest = (1, 1);

        internal IntPtr _handle;
        private readonly AvaloniaX11Platform _platform;
        private readonly X11PlatformThreading.EventHandler _ownerEventHandler;

        /// <summary>
        ///     Initializes instance and creates the underlying X window.
        /// </summary>
        /// 
        /// <param name="platform">The X11 platform.</param>
        /// <param name="parent">The parent window to proxy the focus for.</param>
        /// <param name="eventHandler">An event handler that will handle X events that come to the proxy.</param>
        internal X11FocusProxy(AvaloniaX11Platform platform, IntPtr parent,
            X11PlatformThreading.EventHandler eventHandler)
        {
            _handle = PrepareXWindow(platform.Info.Display, parent);
            _platform = platform;
            _ownerEventHandler = eventHandler;
            _platform.Windows[_handle] = OnEvent;
        }

        internal void Cleanup()
        {
            if (_handle != IntPtr.Zero)
            {
                _platform.Windows.Remove(_handle);
                _handle = IntPtr.Zero;
            }
        }

        private void OnEvent(ref XEvent ev)
        {
            if (ev.type is XEventName.FocusIn or XEventName.FocusOut)
            {
                this._ownerEventHandler(ref ev);
            }

            if (ev.type is XEventName.KeyPress or XEventName.KeyRelease)
            {
                this._ownerEventHandler(ref ev);
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
                _outOfScreen.X, _outOfScreen.Y,
                _smallest.Width, _smallest.Height,
                InvisibleBorder,
                DepthCopyFromParent,
                (int)CreateWindowArgs.InputOutput,
                _visualCopyFromParent,
                new UIntPtr((uint)valueMask),
                ref attrs);
            XMapWindow(display, handle);
            XSelectInput(display, handle, new IntPtr((uint)valueMask));
            return handle;
        }
    }
}
