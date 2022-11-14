using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia.Input.Raw;
using Avalonia.Platform;

namespace Avalonia.Input
{
    /// <summary>
    /// Represents a pen/stylus device.
    /// </summary>
    public class PenDevice : IPenDevice, IDisposable
    {
        private readonly Dictionary<long, Pointer> _pointers = new();
        private int _clickCount;
        private Rect _lastClickRect;
        private ulong _lastClickTime;
        private MouseButton _lastMouseDownButton;

        private bool _disposed;

        public void ProcessRawEvent(RawInputEventArgs e)
        {
            if (!e.Handled && e is RawPointerEventArgs margs)
                ProcessRawEvent(margs);
        }

        private void ProcessRawEvent(RawPointerEventArgs e)
        {
            e = e ?? throw new ArgumentNullException(nameof(e));

            if (!_pointers.TryGetValue(e.RawPointerId, out var pointer))
            {
                if (e.Type == RawPointerEventType.LeftButtonUp
                    || e.Type == RawPointerEventType.TouchEnd)
                    return;

                _pointers[e.RawPointerId] = pointer = new Pointer(Pointer.GetNextFreeId(),
                    PointerType.Pen, _pointers.Count == 0);
            }
            
            var props = new PointerPointProperties(e.InputModifiers, e.Type.ToUpdateKind(),
                e.Point.Twist, e.Point.Pressure, e.Point.XTilt, e.Point.YTilt);
            var keyModifiers = e.InputModifiers.ToKeyModifiers();

            bool shouldReleasePointer = false;
            switch (e.Type)
            {
                case RawPointerEventType.LeaveWindow:
                    shouldReleasePointer = true;
                    break;
                case RawPointerEventType.LeftButtonDown:
                    e.Handled = PenDown(pointer, e.Timestamp, e.Root, e.Position, props, keyModifiers, e.InputHitTestResult);
                    break;
                case RawPointerEventType.LeftButtonUp:
                    e.Handled = PenUp(pointer, e.Timestamp, e.Root, e.Position, props, keyModifiers, e.InputHitTestResult);
                    break;
                case RawPointerEventType.Move:
                    e.Handled = PenMove(pointer, e.Timestamp, e.Root, e.Position, props, keyModifiers, e.InputHitTestResult, e.IntermediatePoints);
                    break;
            }

            if (shouldReleasePointer)
            {
                pointer.Dispose();
                _pointers.Remove(e.RawPointerId);
            }
        }

        private bool PenDown(Pointer pointer, ulong timestamp,
            IInputElement root, Point p, PointerPointProperties properties,
            KeyModifiers inputModifiers, IInputElement? hitTest)
        {
            var source = pointer.Captured ?? hitTest;

            if (source != null)
            {
                pointer.Capture(source);
                var settings = AvaloniaLocator.Current.GetService<IPlatformSettings>();
                var doubleClickTime = settings?.GetDoubleTapTime(PointerType.Pen).TotalMilliseconds ?? 500;
                var doubleClickSize = settings?.GetDoubleTapSize(PointerType.Pen) ?? new Size(4, 4);

                if (!_lastClickRect.Contains(p) || timestamp - _lastClickTime > doubleClickTime)
                {
                    _clickCount = 0;
                }

                ++_clickCount;
                _lastClickTime = timestamp;
                _lastClickRect = new Rect(p, new Size())
                    .Inflate(new Thickness(doubleClickSize.Width / 2, doubleClickSize.Height / 2));
                _lastMouseDownButton = properties.PointerUpdateKind.GetMouseButton();
                var e = new PointerPressedEventArgs(source, pointer, root, p, timestamp, properties, inputModifiers, _clickCount);
                source.RaiseEvent(e);
                return e.Handled;
            }

            return false;
        }

        private static bool PenMove(Pointer pointer, ulong timestamp,
            IInputRoot root, Point p, PointerPointProperties properties,
            KeyModifiers inputModifiers, IInputElement? hitTest,
            Lazy<IReadOnlyList<RawPointerPoint>?>? intermediatePoints)
        {
            var source = pointer.Captured ?? hitTest;

            if (source is not null)
            {
                var e = new PointerEventArgs(InputElement.PointerMovedEvent, source, pointer, root,
                    p, timestamp, properties, inputModifiers, intermediatePoints);

                source.RaiseEvent(e);
                return e.Handled;
            }

            return false;
        }

        private bool PenUp(Pointer pointer, ulong timestamp,
            IInputElement root, Point p, PointerPointProperties properties,
            KeyModifiers inputModifiers, IInputElement? hitTest)
        {
            var source = pointer.Captured ?? hitTest;

            if (source is not null)
            {
                var e = new PointerReleasedEventArgs(source, pointer, root, p, timestamp, properties, inputModifiers,
                    _lastMouseDownButton);

                source?.RaiseEvent(e);
                pointer.Capture(null);
                return e.Handled;
            }

            return false;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            var values = _pointers.Values.ToList();
            _pointers.Clear();
            _disposed = true;
            foreach (var p in values)
                p.Dispose();
        }

        public IPointer? TryGetPointer(RawPointerEventArgs ev)
        {
            return _pointers.TryGetValue(ev.RawPointerId, out var pointer)
                ? pointer
                : null;
        }
    }
}
