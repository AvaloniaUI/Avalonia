using Perspex.Platform;
using System;
using System.Collections.Generic;
using System.Text;
using Perspex.Controls;
using Perspex.Input.Raw;
using Perspex.iOS.Rendering;
using UIKit;
using CoreGraphics;
using Foundation;
using Perspex.Input;
using CoreAnimation;
using ObjCRuntime;
using Serilog;

namespace Perspex.iOS
{
    public class WindowImpl : IWindowImpl
    {
        public IInputRoot InputRoot { get; private set; }

        private UIWindow _iosWindow;
        private UIViewController _viewController;

        public WindowImpl()
        {
            // create a new window instance based on the screen size
            _iosWindow = new UIWindow(UIScreen.MainScreen.NativeBounds);

            // create client view wrapper
            var clientView = new iOSHostView(UIScreen.MainScreen.Bounds, this);
            _viewController = new UIViewController();
            _viewController.View.BackgroundColor = UIColor.Red;
            _viewController.View.AddSubview(clientView);
            //_iosWindow.RootViewController = _viewController;

            Handle = new PlatformHandle(_iosWindow.Handle, "UIWindow");
        }

        public Action Activated { get; set; }
        public Action Closed { get; set; }
        public Action Deactivated { get; set; }

        public Action<RawInputEventArgs> Input { get; set; }
        public Action<Rect> Paint { get; set; }
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
            // TODO: This is not generating a draw call for some reason, so for now
            // I haved added a call to this in the input handlers below
            this._viewController.View.SetNeedsDisplayInRect(rect.ToCoreGraphics());
            //Log.Information("Invalidate: " + rect.ToString());
        }

        public Point PointToScreen(Point point)
        {
            throw new NotImplementedException();
        }

        public void SetCursor(IPlatformHandle cursor)
        {
            // noop on iOS
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
            InputRoot = inputRoot;
        }

        public void SetTitle(string title)
        {
            // noop on iOS
        }

        public void Show()
        {
            // TODO: once we have multiple windows instances this may not be good!!

            // make the window visible
            _iosWindow.MakeKeyAndVisible();

            // In iOS 8+ we need to set the root view controller *after* Window MakeKey
            // This ensures that the viewController's supported interface orientations
            // will be respected at launch
            _iosWindow.RootViewController = _viewController;

            _viewController.View.BecomeFirstResponder();
        }

        public IDisposable ShowDialog()
        {
            throw new NotImplementedException();
        }
    }

    public class iOSHostView : UIView
    {
        WindowImpl _WindowImpl;

        public iOSHostView(CGRect frame, WindowImpl impl) : base(frame)
        {
            _WindowImpl = impl;
        }

        public override void Draw(CGRect rect)
        {
            base.Draw(rect);

            //Log.Information("Draw: " + rect.ToString());

            // Test code - leave in for now until iOS is fully working
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
            if (_WindowImpl != null)
            {
                _WindowImpl.Paint(new Rect(rect.Left, rect.Top, rect.Width, rect.Height));
            }
        }

        public override void TouchesBegan(NSSet touches, UIEvent evt)
        {
            base.TouchesBegan(touches, evt);

            UITouch touch = touches.AnyObject as UITouch;
            if (touch != null)
            {
                var location = touch.LocationInView(this).ToPerspex();

                //Log.Information("TouchesBegan: " + location.ToString());

                if (_WindowImpl != null)
                {
                    // TODO: we have to send down a Move first so that Enter is triggered properly
                    // until we have proper support for Touch input
                    var me = new RawMouseEventArgs(
                        iOSMouseDevice.Instance,
                        (uint)touch.Timestamp, // TODO: not sure about this cast
                        _WindowImpl.InputRoot,
                        RawMouseEventType.Move,
                        location,
                        InputModifiers.None);

                    _WindowImpl.Input(me);

                    var e = new RawMouseEventArgs(
                        iOSMouseDevice.Instance,
                        (uint) touch.Timestamp, // TODO: not sure about this cast
                        _WindowImpl.InputRoot,
                        RawMouseEventType.LeftButtonDown,
                        location,
                        InputModifiers.None);

                    _WindowImpl.Input(e);
                }

                // for some reason this fails when I do it from a Perspex driven callstack. For now
                // so this here until we can figure out why
                this.SetNeedsDisplay();
            }
        }

        public override void TouchesEnded(NSSet touches, UIEvent evt)
        {
            base.TouchesEnded(touches, evt);

            UITouch touch = touches.AnyObject as UITouch;
            if (touch != null)
            {
                var location = touch.LocationInView(this).ToPerspex();

                //Log.Information("TouchesEnded: " + location.ToString());

                if (_WindowImpl != null)
                {
                    var e = new RawMouseEventArgs(
                        iOSMouseDevice.Instance,
                        (uint)touch.Timestamp, // TODO: not sure about this cast
                        _WindowImpl.InputRoot,
                        RawMouseEventType.LeftButtonUp,
                        location,
                        InputModifiers.None);

                    _WindowImpl.Input(e);
                }

                // for some reason this fails when I do it from a Perspex driven callstack. For now
                // so this here until we can figure out why
                this.SetNeedsDisplay();
            }
        }

        public override void TouchesMoved(NSSet touches, UIEvent evt)
        {
            base.TouchesMoved(touches, evt);

            UITouch touch = touches.AnyObject as UITouch;
            if (touch != null)
            {
                var location = touch.LocationInView(this).ToPerspex();

                //Log.Information("TouchesMoved: " + location.ToString());

                if (_WindowImpl != null)
                {
                    var e = new RawMouseEventArgs(
                        iOSMouseDevice.Instance,
                        (uint)touch.Timestamp, // TODO: not sure about this cast
                        _WindowImpl.InputRoot,
                        RawMouseEventType.Move,
                        location,
                        InputModifiers.None);

                    _WindowImpl.Input(e);
                }

                // for some reason this fails when I do it from a Perspex driven callstack. For now
                // so this here until we can figure out why
                this.SetNeedsDisplay();
            }
        }

        public override void TouchesCancelled(NSSet touches, UIEvent evt)
        {
            base.TouchesCancelled(touches, evt);

            // TODO: what to do here??
        }
    }
}
