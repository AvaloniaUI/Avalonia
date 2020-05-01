using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Native.Interop;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;

namespace Avalonia.Native
{
    public class MacOSTopLevelWindowHandle : IPlatformHandle, IMacOSTopLevelPlatformHandle
    {
        IAvnWindowBase _native;

        public MacOSTopLevelWindowHandle(IAvnWindowBase native)
        {
            _native = native;
        }

        public IntPtr Handle => NSWindow;

        public string HandleDescriptor => "NSWindow";

        public IntPtr NSView => _native.ObtainNSViewHandle();

        public IntPtr NSWindow => _native.ObtainNSWindowHandle();

        public IntPtr GetNSViewRetained()
        {
            return _native.ObtainNSViewHandleRetained();
        }

        public IntPtr GetNSWindowRetained()
        {
            return _native.ObtainNSWindowHandleRetained();
        }
    }

    public abstract class WindowBaseImpl : IWindowBaseImpl,
        IFramebufferPlatformSurface
    {
        IInputRoot _inputRoot;
        IAvnWindowBase _native;
        private object _syncRoot = new object();
        private bool _deferredRendering = false;
        private bool _gpu = false;
        private readonly MouseDevice _mouse;
        private readonly IKeyboardDevice _keyboard;
        private readonly IStandardCursorFactory _cursorFactory;
        private Size _savedLogicalSize;
        private Size _lastRenderedLogicalSize;
        private double _savedScaling;
        private GlPlatformSurface _glSurface;

        internal WindowBaseImpl(AvaloniaNativePlatformOptions opts, GlPlatformFeature glFeature)
        {
            _gpu = opts.UseGpu && glFeature != null;
            _deferredRendering = opts.UseDeferredRendering;

            _keyboard = AvaloniaLocator.Current.GetService<IKeyboardDevice>();
            _mouse = new MouseDevice();
            _cursorFactory = AvaloniaLocator.Current.GetService<IStandardCursorFactory>();
        }

        protected void Init(IAvnWindowBase window, IAvnScreens screens)
        {
            _native = window;

            Handle = new MacOSTopLevelWindowHandle(window);
            if (_gpu)
                _glSurface = new GlPlatformSurface(window);
            Screen = new ScreenImpl(screens);
            _savedLogicalSize = ClientSize;
            _savedScaling = Scaling;

            var monitor = Screen.AllScreens.OrderBy(x => x.PixelDensity)
                    .FirstOrDefault(m => m.Bounds.Contains(Position));

            Resize(new Size(monitor.WorkingArea.Width * 0.75d, monitor.WorkingArea.Height * 0.7d));
        }

        public Size ClientSize 
        {
            get
            {
                var s = _native.GetClientSize();
                return new Size(s.Width, s.Height);
            }
        }

        public IEnumerable<object> Surfaces => new[] {
            (_gpu ? _glSurface : (object)null),
            this 
        };

        public ILockedFramebuffer Lock()
        {
            var w = _savedLogicalSize.Width * _savedScaling;
            var h = _savedLogicalSize.Height * _savedScaling;
            var dpi = _savedScaling * 96;
            return new DeferredFramebuffer(cb =>
            {
                lock (_syncRoot)
                {
                    if (_native == null)
                        return false;
                    cb(_native);
                    _lastRenderedLogicalSize = _savedLogicalSize;
                    return true;
                }
            }, (int)w, (int)h, new Vector(dpi, dpi));
        }

        public Action<Rect> Paint { get; set; }
        public Action<Size> Resized { get; set; }
        public Action Closed { get; set; }
        public IMouseDevice MouseDevice => _mouse;
        public abstract IPopupImpl CreatePopup();

        protected class WindowBaseEvents : CallbackBase, IAvnWindowBaseEvents
        {
            private readonly WindowBaseImpl _parent;

            public WindowBaseEvents(WindowBaseImpl parent)
            {
                _parent = parent;
            }

            void IAvnWindowBaseEvents.Closed()
            {
                var n = _parent._native;
                _parent._native = null;
                try
                {
                    _parent?.Closed?.Invoke();
                }
                finally
                {
                    n?.Dispose();
                }
                _parent._mouse.Dispose();
            }

            void IAvnWindowBaseEvents.Activated() => _parent.Activated?.Invoke();

            void IAvnWindowBaseEvents.Deactivated() => _parent.Deactivated?.Invoke();

            void IAvnWindowBaseEvents.Paint()
            {
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Render);
                var s = _parent.ClientSize;
                _parent.Paint?.Invoke(new Rect(0, 0, s.Width, s.Height));
            }

            void IAvnWindowBaseEvents.Resized(AvnSize size)
            {
                if (_parent._native != null)
                {
                    var s = new Size(size.Width, size.Height);
                    _parent._savedLogicalSize = s;
                    _parent.Resized?.Invoke(s);
                }
            }

            void IAvnWindowBaseEvents.PositionChanged(AvnPoint position)
            {
                _parent.PositionChanged?.Invoke(position.ToAvaloniaPixelPoint());
            }

            void IAvnWindowBaseEvents.RawMouseEvent(AvnRawMouseEventType type, uint timeStamp, AvnInputModifiers modifiers, AvnPoint point, AvnVector delta)
            {
                _parent.RawMouseEvent(type, timeStamp, modifiers, point, delta);
            }

            bool IAvnWindowBaseEvents.RawKeyEvent(AvnRawKeyEventType type, uint timeStamp, AvnInputModifiers modifiers, uint key)
            {
                return _parent.RawKeyEvent(type, timeStamp, modifiers, key);
            }

            bool IAvnWindowBaseEvents.RawTextInputEvent(uint timeStamp, string text)
            {
                return _parent.RawTextInputEvent(timeStamp, text);
            }


            void IAvnWindowBaseEvents.ScalingChanged(double scaling)
            {
                _parent._savedScaling = scaling;
                _parent.ScalingChanged?.Invoke(scaling);
            }

            void IAvnWindowBaseEvents.RunRenderPriorityJobs()
            {
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Render);
            }
        }

        public void Activate()
        {
            _native.Activate();
        }

        public bool RawTextInputEvent(uint timeStamp, string text)
        {
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Input + 1);

            var args = new RawTextInputEventArgs(_keyboard, timeStamp, _inputRoot, text);

            Input?.Invoke(args);

            return args.Handled;
        }

        public bool RawKeyEvent(AvnRawKeyEventType type, uint timeStamp, AvnInputModifiers modifiers, uint key)
        {
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Input + 1);

            var args = new RawKeyEventArgs(_keyboard, timeStamp, _inputRoot, (RawKeyEventType)type, (Key)key, (RawInputModifiers)modifiers);

            Input?.Invoke(args);

            return args.Handled;
        }

        public void RawMouseEvent(AvnRawMouseEventType type, uint timeStamp, AvnInputModifiers modifiers, AvnPoint point, AvnVector delta)
        {
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Input + 1);

            switch (type)
            {
                case AvnRawMouseEventType.Wheel:
                    Input?.Invoke(new RawMouseWheelEventArgs(_mouse, timeStamp, _inputRoot, point.ToAvaloniaPoint(), new Vector(delta.X, delta.Y), (RawInputModifiers)modifiers));
                    break;

                default:
                    Input?.Invoke(new RawPointerEventArgs(_mouse, timeStamp, _inputRoot, (RawPointerEventType)type, point.ToAvaloniaPoint(), (RawInputModifiers)modifiers));
                    break;
            }
        }

        public void Resize(Size clientSize)
        {
            _native.Resize(clientSize.Width, clientSize.Height);
        }

        public IRenderer CreateRenderer(IRenderRoot root)
        {
            if (_deferredRendering)
                return new DeferredRenderer(root, AvaloniaLocator.Current.GetService<IRenderLoop>());
            return new ImmediateRenderer(root);
        }

        public virtual void Dispose()
        {
            _native?.Close();
            _native?.Dispose();
            _native = null;

            (Screen as ScreenImpl)?.Dispose();
        }


        public void Invalidate(Rect rect)
        {
            if (!_deferredRendering && _native != null)
                _native.Invalidate(new AvnRect { Height = rect.Height, Width = rect.Width, X = rect.X, Y = rect.Y });
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
            _inputRoot = inputRoot;
        }


        public void Show()
        {
            _native.Show();
        }


        public PixelPoint Position
        {
            get => _native.GetPosition().ToAvaloniaPixelPoint();
            set => _native.SetPosition(value.ToAvnPoint());
        }

        public Point PointToClient(PixelPoint point)
        {
            return _native.PointToClient(point.ToAvnPoint()).ToAvaloniaPoint();
        }

        public PixelPoint PointToScreen(Point point)
        {
            return _native.PointToScreen(point.ToAvnPoint()).ToAvaloniaPixelPoint();
        }

        public void Hide()
        {
            _native.Hide();
        }

        public void BeginMoveDrag(PointerPressedEventArgs e)
        {
            _native.BeginMoveDrag();
        }

        public Size MaxClientSize => Screen.AllScreens.Select(s => s.Bounds.Size.ToSize(s.PixelDensity))
            .OrderByDescending(x => x.Width + x.Height).FirstOrDefault();

        public void SetTopmost(bool value)
        {
            _native.SetTopMost(value);
        }

        public double Scaling => _native?.GetScaling() ?? 1;

        public Action Deactivated { get; set; }
        public Action Activated { get; set; }

        public void SetCursor(IPlatformHandle cursor)
        {
            if (_native == null)
            {
                return;
            }
            
            var newCursor = cursor as AvaloniaNativeCursor;
            newCursor = newCursor ?? (_cursorFactory.GetCursor(StandardCursorType.Arrow) as AvaloniaNativeCursor);
            _native.Cursor = newCursor.Cursor;
        }

        public Action<PixelPoint> PositionChanged { get; set; }

        public Action<RawInputEventArgs> Input { get; set; }

        Action<double> ScalingChanged { get; set; }

        Action<double> ITopLevelImpl.ScalingChanged { get; set; }

        public IScreenImpl Screen { get; private set; }

        // TODO

        public void SetMinMaxSize(Size minSize, Size maxSize)
        {
            _native.SetMinMaxSize(minSize.ToAvnSize(), maxSize.ToAvnSize());
        }

        public void BeginResizeDrag(WindowEdge edge, PointerPressedEventArgs e)
        {

        }

        public IPlatformHandle Handle { get; private set; }
    }
}
