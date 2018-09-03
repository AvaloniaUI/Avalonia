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
        private readonly IWindowWrapper _windowWrapper;
        private readonly ManagedWindowResizeDragHelper _managedDrag;

        private LogicalPosition _lastPosition;
        private const int FramesPerSecond = 60;
        private bool _visible = false;

        public IWindowWrapper WindowWrapper => _windowWrapper;

        public WindowImpl(IWindowWrapper wrapper)
        {
            _windowWrapper = wrapper;
            _managedDrag = new ManagedWindowResizeDragHelper(this, _ => { }, ResizeForManagedDrag);

            // TODO: This is only necessary when using ImmediateRenderer
       /*     Observable.Interval(TimeSpan.FromMilliseconds(1000 / FramesPerSecond))
                      .ObserveOn(AvaloniaScheduler.Instance)
                      .Subscribe((x) =>
                      {
                        // Dont schedule a paint for empty invalidations.
                        if (coalescedRect != Rect.Empty) {
                            Paint(coalescedRect);
                            coalescedRect = Rect.Empty;
                        }
                      });*/
        }

        private void ResizeForManagedDrag(Rect obj)
        {
            this._windowWrapper.SetPosition(obj.X, obj.Y);
            this._windowWrapper.SetSize(obj.Width, obj.Height);
        }

        public WindowState WindowState { get; set; }
        public Action<WindowState> WindowStateChanged { get; set; }
        public Func<bool> Closing { get; set; }

        private Point _position;
        public Point Position
        {
            get
            {
                var (x, y) = _windowWrapper.GetPosition();
                return new Point(x, y);
            }
            set
            {
                _position = value;
                if (_visible)
                    _windowWrapper.SetPosition(_position.X, _position.Y);
            }
        }

        public Action<Point> PositionChanged { get; set; }
        public Action Deactivated { get; set; }
        public Action Activated { get; set; }

        public IPlatformHandle Handle => new DummyPlatformHandle();

        public Size MaxClientSize => Size.Empty;

        public IScreenImpl Screen => new ScreenImpl(_windowWrapper.EventsLoop);

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
            _managedDrag.BeginMoveDrag(new Point(_lastPosition.X, _lastPosition.Y));
        }

        public void BeginResizeDrag(WindowEdge edge)
        {
        }

        public void CanResize(bool value)
        {
        }

        public IRenderer CreateRenderer(IRenderRoot root)
        {
            return new DeferredRenderer(root, AvaloniaLocator.Current.GetService<IRenderLoop>());
        }

        public void Dispose()
        {
            _windowWrapper.Dispose();
        }

        public void Hide()
        {
            _windowWrapper.Hide();
            _visible = false;
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
            _visible = true;
            _windowWrapper.Show();
            Position = _position;
        }

        public IDisposable ShowDialog()
        {
            _visible = true;
            _windowWrapper.Show();
            Position = _position;
            return Disposable.Create(() => {});
        }

        public void ShowTaskbarIcon(bool value)
        {
        }

        public void OnCharacterEvent(CharacterEvent evt) 
        {
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Input + 1);
            var timeStamp = (uint)Environment.TickCount;

            if (evt.Character >= 32)
            {
                OnInput
                (
                    new RawTextInputEventArgs
                    (
                        KeyboardDevice,
                        timeStamp,
                        evt.Character.ToString()
                    )
                );
            }
        }

        public void OnKeyboardEvent (KeyboardEvent evt)
        {
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Input + 1);
            var eventType = evt.Pressed == 1 ? RawKeyEventType.KeyDown : RawKeyEventType.KeyUp;
            var timeStamp = (uint)Environment.TickCount;

            var modifiers = InputModifiers.None;

            if(evt.Control == 1)
            {
                modifiers |= InputModifiers.Control;
            }

            if (evt.Alt == 1)
            {
                modifiers |= InputModifiers.Alt;
            }

            if(evt.Shift == 1)
            {
                modifiers |= InputModifiers.Shift;
            }

            if (evt.Logo == 1)
            {
                modifiers |= InputModifiers.Windows;
            }

            var keyCode = KeyTransform.TransformKeyCode(evt.VirtualKeyCode);

            if (keyCode.HasValue)
            {
                OnInput
                (
                    new RawKeyEventArgs
                    (
                        KeyboardDevice,
                        timeStamp,
                        eventType,
                        keyCode.Value,
                        modifiers
                    )
                );
            }
        }

        public void OnInput(RawInputEventArgs args)
        {
            if (_managedDrag.PreprocessInputEvent(ref args))
                return;

            Input?.Invoke(args);
        }

        public void OnMouseEvent(MouseEvent evt)
        {
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Input + 1);
            var eventType = (RawMouseEventType)evt.EventType;
            var timeStamp = (uint)Environment.TickCount;

            if (evt.EventType == MouseEventType.Move)
                _lastPosition = evt.Position;
            
            var modifiers = InputModifiers.None;

            if (evt.Control == 1)
            {
                modifiers |= InputModifiers.Control;
            }

            if (evt.Alt == 1)
            {
                modifiers |= InputModifiers.Alt;
            }

            if (evt.Shift == 1)
            {
                modifiers |= InputModifiers.Shift;
            }

            if (evt.Logo == 1)
            {
                modifiers |= InputModifiers.Windows;
            }

            OnInput
            (
                eventType != RawMouseEventType.Wheel ?
                new RawMouseEventArgs
                (
                    MouseDevice, 
                    timeStamp, 
                    _inputRoot,
                    eventType,
                    new Point(_lastPosition.X, _lastPosition.Y), 
                    modifiers
                )
                :
                new RawMouseWheelEventArgs 
                (
                    MouseDevice,
                    timeStamp,
                    _inputRoot,
                    new Point(_lastPosition.X, _lastPosition.Y),
                    new Point(evt.Position.X, evt.Position.Y),
                    modifiers
                )
            );
        }

        public void OnResizeEvent(ResizeEvent evt)
        {
            Resized?.Invoke(ClientSize);

            // TODO: There's a bug in winit where OSX resizing blocks the event loop, to work around this, paint while resizing.
            Paint?.Invoke(new Rect(ClientSize));
        }

        public void OnClosed() 
        {
            Closed?.Invoke();
        }

        public bool OnCloseRequested() 
        {
            return Closing?.Invoke() ?? false; 
        }

        public void OnFocused(bool focused) 
        {
            if (focused)
                Activated?.Invoke();
            else
                Deactivated?.Invoke();
        }
    }
}