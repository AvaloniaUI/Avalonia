using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Skia.iOS;
using UIKit;
using Avalonia.iOS.Specific;
using ObjCRuntime;
using Avalonia.Controls;

namespace Avalonia.iOS
{
    [Adopts("UIKeyInput")]
    class AvaloniaView : SkiaView, IWindowImpl
    {
        private readonly UIWindow _window;
        private readonly UIViewController _controller;
        private IInputRoot _inputRoot;
        private readonly KeyboardEventsHelper<AvaloniaView> _keyboardHelper;
        private Point _position;

        public AvaloniaView(UIWindow window, UIViewController controller) : base(onFrame => PlatformThreadingInterface.Instance.Render = onFrame)
        {
            if (controller == null) throw new ArgumentNullException(nameof(controller));
            _window = window;
            _controller = controller;
            _keyboardHelper = new KeyboardEventsHelper<AvaloniaView>(this);
            AutoresizingMask = UIViewAutoresizing.All;
            AutoFit();
            UIApplication.Notifications.ObserveDidChangeStatusBarOrientation(delegate { AutoFit(); });
            UIApplication.Notifications.ObserveDidChangeStatusBarFrame(delegate { AutoFit(); });
        }

        [Export("hasText")]
        bool HasText => _keyboardHelper.HasText();

        [Export("insertText:")]
        void InsertText(string text) => _keyboardHelper.InsertText(text);

        [Export("deleteBackward")]
        void DeleteBackward() => _keyboardHelper.DeleteBackward();

        public override bool CanBecomeFirstResponder => _keyboardHelper.CanBecomeFirstResponder();

        void AutoFit()
        {
            var needFlip = !UIDevice.CurrentDevice.CheckSystemVersion(8, 0) &&
                           (_controller.InterfaceOrientation == UIInterfaceOrientation.LandscapeLeft
                            || _controller.InterfaceOrientation == UIInterfaceOrientation.LandscapeRight);

            // Bounds here (if top level) needs to correspond with the rendertarget 
            var frame = UIScreen.MainScreen.Bounds;
            if (needFlip)
                Frame = new CGRect(frame.Y, frame.X, frame.Height, frame.Width);
            else
                Frame = frame;
        }

        public Action Activated { get; set; }
        public Action Closed { get; set; }
        public Action Deactivated { get; set; }
        public Action<RawInputEventArgs> Input { get; set; }
        public Action<Rect> Paint { get; set; }
        public Action<Size> Resized { get; set; }
        public Action<double> ScalingChanged { get; set; }
        public Action<Point> PositionChanged { get; set; }

        public IPlatformHandle Handle => AvaloniaPlatformHandle;

        public double Scaling
        {
            get
            {
                // This does not appear to make any difference, but on iOS we
                // have Retina (x2) and we probably want this eventually
                return 1;   //UIScreen.MainScreen.Scale;
            }
        }

        public WindowState WindowState
        {
            get { return WindowState.Normal; }
            set { }
        }

        public override void LayoutSubviews() => Resized?.Invoke(ClientSize);

        public Size ClientSize
        {
            get { return Bounds.Size.ToAvalonia(); }
            set { Resized?.Invoke(ClientSize); }
        }

        public void Activate()
        {
        }

        protected override void Draw()
        {
            Paint?.Invoke(new Rect(new Point(), ClientSize));
        }

        public void Invalidate(Rect rect) => DrawOnNextFrame();

        public void SetInputRoot(IInputRoot inputRoot) => _inputRoot = inputRoot;

        public Point PointToClient(Point point) => point;

        public Point PointToScreen(Point point) => point;

        public void SetCursor(IPlatformHandle cursor)
        {
            //Not supported
        }

        public void Show()
        {
            _keyboardHelper.ActivateAutoShowKeybord();
        }

        public void BeginMoveDrag()
        {
            //Not supported
        }

        public void BeginResizeDrag(WindowEdge edge)
        {
            //Not supported
        }

        public Point Position
        {
            get { return _position; }
            set
            {
                _position = value;
                PositionChanged?.Invoke(_position);
            }
        }

        public Size MaxClientSize => Bounds.Size.ToAvalonia();
        public void SetTitle(string title)
        {
            //Not supported
        }

        public IDisposable ShowDialog()
        {
            //Not supported
            return Disposable.Empty;
        }

        public void Hide()
        {
            //Not supported
        }

        public void SetSystemDecorations(bool enabled)
        {
            //Not supported
        }

        public void SetCoverTaskbarWhenMaximized(bool enable)
        {
            //Not supported
        }

        public override void TouchesEnded(NSSet touches, UIEvent evt)
        {
            var touch = touches.AnyObject as UITouch;
            if (touch != null)
            {
                var location = touch.LocationInView(this).ToAvalonia();

                Input?.Invoke(new RawMouseEventArgs(
                    iOSPlatform.MouseDevice,
                    (uint)touch.Timestamp,
                    _inputRoot,
                    RawMouseEventType.LeftButtonUp,
                    location,
                    InputModifiers.None));
            }
        }

        Point _touchLastPoint;
        public override void TouchesBegan(NSSet touches, UIEvent evt)
        {
            var touch = touches.AnyObject as UITouch;
            if (touch != null)
            {
                var location = touch.LocationInView(this).ToAvalonia();
                _touchLastPoint = location;
                Input?.Invoke(new RawMouseEventArgs(iOSPlatform.MouseDevice, (uint)touch.Timestamp, _inputRoot,
                    RawMouseEventType.Move, location, InputModifiers.None));

                Input?.Invoke(new RawMouseEventArgs(iOSPlatform.MouseDevice, (uint)touch.Timestamp, _inputRoot,
                    RawMouseEventType.LeftButtonDown, location, InputModifiers.None));
            }
        }

        public override void TouchesMoved(NSSet touches, UIEvent evt)
        {
            var touch = touches.AnyObject as UITouch;
            if (touch != null)
            {
                var location = touch.LocationInView(this).ToAvalonia();
                if (iOSPlatform.MouseDevice.Captured != null)
                    Input?.Invoke(new RawMouseEventArgs(iOSPlatform.MouseDevice, (uint)touch.Timestamp, _inputRoot,
                        RawMouseEventType.Move, location, InputModifiers.LeftMouseButton));
                else
                {
                    //magic number based on test - correction of 0.02 is working perfect
                    double correction = 0.02;

                    Input?.Invoke(new RawMouseWheelEventArgs(iOSPlatform.MouseDevice, (uint)touch.Timestamp,
                        _inputRoot, location, (location - _touchLastPoint) * correction, InputModifiers.LeftMouseButton));
                }
                _touchLastPoint = location;
            }
        }

        public void SetIcon(IWindowIconImpl icon)
        {
        }
    }

    class AvaloniaViewController : UIViewController
    {
        public AvaloniaView AvaloniaView { get; }

        public AvaloniaViewController(UIWindow window)
        {
            AvaloniaView = new AvaloniaView(window, this);
        }

        public override void LoadView()
        {
            View = AvaloniaView;
        }
    }
}
