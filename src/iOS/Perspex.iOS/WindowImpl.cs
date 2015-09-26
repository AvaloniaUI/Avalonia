using Perspex.Platform;
using System;
using System.Collections.Generic;
using System.Text;
using Perspex.Controls;
using Perspex.Input.Raw;
using UIKit;

namespace Perspex.iOS
{
    public class WindowImpl : IWindowImpl
    {
        private TopLevel _owner;
        static private UIWindow _iosRootWindow;

        public WindowImpl()
        {
            Handle = new PlatformHandle(_iosRootWindow.Handle, "UIWindow");
        }

        public static void SetHostUIWindow(UIWindow wnd)
        {
            _iosRootWindow = wnd;
        }

        public Action Activated { get; set; }
        public Action Closed { get; set; }
        public Action Deactivated { get; set; }

        public Action<RawInputEventArgs> Input { get; set; }
        public Action<Rect, IPlatformHandle> Paint { get; set; }
        public Action<Size> Resized { get; set; }

        // Can we allow changing this on iOS?
        public Size ClientSize
        {
            get
            {
                // TODO: This should take into account things such as taskbar and window border thickness etc.
                return new Size(_iosRootWindow.Bounds.Width, _iosRootWindow.Bounds.Height);
            }

            // Can we allow changing this on iOS?
            set { }
        }

        public Size MaxClientSize
        {
            get
            {
                // TODO: This should take into account things such as taskbar and window border thickness etc.
                return new Size(_iosRootWindow.Bounds.Width, _iosRootWindow.Bounds.Height);
            }
        }

        public IPlatformHandle Handle
        {
            get;
            private set;
        }

        public void Activate()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Hide()
        {
            throw new NotImplementedException();
        }

        public void Invalidate(Rect rect)
        {
            throw new NotImplementedException();
        }

        public Point PointToScreen(Point point)
        {
            throw new NotImplementedException();
        }

        public void SetCursor(IPlatformHandle cursor)
        {
            // noop on iOS
        }

        public void SetOwner(TopLevel owner)
        {
            _owner = owner;
        }

        public void SetTitle(string title)
        {
            // noop on iOS
        }

        public void Show()
        {
            throw new NotImplementedException();
        }

        public IDisposable ShowDialog()
        {
            throw new NotImplementedException();
        }
    }
}
