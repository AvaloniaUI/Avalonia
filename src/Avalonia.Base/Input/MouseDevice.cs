using System;
using System.Collections.Generic;
using Avalonia.Reactive;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Utilities;
using Avalonia.VisualTree;
using Avalonia.Input.GestureRecognizers;
#pragma warning disable CS0618

namespace Avalonia.Input
{
    /// <summary>
    /// Represents a mouse device.
    /// </summary>
    [PrivateApi]
    public class MouseDevice : IMouseDevice, IDisposable
    {
        private int _clickCount;
        private Rect _lastClickRect;
        private ulong _lastClickTime;

        private readonly Pointer _pointer;
        private bool _disposed;
        private MouseButton _lastMouseDownButton;

        public MouseDevice(Pointer? pointer = null)
        {
            _pointer = pointer ?? new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
        }

        public void ProcessRawEvent(RawInputEventArgs e)
        {
            if (!e.Handled && e is RawPointerEventArgs margs)
                ProcessRawEvent(margs);
        }

        static int ButtonCount(PointerPointProperties props)
        {
            var rv = 0;
            if (props.IsLeftButtonPressed)
                rv++;
            if (props.IsMiddleButtonPressed)
                rv++;
            if (props.IsRightButtonPressed)
                rv++;
            if (props.IsXButton1Pressed)
                rv++;
            if (props.IsXButton2Pressed)
                rv++;
            return rv;
        }

        private void ProcessRawEvent(RawPointerEventArgs e)
        {
            e = e ?? throw new ArgumentNullException(nameof(e));

            var mouse = (MouseDevice)e.Device;
            if(mouse._disposed)
                return;

            var props = CreateProperties(e);
            var keyModifiers = e.InputModifiers.ToKeyModifiers();
            switch (e.Type)
            {
                case RawPointerEventType.LeaveWindow:
                case RawPointerEventType.NonClientLeftButtonDown:
                    LeaveWindow();
                    break;
                case RawPointerEventType.LeftButtonDown:
                case RawPointerEventType.RightButtonDown:
                case RawPointerEventType.MiddleButtonDown:
                case RawPointerEventType.XButton1Down:
                case RawPointerEventType.XButton2Down:
                    if (ButtonCount(props) > 1)
                        e.Handled = MouseMove(mouse, e.Timestamp, e.Root, e.Position, props, keyModifiers, e.IntermediatePoints, e.InputHitTestResult);
                    else
                        e.Handled = MouseDown(mouse, e.Timestamp, e.Root, e.Position, props, keyModifiers, e.InputHitTestResult);
                    break;
                case RawPointerEventType.LeftButtonUp:
                case RawPointerEventType.RightButtonUp:
                case RawPointerEventType.MiddleButtonUp:
                case RawPointerEventType.XButton1Up:
                case RawPointerEventType.XButton2Up:
                    if (ButtonCount(props) != 0)
                        e.Handled = MouseMove(mouse, e.Timestamp, e.Root, e.Position, props, keyModifiers, e.IntermediatePoints, e.InputHitTestResult);
                    else
                        e.Handled = MouseUp(mouse, e.Timestamp, e.Root, e.Position, props, keyModifiers, e.InputHitTestResult);
                    break;
                case RawPointerEventType.Move:
                    e.Handled = MouseMove(mouse, e.Timestamp, e.Root, e.Position, props, keyModifiers, e.IntermediatePoints, e.InputHitTestResult);
                    break;
                case RawPointerEventType.Wheel:
                    e.Handled = MouseWheel(mouse, e.Timestamp, e.Root, e.Position, props, ((RawMouseWheelEventArgs)e).Delta, keyModifiers, e.InputHitTestResult);
                    break;
                case RawPointerEventType.Magnify:
                    e.Handled = GestureMagnify(mouse, e.Timestamp, e.Root, e.Position, props, ((RawPointerGestureEventArgs)e).Delta, keyModifiers, e.InputHitTestResult);
                    break;
                case RawPointerEventType.Rotate:
                    e.Handled = GestureRotate(mouse, e.Timestamp, e.Root, e.Position, props, ((RawPointerGestureEventArgs)e).Delta, keyModifiers, e.InputHitTestResult);
                    break;
                case RawPointerEventType.Swipe:
                    e.Handled = GestureSwipe(mouse, e.Timestamp, e.Root, e.Position, props, ((RawPointerGestureEventArgs)e).Delta, keyModifiers, e.InputHitTestResult);
                    break;
            }
        }

        private void LeaveWindow()
        {

        }

        static PointerPointProperties CreateProperties(RawPointerEventArgs args)
        {
            return new PointerPointProperties(args.InputModifiers, args.Type.ToUpdateKind());
        }

        private bool MouseDown(IMouseDevice device, ulong timestamp, IInputElement root, Point p,
            PointerPointProperties properties,
            KeyModifiers inputModifiers, IInputElement? hitTest)
        {
            device = device ?? throw new ArgumentNullException(nameof(device));
            root = root ?? throw new ArgumentNullException(nameof(root));

            var source = _pointer.Captured ?? root.InputHitTest(p);

            if (source != null)
            {
                _pointer.Capture(source);

                var settings = ((IInputRoot?)(source as Interactive)?.GetVisualRoot())?.PlatformSettings;
                if (settings is not null)
                {
                    var doubleClickTime = settings.GetDoubleTapTime(PointerType.Mouse).TotalMilliseconds;
                    var doubleClickSize = settings.GetDoubleTapSize(PointerType.Mouse);

                    if (!_lastClickRect.Contains(p) || timestamp - _lastClickTime > doubleClickTime)
                    {
                        _clickCount = 0;
                    }

                    ++_clickCount;
                    _lastClickTime = timestamp;
                    _lastClickRect = new Rect(p, new Size())
                        .Inflate(new Thickness(doubleClickSize.Width / 2, doubleClickSize.Height / 2));
                }

                _lastMouseDownButton = properties.PointerUpdateKind.GetMouseButton();
                var e = new PointerPressedEventArgs(source, _pointer, (Visual)root, p, timestamp, properties, inputModifiers, _clickCount);
                source.RaiseEvent(e);
                return e.Handled;
            }

            return false;
        }

