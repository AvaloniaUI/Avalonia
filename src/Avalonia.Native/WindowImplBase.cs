using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Native.Interop;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;

namespace Avalonia.Native
{
    public class WindowBaseImpl : IWindowBaseImpl, IFramebufferPlatformSurface
    {
        IInputRoot _inputRoot;
        IAvnWindowBase _native;
        private object _syncRoot = new object();
        private bool _deferredRendering = true;
        private readonly IMouseDevice _mouse;
        private Size _savedLogicalSize;
        private Size _lastRenderedLogicalSize;
        private double _savedScaling;

        public WindowBaseImpl()
        {
            _mouse = AvaloniaLocator.Current.GetService<IMouseDevice>();
        }

        protected void Init(IAvnWindowBase window, IAvnScreens screens)
        {
            _native = window;
            
            Screen = new ScreenImpl(screens);
            _savedLogicalSize = ClientSize;
            _savedScaling = Scaling;
        }

        public Size ClientSize 
        {
            get
            {
                var s = _native.GetClientSize();
                return new Size(s.Width, s.Height);
            }
        }
        SavedFramebuffer _framebuffer;

        public IEnumerable<object> Surfaces => new[] { this };
        public ILockedFramebuffer Lock()
        {
            if(_deferredRendering)
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

            var fb = _framebuffer;
            _framebuffer = null;
            if (fb == null)
                throw new InvalidOperationException("Lock call without corresponding Paint event");
            return fb;
        }

        public Action<Rect> Paint { get; set; }
        public Action<Size> Resized { get; set; }
        public Action Closed { get; set; }
        public IMouseDevice MouseDevice => AvaloniaNativePlatform.MouseDevice;


        class SavedFramebuffer : ILockedFramebuffer
        {
            public IntPtr Address { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public int RowBytes {get;set;}
            public Vector Dpi { get; set; }
            public PixelFormat Format => PixelFormat.Rgba8888;
            public void Dispose()
            {
                // Do nothing
            }
        }

        protected class WindowBaseEvents : CallbackBase, IAvnWindowBaseEvents
        {
            private readonly WindowBaseImpl _parent;

            public WindowBaseEvents(WindowBaseImpl parent)
            {
                _parent = parent;
            }

            void IAvnWindowBaseEvents.Closed() => _parent.Closed?.Invoke();

            void IAvnWindowBaseEvents.Activated() => _parent.Activated?.Invoke();

            void IAvnWindowBaseEvents.Deactivated() => _parent.Deactivated?.Invoke();

            void IAvnWindowBaseEvents.SoftwareDraw(ref AvnFramebuffer fb)
            {
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Render);

                _parent._framebuffer = new SavedFramebuffer
                {
                    Address = fb.Data,
                    RowBytes = fb.Stride,
                    Width = fb.Width,
                    Height = fb.Height,
                    Dpi = new Vector(fb.Dpi.X, fb.Dpi.Y)
                };

                _parent.Paint?.Invoke(new Rect(0, 0, fb.Width / (fb.Dpi.X / 96), fb.Height / (fb.Dpi.Y / 96)));

            }

            void IAvnWindowBaseEvents.Resized(AvnSize size)
            {
                var s = new Size(size.Width, size.Height);
                _parent._savedLogicalSize = s;
                _parent.Resized?.Invoke(s);
            }

            void IAvnWindowBaseEvents.PositionChanged(AvnPoint position)
            {
                _parent.PositionChanged?.Invoke(position.ToAvaloniaPoint());
            }

            void IAvnWindowBaseEvents.RawMouseEvent(AvnRawMouseEventType type, uint timeStamp, AvnInputModifiers modifiers, AvnPoint point, AvnVector delta)
            {
                _parent.RawMouseEvent(type, timeStamp, modifiers, point, delta);
            }

            void IAvnWindowBaseEvents.ScalingChanged(double scaling)
            {
                _parent._savedScaling = scaling;
                _parent.ScalingChanged?.Invoke(scaling);
            }

            void IAvnWindowBaseEvents.RunRenderPriorityJobs()
            {
                if (_parent._deferredRendering 
                    && _parent._lastRenderedLogicalSize != _parent.ClientSize)
                    // Hack to trigger Paint event on the renderer
                    _parent.Paint?.Invoke(new Rect());
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Render);
            }
        }


        public void Activate()
        {
        
        }

        public void RawMouseEvent(AvnRawMouseEventType type, uint timeStamp, AvnInputModifiers modifiers, AvnPoint point, AvnVector delta)
        {
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Input + 1);

            switch (type)
            {
                case AvnRawMouseEventType.Wheel:
                    Input?.Invoke(new RawMouseWheelEventArgs(_mouse, timeStamp, _inputRoot, point.ToAvaloniaPoint(), new Vector(delta.X, delta.Y), (InputModifiers)modifiers));
                    break;

                default:
                    Input?.Invoke(new RawMouseEventArgs(_mouse, timeStamp, _inputRoot, (RawMouseEventType)type, point.ToAvaloniaPoint(), (InputModifiers)modifiers));
                    break;
            }
        }

        public void Resize(Size clientSize)
        {
            _native.Resize(clientSize.Width, clientSize.Height);
        }

        public IRenderer CreateRenderer(IRenderRoot root)
        {
            if(_deferredRendering)
                return new DeferredRendererProxy(root);
            return new ImmediateRenderer(root);
        }

        public virtual void Dispose()
        {
            _native.Close();
            _native.Dispose();
            _native = null;

            (Screen as ScreenImpl)?.Dispose();
        }


        public void Invalidate(Rect rect)
        {
            if (!_deferredRendering)
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


        public Point Position
        {
            get => _native.GetPosition().ToAvaloniaPoint();
            set => _native.SetPosition(value.ToAvnPoint());
        }

        public Point PointToClient(Point point)
        {
            return _native.PointToClient(point.ToAvnPoint()).ToAvaloniaPoint();
        }

        public Point PointToScreen(Point point)
        {
            return _native.PointToScreen(point.ToAvnPoint()).ToAvaloniaPoint();
        }

        public void Hide()
        {
            _native.Hide();
        }

        public void BeginMoveDrag()
        {
            _native.BeginMoveDrag();
        }

        public Size MaxClientSize => _native.GetMaxClientSize().ToAvaloniaSize();

        public void SetTopmost(bool value)
        {
            _native.SetTopMost(value);
        }

        public double Scaling => _native.GetScaling();

        public Action Deactivated { get; set; }
        public Action Activated { get; set; }

        #region Stubs

        public Action<Point> PositionChanged { get; set; }

        public Action<RawInputEventArgs> Input { get; set; }

        Action<double> ScalingChanged { get; set; }
        public IPlatformHandle Handle => new PlatformHandle(IntPtr.Zero, "NOT SUPPORTED");


        public IScreenImpl Screen { get; private set; }

        Action<double> ITopLevelImpl.ScalingChanged { get; set; }

        public void SetMinMaxSize(Size minSize, Size maxSize)
        {
        }


        public void SetCursor(IPlatformHandle cursor)
        {
        }

        public void BeginResizeDrag(WindowEdge edge)
        {
        }

        #endregion
    }
}
