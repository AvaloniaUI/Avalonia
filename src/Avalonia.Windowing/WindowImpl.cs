using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Windowing.Bindings;

namespace Avalonia.Windowing
{
    public class Window : IWindowImpl
    {
        IWindowWrapper _windowWrapper;

        public Window(IWindowWrapper wrapper)
        {
            _windowWrapper = wrapper;
        }

        public WindowState WindowState { get; set; }
        public Action<WindowState> WindowStateChanged { get; set; }
        public Func<bool> Closing { get; set; }
        public Point Position 
        {
            get 
            {
                var (x, y) = _windowWrapper.GetPosition();
                return new Point(x, y);
            } 
            set 
            {
                var x = value; 
            }
        }

        public Action<Point> PositionChanged { get; set; }
        public Action Deactivated { get; set; }
        public Action Activated { get; set; }

        public IPlatformHandle Handle => new DummyPlatformHandle();

        public Size MaxClientSize => Size.Empty;

        public IScreenImpl Screen => new Monitors();

        public Size ClientSize {
            get 
            {
                var (width, height) = _windowWrapper.GetSize();
                return new Size(width * 2, height * 2);
            }
        }

        public double Scaling => 192 / 96;

        public IEnumerable<object> Surfaces => new List<object>() { _windowWrapper };

        public Action<RawInputEventArgs> Input { get; set; }
        public Action<Rect> Paint { get; set; }
        public Action<Size> Resized { get; set; }
        public Action<double> ScalingChanged { get; set; }
        public Action Closed { get; set; }

        public IMouseDevice MouseDevice => throw new NotImplementedException();

        public void Activate()
        {
        }

        public void BeginMoveDrag()
        {
        }

        public void BeginResizeDrag(WindowEdge edge)
        {
        }

        public void CanResize(bool value)
        {
        }

        public IRenderer CreateRenderer(IRenderRoot root)
        {
            return new ImmediateRenderer(root);
        }

        public void Dispose()
        {
            _windowWrapper.Dispose();
        }

        public void Hide()
        {
        }

        public void Invalidate(Rect rect)
        {
            Paint?.Invoke(rect);
        }

        public Point PointToClient(Point point)
        {
            point = point.WithY(ClientSize.Height - point.Y);
            var (x, y) = _windowWrapper.GetPosition();
            return new Point(point.X + x, point.Y + y);
        }

        public Point PointToScreen(Point point)
        {
            point = point.WithY(ClientSize.Height - point.Y);
            var (x, y) = _windowWrapper.GetPosition();
            return new Point(point.X - x, point.Y - y);;
        }

        public void Resize(Size clientSize)
        {
            // This is where we size the window accordingly..
            _windowWrapper.SetSize(clientSize.Width / Scaling, clientSize.Height / Scaling);

            // TODO: Move this to when we receive the Resized message.
            Resized(clientSize);
        }

        public void SetCursor(IPlatformHandle cursor)
        {
        }

        public void SetIcon(IWindowIconImpl icon)
        {
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
        }

        public void SetMinMaxSize(Size minSize, Size maxSize)
        {
        }

        public void SetSystemDecorations(bool enabled)
        {
        }

        public void SetTitle(string title)
        {
            _windowWrapper.SetTitle(title);
        }

        public void SetTopmost(bool value)
        {
        }

        public void Show()
        {
            _windowWrapper.Show();
        }

        public IDisposable ShowDialog()
        {
            return null;
        }

        public void ShowTaskbarIcon(bool value)
        {
        }
    }
}