        private bool MouseMove(IMouseDevice device, ulong timestamp, IInputRoot root, Point p,
            PointerPointProperties properties, KeyModifiers inputModifiers, Lazy<IReadOnlyList<RawPointerPoint>?>? intermediatePoints,
            IInputElement? hitTest)
        {
            device = device ?? throw new ArgumentNullException(nameof(device));
            root = root ?? throw new ArgumentNullException(nameof(root));

            var source = _pointer.CapturedGestureRecognizer?.Target ?? _pointer.Captured ?? hitTest;

            if (source is object)
            {
                var e = new PointerEventArgs(InputElement.PointerMovedEvent, source, _pointer, (Visual)root,
                    p, timestamp, properties, inputModifiers, intermediatePoints);

                if (_pointer.CapturedGestureRecognizer is GestureRecognizer gestureRecognizer)
                    gestureRecognizer.PointerMovedInternal(e);
                else
                    source.RaiseEvent(e);
                return e.Handled;
            }


            return false;
        }

        private bool MouseUp(IMouseDevice device, ulong timestamp, IInputRoot root, Point p, PointerPointProperties props,
            KeyModifiers inputModifiers, IInputElement? hitTest)
        {
            device = device ?? throw new ArgumentNullException(nameof(device));
            root = root ?? throw new ArgumentNullException(nameof(root));

            var source = _pointer.CapturedGestureRecognizer?.Target ?? _pointer.Captured ?? hitTest;

            if (source is not null)
            {
                var e = new PointerReleasedEventArgs(source, _pointer, (Visual)root, p, timestamp, props, inputModifiers,
                    _lastMouseDownButton);

                if (_pointer.CapturedGestureRecognizer is GestureRecognizer gestureRecognizer)
                    gestureRecognizer.PointerReleasedInternal(e);
                else
                    source?.RaiseEvent(e);
                _pointer.Capture(null);
                _pointer.CaptureGestureRecognizer(null);
                _lastMouseDownButton = default;
                return e.Handled;
            }

            return false;
        }

        private bool MouseWheel(IMouseDevice device, ulong timestamp, IInputRoot root, Point p,
            PointerPointProperties props,
            Vector delta, KeyModifiers inputModifiers, IInputElement? hitTest)
        {
            var rawDelta = delta;
            device = device ?? throw new ArgumentNullException(nameof(device));
            root = root ?? throw new ArgumentNullException(nameof(root));

            var source = _pointer.Captured ?? hitTest;

            if (source is not null)
            {
                var e = new PointerWheelEventArgs(source, _pointer, (Visual)root, p, timestamp, props, inputModifiers, delta);

                source?.RaiseEvent(e);
                return e.Handled;
            }

            return false;
        }
        
        private bool GestureMagnify(IMouseDevice device, ulong timestamp, IInputRoot root, Point p,
            PointerPointProperties props, Vector delta, KeyModifiers inputModifiers, IInputElement? hitTest)
        {
            device = device ?? throw new ArgumentNullException(nameof(device));
            root = root ?? throw new ArgumentNullException(nameof(root));

            var source = _pointer.Captured ?? hitTest;

            if (source != null)
            {
                var e = new PointerDeltaEventArgs(Gestures.PointerTouchPadGestureMagnifyEvent, source,
                    _pointer, (Visual)root, p, timestamp, props, inputModifiers, delta);

                source?.RaiseEvent(e);
                return e.Handled;
            }

            return false;
        }
        
        private bool GestureRotate(IMouseDevice device, ulong timestamp, IInputRoot root, Point p,
            PointerPointProperties props, Vector delta, KeyModifiers inputModifiers, IInputElement? hitTest)
        {
            device = device ?? throw new ArgumentNullException(nameof(device));
            root = root ?? throw new ArgumentNullException(nameof(root));

            var source = _pointer.Captured ?? hitTest;

            if (source != null)
            {
                var e = new PointerDeltaEventArgs(Gestures.PointerTouchPadGestureRotateEvent, source,
                    _pointer, (Visual)root, p, timestamp, props, inputModifiers, delta);

                source?.RaiseEvent(e);
                return e.Handled;
            }

            return false;
        }
        
        private bool GestureSwipe(IMouseDevice device, ulong timestamp, IInputRoot root, Point p,
            PointerPointProperties props, Vector delta, KeyModifiers inputModifiers, IInputElement? hitTest)
        {
            device = device ?? throw new ArgumentNullException(nameof(device));
            root = root ?? throw new ArgumentNullException(nameof(root));

            var source = _pointer.Captured ?? hitTest;

            if (source != null)
            {
                var e = new PointerDeltaEventArgs(Gestures.PointerTouchPadGestureSwipeEvent, source, 
                    _pointer, (Visual)root, p, timestamp, props, inputModifiers, delta);

                source?.RaiseEvent(e);
                return e.Handled;
            }

            return false;
        }

        public void Dispose()
        {
            _disposed = true;
            _pointer?.Dispose();
        }
        
        public IPointer? TryGetPointer(RawPointerEventArgs ev)
        {
            return _pointer;
        }
    }
}
