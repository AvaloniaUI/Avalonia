using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Controls;
using Avalonia.Gpu;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;
using Avalonia.Windowing.Bindings;

namespace Avalonia.Windowing
{
    public class WindowImpl : IWindowImpl
    {
        IWindowWrapper _windowWrapper;
        private LogicalPosition _lastPosition;

        const int FramesPerSecond = 60;

        public WindowImpl(IWindowWrapper wrapper)
        {
            _windowWrapper = wrapper;

            // TODO: This is only necessary when using ImmediateRenderer
            Observable.Repeat(Observable.Timer(TimeSpan.FromMilliseconds(1000 / FramesPerSecond)))
                      .SubscribeOn(AvaloniaScheduler.Instance)
                      .Subscribe((x) =>
                      {
                        // Dont schedule a paint for empty invalidations.
                        if (coalescedRect != Rect.Empty) {
                            Dispatcher.UIThread.Post(() => 
                            { 
                                Paint(coalescedRect);
                                coalescedRect = Rect.Empty;
                            }, DispatcherPriority.Render);
                        }
                      });
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
                _windowWrapper.SetPosition(value.X, value.Y);
            }
        }

        public Action<Point> PositionChanged { get; set; }
        public Action Deactivated { get; set; }
        public Action Activated { get; set; }

        public IPlatformHandle Handle => new DummyPlatformHandle();

        public Size MaxClientSize => Size.Empty;

        public IScreenImpl Screen => new Monitors();

        public Size ClientSize
        {
            get
            {
                var (width, height) = _windowWrapper.GetSize();
                return new Size(width, height);
            }
        }

        public double Scaling => _windowWrapper.GetScaleFactor();

        public IEnumerable<object> Surfaces => new List<object>() { _windowWrapper };

        public Action<RawInputEventArgs> Input { get; set; }
        public Action<Rect> Paint { get; set; }
        public Action<Size> Resized { get; set; }
        public Action<double> ScalingChanged { get; set; }
        public Action Closed { get; set; }

        public IMouseDevice MouseDevice => AvaloniaLocator.Current.GetService<IMouseDevice>();
        public IKeyboardDevice KeyboardDevice => AvaloniaLocator.Current.GetService<IKeyboardDevice>();

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
            _windowWrapper.Hide();
        }

        private Rect coalescedRect = Rect.Empty;
        public void Invalidate(Rect rect)
        {
            coalescedRect = coalescedRect.Union(rect);
        }

        public Point PointToClient(Point point)
        {
            var position = Position;
            return new Point(point.X - position.X, point.Y - position.Y);
        }

        public Point PointToScreen(Point point)
        {
            var position = Position;
            return new Point(point.X + position.X, point.Y + position.Y); ;
        }

        public void Resize(Size clientSize)
        {
            if (clientSize == ClientSize)
                return;

            _windowWrapper.SetSize(clientSize.Width, clientSize.Height);
        }

        public void SetCursor(IPlatformHandle cursor)
        {
        }

        public void SetIcon(IWindowIconImpl icon)
        {
        }

        private IInputRoot _inputRoot;
        public void SetInputRoot(IInputRoot inputRoot) => _inputRoot = inputRoot;

        public void SetMinMaxSize(Size minSize, Size maxSize)
        {
        }

        public void SetSystemDecorations(bool enabled)
        {
            _windowWrapper.ToggleDecorations(enabled);
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
            return Disposable.Create(() => {});
        }

        public void ShowTaskbarIcon(bool value)
        {
        }

        public void OnCharacterEvent(CharacterEvent evt) 
        {
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Input + 1);
            var timeStamp = (uint)Environment.TickCount;

            Input
            (
                new RawTextInputEventArgs
                (
                    KeyboardDevice,
                    timeStamp,
                    evt.Character.ToString()
                )
            );
        }

        public void OnKeyboardEvent (KeyboardEvent evt)
        {
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Input + 1);
            var eventType = evt.Pressed == 1 ? RawKeyEventType.KeyDown : RawKeyEventType.KeyUp;
            var timeStamp = (uint)Environment.TickCount;

            Input
            (
                new RawKeyEventArgs
                (
                    KeyboardDevice, 
                    timeStamp,
                    eventType, 
                    KeyTransform.TransformKeyCode(evt.VirtualKeyCode).Value, 
                    InputModifiers.None
                )
            );
        }

        public void OnMouseEvent(MouseEvent evt)
        {
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Input + 1);
            var eventType = (RawMouseEventType)evt.EventType;
            var timeStamp = (uint)Environment.TickCount;

            if (evt.EventType == MouseEventType.Move)
                _lastPosition = evt.Position;
            
            Input
            (
                eventType != RawMouseEventType.Wheel ?
                new RawMouseEventArgs
                (
                    MouseDevice, 
                    timeStamp, 
                    _inputRoot,
                    eventType,
                    new Point(_lastPosition.X, _lastPosition.Y), 
                    InputModifiers.None
                )
                :
                new RawMouseWheelEventArgs 
                (
                    MouseDevice,
                    timeStamp,
                    _inputRoot,
                    new Point(_lastPosition.X, _lastPosition.Y),
                    new Point(evt.Position.X, evt.Position.Y),
                    InputModifiers.None
                )
            );
        }

        public void OnResizeEvent(ResizeEvent evt)
        {
            Resized?.Invoke(ClientSize);
            if (_windowWrapper is IGpuContext gpuCtx) {
                gpuCtx.ResizeContext(ClientSize.Width, ClientSize.Height);
            }
        }
    }
}