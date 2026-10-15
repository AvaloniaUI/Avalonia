using System;
using System.Linq;

namespace Avalonia.X11;

using static XLib;
partial class X11Window
{
    public class DefaultTopLevelWindowMode : X11WindowMode
    {
        public override void Activate()
        {
            if (Platform.Globals.NetSupported?.Contains(X11.Atoms._NET_ACTIVE_WINDOW) == true)
            {
                Window.SendNetWMMessage(X11.Atoms._NET_ACTIVE_WINDOW, (IntPtr)1, X11.LastActivityTimestamp,
                    IntPtr.Zero);
            }
            else
            {
                XRaiseWindow(X11.Display, Handle);
                OnManualXRaiseWindow();
            }

            base.Activate();
        }

        protected virtual void OnManualXRaiseWindow()
        {
            
        }

        public override void Show(bool activate, bool isDialog)
        {
            base.Show(activate, isDialog);

            Window._wasMappedAtLeastOnce = true;

            if (!activate)
            {
                var time = IntPtr.Zero;
                XChangeProperty(X11.Display, Handle, X11.Atoms._NET_WM_USER_TIME, X11.Atoms.CARDINAL, 32,
                    PropertyMode.Replace, ref time, 1);
            }

            XMapWindow(X11.Display, Handle);
            XFlush(X11.Display);
        }

        public override void Hide()
        {
            XUnmapWindow(X11.Display, Handle);

            if (!Window._overrideRedirect)
                SendSyntheticUnmapNotify();

            base.Hide();
        }

        // ICCCM 4.1.4: withdrawing also requires a synthetic UnmapNotify, since XUnmapWindow
        // generates no event when the window manager has already unmapped the window.
        private void SendSyntheticUnmapNotify()
        {
            var ev = new XEvent
            {
                UnmapEvent =
                {
                    type = XEventName.UnmapNotify,
                    send_event = 1,
                    display = X11.Display,
                    xevent = X11.RootWindow,
                    window = Handle,
                    from_configure = 0
                }
            };
            XSendEvent(X11.Display, X11.RootWindow, false,
                (IntPtr)(EventMask.SubstructureRedirectMask | EventMask.SubstructureNotifyMask), ref ev);
        }

        public override Point PointToClient(PixelPoint point) => new Point(
            (point.X - (Window._position ?? default).X) / Window.RenderScaling,
            (point.Y - (Window._position ?? default).Y) / Window.RenderScaling);

        public override PixelPoint PointToScreen(Point point) => new PixelPoint(
            (int)(point.X * Window.RenderScaling + (Window._position ?? default).X),
            (int)(point.Y * Window.RenderScaling + (Window._position ?? default).Y));
    }
}
