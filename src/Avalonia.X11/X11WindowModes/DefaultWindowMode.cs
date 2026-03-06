using System;

namespace Avalonia.X11;

using static XLib;
partial class X11Window
{
    public class DefaultTopLevelWindowMode : X11WindowMode
    {
        public override void Activate()
        {
            if (X11.Atoms._NET_ACTIVE_WINDOW != IntPtr.Zero)
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
            Window._wasMappedAtLeastOnce = true;
            XMapWindow(X11.Display, Handle);
            XFlush(X11.Display);
            base.Show(activate, isDialog);
        }

        public override void Hide()
        {
            XUnmapWindow(X11.Display, Handle);
            base.Hide();
        }

        public override Point PointToClient(PixelPoint point) => new Point(
            (point.X - (Window._position ?? default).X) / Window.RenderScaling,
            (point.Y - (Window._position ?? default).Y) / Window.RenderScaling);

        public override PixelPoint PointToScreen(Point point) => new PixelPoint(
            (int)(point.X * Window.RenderScaling + (Window._position ?? default).X),
            (int)(point.Y * Window.RenderScaling + (Window._position ?? default).Y));
    }
}