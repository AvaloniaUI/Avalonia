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

        private bool _deferredRendering = false;
        private readonly IMouseDevice _mouse;
        private readonly IKeyboardDevice _keyboard;

        public WindowBaseImpl()
        {
            _keyboard = AvaloniaLocator.Current.GetService<IKeyboardDevice>();
            _mouse = AvaloniaLocator.Current.GetService<IMouseDevice>();
        }

        protected void Init(IAvnWindowBase window)
        {
            _native = window;
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
            return _framebuffer;
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

            void IAvnWindowBaseEvents.SoftwareDraw(IntPtr ptr, int stride, int pixelWidth, int pixelHeight, AvnSize logicalSize)
            {
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Render);

                _parent._framebuffer = new SavedFramebuffer
                {
                    Address = ptr,
                    RowBytes = stride,
                    Width = pixelWidth,
                    Height = pixelHeight,
                    Dpi = new Vector(pixelWidth / logicalSize.Width * 96, pixelHeight / logicalSize.Height * 96)
                };

                _parent.Paint?.Invoke(new Rect(0, 0, logicalSize.Width, logicalSize.Height));

            }

            void IAvnWindowBaseEvents.Resized(AvnSize size) => _parent.Resized?.Invoke(new Size(size.Width, size.Height));

            public void RawMouseEvent(AvnRawMouseEventType type, uint timeStamp, AvnInputModifiers modifiers, AvnPoint point, AvnVector delta)
            {
                _parent.RawMouseEvent(type, timeStamp, modifiers, point, delta);
            }

            public bool RawKeyEvent(AvnRawKeyEventType type, uint timeStamp, AvnInputModifiers modifiers, uint key)
            {
                return _parent.RawKeyEvent(type, timeStamp, modifiers, key);
            }
        }


        public void Activate()
        {
        
        }

        public bool RawKeyEvent(AvnRawKeyEventType type, uint timeStamp, AvnInputModifiers modifiers, uint key)
        {
            var args = new RawKeyEventArgs(_keyboard, timeStamp, (RawKeyEventType)type, (Key)key, (InputModifiers)modifiers);

            Input?.Invoke(args);

            return args.Handled;
        }

        public void RawMouseEvent(AvnRawMouseEventType type, uint timeStamp, AvnInputModifiers modifiers, AvnPoint point, AvnVector delta)
        {
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Input + 1);

            switch (type)
            {
                case AvnRawMouseEventType.Wheel:
                    Input?.Invoke(new RawMouseWheelEventArgs(_mouse, timeStamp, _inputRoot, new Point(point.X, point.Y), new Vector(delta.X, delta.Y), (InputModifiers)modifiers));
                    break;

                default:
                    Input?.Invoke(new RawMouseEventArgs(_mouse, timeStamp, _inputRoot, (RawMouseEventType)type, new Point(point.X, point.Y), (InputModifiers)modifiers));
                    break;
            }
        }

        public void Resize(Size clientSize)
        {
            _native.Resize(clientSize.Width, clientSize.Height);
        }

        public IRenderer CreateRenderer(IRenderRoot root)
        {
            //_deferredRendering = true;
            //return new DeferredRenderer(root, AvaloniaLocator.Current.GetService<IRenderLoop>());
            return new ImmediateRenderer(root);
        }

        public void Dispose()
        {
            _native.Close();
            _native.Dispose();
            _native = null;
        }


        public void Invalidate(Rect rect)
        {
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


        #region Stubs
        public double Scaling => 1;

        public Point Position { get; set; }
        public Action<Point> PositionChanged { get; set; }
        public Action Deactivated { get; set; }
        public Action Activated { get; set; }
        public Action<RawInputEventArgs> Input { get; set; }

        Action<double> ScalingChanged { get; set; }
        public IPlatformHandle Handle => new PlatformHandle(IntPtr.Zero, "NOT SUPPORTED");

        public Size MaxClientSize => new Size(1600, 900);

        public IScreenImpl Screen => new ScreenImpl();

        Action<double> ITopLevelImpl.ScalingChanged { get; set; }

        public void SetTopmost(bool value)
        {
        }

        public void SetMinMaxSize(Size minSize, Size maxSize)
        {
        }


        public void SetCursor(IPlatformHandle cursor)
        {
        }

        public void Hide()
        {
        }

        public void BeginMoveDrag()
        {
            _native.BeginMoveDrag();
        }

        public void BeginResizeDrag(WindowEdge edge)
        {
        }

        public Point PointToClient(Point point)
        {
            return point;
        }

        public Point PointToScreen(Point point)
        {
            return point;
        }

        #endregion
    }
}
