using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Native.Interop;
using Avalonia.Platform;
using Avalonia.Rendering;

namespace Avalonia.Native
{
    public class WindowBaseImpl : IWindowBaseImpl, IFramebufferPlatformSurface
    {
        IInputRoot _inputRoot;
        IAvnWindowBase _native;

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

        }


        public void Activate()
        {
        
        }


        public void Resize(Size clientSize)
        {
            _native.Resize(clientSize.Width, clientSize.Height);
        }

        public IRenderer CreateRenderer(IRenderRoot root)
        {
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
            //TODO;
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
