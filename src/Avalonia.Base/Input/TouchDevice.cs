using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.VisualTree;

#pragma warning disable CS0618

namespace Avalonia.Input
{
    /// <summary>
    /// Handles raw touch events
    /// </summary>
    /// <remarks>
    /// This class is supposed to be used on per-toplevel basis, don't use a shared one
    /// </remarks>
    [PrivateApi]
    public class TouchDevice : IPointerDevice, IDisposable
    {
        private readonly Dictionary<long, Pointer> _pointers = new Dictionary<long, Pointer>();
        private bool _disposed;
        private int _clickCount;
        private Rect _lastClickRect;
        private ulong _lastClickTime;

        static RawInputModifiers GetModifiers(RawInputModifiers modifiers, bool isLeftButtonDown)
        {
            var rv = modifiers &= RawInputModifiers.KeyboardMask;
            if (isLeftButtonDown)
                rv |= RawInputModifiers.LeftMouseButton;
            return rv;
        }

        public void ProcessRawEvent(RawInputEventArgs ev)
        {
            if (ev.Handled || _disposed)
                return;
            var args = (RawPointerEventArgs)ev;
            if (!_pointers.TryGetValue(args.RawPointerId, out var pointer))
            {
                if (args.Type == RawPointerEventType.TouchEnd)
                    return;
                var hit = args.InputHitTestResult;

                _pointers[args.RawPointerId] = pointer = new Pointer(Pointer.GetNextFreeId(),
                    PointerType.Touch, _pointers.Count == 0);
                pointer.Capture(hit);
            }

            var target = pointer.Captured ?? args.InputHitTestResult ?? args.Root;
            var gestureTarget = pointer.CapturedGestureRecognizer?.Target;
            var updateKind = args.Type.ToUpdateKind();
            var keyModifier = args.InputModifiers.ToKeyModifiers();

            if (args.Type == RawPointerEventType.TouchBegin)
            {
                if (_pointers.Count > 1)
                {
                    _clickCount = 1;
                    _lastClickTime = 0;
                    _lastClickRect = new Rect();
                }
                else
                {
                    var settings = ((IInputRoot?)(target as Interactive)?.GetVisualRoot())?.PlatformSettings;
                    if (settings is not null)
                    {
                        var doubleClickTime = settings.GetDoubleTapTime(PointerType.Touch).TotalMilliseconds;
                        var doubleClickSize = settings.GetDoubleTapSize(PointerType.Touch);

                        if (!_lastClickRect.Contains(args.Position)
                            || ev.Timestamp - _lastClickTime > doubleClickTime)
                        {
                            _clickCount = 0;
                        }

                        ++_clickCount;
                        _lastClickTime = ev.Timestamp;
                        _lastClickRect = new Rect(args.Position, new Size())
                            .Inflate(new Thickness(doubleClickSize.Width / 2, doubleClickSize.Height / 2));
                    }
                }

                target.RaiseEvent(new PointerPressedEventArgs(target, pointer,
                    (Visual)args.Root, args.Position, ev.Timestamp,
                    new PointerPointProperties(GetModifiers(args.InputModifiers, true), updateKind),
                    keyModifier, _clickCount));
            }

            if (args.Type == RawPointerEventType.TouchEnd)
            {
                _pointers.Remove(args.RawPointerId);
                using (pointer)
                {
                    target = gestureTarget ?? target;
                    var e = new PointerReleasedEventArgs(target, pointer,
                            (Visual)args.Root, args.Position, ev.Timestamp,
                            new PointerPointProperties(GetModifiers(args.InputModifiers, false), updateKind),
                            keyModifier, MouseButton.Left);
                    if (gestureTarget != null)
                    {
                        pointer?.CapturedGestureRecognizer?.PointerReleasedInternal(e);
                    }
                    else
                    {
                        target.RaiseEvent(e);
                    }
                }
            }

            if (args.Type == RawPointerEventType.TouchCancel)
            {
                _pointers.Remove(args.RawPointerId);
                using (pointer)
                {
                    pointer?.Capture(null);
                    pointer?.CaptureGestureRecognizer(null);
                }
            }

            if (args.Type == RawPointerEventType.TouchUpdate)
            {
                target = gestureTarget ?? target;
                var e = new PointerEventArgs(InputElement.PointerMovedEvent, target, pointer!, (Visual)args.Root,
                    args.Position, ev.Timestamp,
                    new PointerPointProperties(GetModifiers(args.InputModifiers, true), updateKind),
                    keyModifier, args.IntermediatePoints);

                if (gestureTarget != null)
                {
                    pointer?.CapturedGestureRecognizer?.PointerMovedInternal(e);
                }
                else
                {
                    target.RaiseEvent(e);
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            var values = _pointers.Values.ToArray();
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
