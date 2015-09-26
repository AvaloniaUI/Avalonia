using Perspex.Platform;
using System;
using System.Collections.Generic;
using System.Text;
using Perspex.Controls;
using Perspex.Input.Raw;
using UIKit;
using CoreGraphics;

namespace Perspex.iOS
{
    public class WindowImpl : IWindowImpl
    {
        private TopLevel _owner;
        private UIWindow _iosWindow;

        public WindowImpl()
        {
            // create a new window instance based on the screen size
            _iosWindow = new UIWindow(UIScreen.MainScreen.Bounds);

            // test ui
            var controller = new UIViewController();
            controller.View.BackgroundColor = UIColor.Red;
            controller.View.Add(new iOSHostView(UIScreen.MainScreen.Bounds, this));
            _iosWindow.RootViewController = controller;

            Handle = new PlatformHandle(_iosWindow.Handle, "UIWindow");
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
                return new Size(_iosWindow.Bounds.Width, _iosWindow.Bounds.Height);
            }

            // Can we allow changing this on iOS?
            set { }
        }

        public Size MaxClientSize
        {
            get
            {
                // TODO: This should take into account things such as taskbar and window border thickness etc.
                return new Size(_iosWindow.Bounds.Width, _iosWindow.Bounds.Height);
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
            // TODO: once we have multiple windows instances this will not be good!!

            // make the window visible
            _iosWindow.MakeKeyAndVisible();
        }

        public IDisposable ShowDialog()
        {
            throw new NotImplementedException();
        }
    }

    internal class iOSHostView : UIView
    {
        WindowImpl _WindowImpl;

        public iOSHostView(CGRect frame, WindowImpl impl) : base(frame)
        {
            _WindowImpl = impl;
        }

        public override void Draw(CGRect rect)
        {
            base.Draw(rect);

			// Test code
            //get graphics context
            //using (var g = UIGraphics.GetCurrentContext())
            //{
            //    // set up drawing attributes
            //    g.SetLineWidth(10.0f);
            //    UIColor.Green.SetFill();
            //    UIColor.Blue.SetStroke();

            //    // create geometry
            //    var path = new CGPath();
            //    path.AddArc(Bounds.GetMidX(), Bounds.GetMidY(), 50f, 0, 2.0f * (float)Math.PI, true);

            //    // add geometry to graphics context and draw
            //    g.AddPath(path);
            //    g.DrawPath(CGPathDrawingMode.FillStroke);
            //}

            // call into Perspex rendering
            _WindowImpl.Paint(new Rect(rect.Left, rect.Top, rect.Width, rect.Height), _WindowImpl.Handle);
        }
    }
}
