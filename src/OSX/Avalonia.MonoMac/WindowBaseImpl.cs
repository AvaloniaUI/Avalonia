using System;
using Avalonia.Controls;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using MonoMac.AppKit;
using MonoMac.CoreGraphics;
using MonoMac.Foundation;
using MonoMac.ObjCRuntime;

namespace Avalonia.MonoMac
{
    class WindowBaseImpl : TopLevelImpl, IWindowBaseImpl
    {
        private readonly ManagedWindowResizeDragHelper _managedDrag;
        public CustomWindow Window { get; private set; }

        public WindowBaseImpl()
        {
            _managedDrag = new ManagedWindowResizeDragHelper(this, _ => { }, ResizeForManagedDrag);
            Window = new CustomWindow(this)
            {
                StyleMask = NSWindowStyle.Titled,
                BackingType = NSBackingStore.Buffered,
                ContentView = View,
                // ReSharper disable once VirtualMemberCallInConstructor
                Delegate = CreateWindowDelegate()
            };
        }

        public class CustomWindow : NSWindow
        {
            readonly WindowBaseImpl _impl;

            public CustomWindow(WindowBaseImpl impl)
            {
                _impl = impl;
            }

            public override void BecomeKeyWindow()
            {
                _impl.Activated?.Invoke();
                base.BecomeKeyWindow();
            }

            public override void ResignKeyWindow()
            {
                _impl.Deactivated?.Invoke();
                base.ResignKeyWindow();
            }

            private bool _canBecomeKeyAndMain;
            public override bool CanBecomeKeyWindow => _canBecomeKeyAndMain;
            public override bool CanBecomeMainWindow => _canBecomeKeyAndMain;

            public void SetCanBecomeKeyAndMain() => _canBecomeKeyAndMain = true;
        }

        protected virtual NSWindowDelegate CreateWindowDelegate() => new WindowBaseDelegate(this);

        public class WindowBaseDelegate : NSWindowDelegate
        {
            readonly WindowBaseImpl _impl;
            public WindowBaseDelegate(WindowBaseImpl impl)
            {
                _impl = impl;
            }

            public override void DidMoved(global::MonoMac.Foundation.NSNotification notification)
            {
                _impl.PositionChanged?.Invoke(_impl.Position);
            }

            public override bool WindowShouldClose(NSObject sender)
            {
                bool? preventClose = _impl.Closing?.Invoke();
                return preventClose != true;
            }

            public override void WillClose(global::MonoMac.Foundation.NSNotification notification)
            {
                _impl.Window.Dispose();
                _impl.Window = null;
                _impl.Dispose();
            }

            public override CGRect WillUseStandardFrame(NSWindow window, CGRect newFrame)
            {
                if (_impl is WindowImpl w && w.UndecoratedIsMaximized && w.UndecoratedLastUnmaximizedFrame.HasValue)
                    return w.UndecoratedLastUnmaximizedFrame.Value;
                return window.Screen.VisibleFrame;
            }

            public override bool ShouldZoom(NSWindow window, CGRect newFrame)
            {
                return true;
            }
        }


        public Point Position
        {
            get => Window.Frame.ToAvaloniaRect().BottomLeft.ConvertPointY();
            set => Window.SetFrameTopLeftPoint(value.ToMonoMacPoint().ConvertPointY());
        }


        protected virtual NSWindowStyle GetStyle() => NSWindowStyle.Borderless;

        protected void UpdateStyle() => Window.StyleMask = GetStyle();


        IPlatformHandle IWindowBaseImpl.Handle => new PlatformHandle(Window.Handle, "NSWindow");
        public Size MaxClientSize => NSScreen.Screens[0].Frame.ToAvaloniaRect().Size;
        public Action<Point> PositionChanged { get; set; }
        public Action Deactivated { get; set; }
        public Action Activated { get; set; }
        public Func<bool> Closing { get; set; }

        public override Size ClientSize => Window.ContentRectFor(Window.Frame).Size.ToAvaloniaSize();


        public void Show() => Window.MakeKeyAndOrderFront(Window);

        public void Hide() => Window?.OrderOut(Window);


        public void BeginMoveDrag()
        {
            var ev = View.LastMouseDownEvent;
            if (ev == null)
                return;
            var handle = Selector.GetHandle("performWindowDragWithEvent:");
            Messaging.void_objc_msgSend_IntPtr(Window.Handle, handle, ev.Handle);
        }

        public void BeginResizeDrag(WindowEdge edge)
        {
            var screenPoint = NSEvent.CurrentMouseLocation.ConvertPointY().ToAvaloniaPoint();
            _managedDrag.BeginResizeDrag(edge, PointToClient(screenPoint));
        }

        protected override void OnInput(RawInputEventArgs args)
        {
            if (_managedDrag.PreprocessInputEvent(ref args))
                return;
            base.OnInput(args);
        }

        public void Activate() => Window.MakeKeyWindow();

        public void ResizeForManagedDrag(Rect rc)
        {
            var frame = new CGRect(rc.X, rc.Position.ConvertPointY().Y - rc.Height, rc.Width, rc.Height);
            Window.SetFrame(frame, true);
        }

        public void Resize(Size clientSize)
        {
            var pos = Position;
            Window.SetContentSize(clientSize.ToMonoMacSize());
            Position = pos;
        }

        public IScreenImpl Screen
        {
            get;
        } = new ScreenImpl();

        public override Point PointToClient(Point point)
        {
            var cocoaScreenPoint = point.ToMonoMacPoint().ConvertPointY();
            var cocoaViewPoint = Window.ConvertScreenToBase(cocoaScreenPoint).ToAvaloniaPoint();
            return View.TranslateLocalPoint(cocoaViewPoint);
        }

        public override Point PointToScreen(Point point)
        {
            var cocoaViewPoint = View.TranslateLocalPoint(point).ToMonoMacPoint();
            var cocoaScreenPoint = Window.ConvertBaseToScreen(cocoaViewPoint);
            return cocoaScreenPoint.ConvertPointY().ToAvaloniaPoint();
        }



        public override void Dispose()
        {
            Window?.Close();
            Window?.Dispose();
            base.Dispose();
        }
    }
}
