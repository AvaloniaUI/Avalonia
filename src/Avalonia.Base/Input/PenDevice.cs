using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using Avalonia.VisualTree;

#pragma warning disable CS0618

namespace Avalonia.Input
{
    using PointerMovedContext = (
        IInputElement source,
        Pointer pointer,
        Visual rootVisual,
        Point rootVisualPosition,
        ulong timestamp,
        PointerPointProperties properties,
        KeyModifiers modifiers,
        Lazy<IReadOnlyList<RawPointerPoint>?>? previousPoints);

    using PointerReleasedContext = (
        IInputElement source,
        IPointer pointer,
        Visual rootVisual,
        Point rootVisualPosition,
        ulong timestamp,
        PointerPointProperties properties,
        KeyModifiers modifiers,
        MouseButton initialPressMouseButton);

    /// <summary>
    /// Represents a pen/stylus device.
    /// </summary>
    [PrivateApi]
    public class PenDevice : IPenDevice, IDisposable
    {
        private readonly Dictionary<long, Pointer> _pointers = new();
        private readonly bool _releasePointerOnPenUp;
        private int _clickCount;
        private Rect _lastClickRect;
        private ulong _lastClickTime;
        private MouseButton _lastMouseDownButton;

        private bool _disposed;

        public PenDevice(bool releasePointerOnPenUp = false)
        {
            _releasePointerOnPenUp = releasePointerOnPenUp;
        }

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
            try
            {
                switch (e.Type)
                {
                    case RawPointerEventType.LeaveWindow:
                        shouldReleasePointer = true;
                        break;
                    case RawPointerEventType.LeftButtonDown:
                        e.Handled = PenDown(pointer, e.Timestamp, e.Root, e.Position, props, keyModifiers, e.InputHitTestResult.firstEnabledAncestor);
                        break;
                    case RawPointerEventType.LeftButtonUp:
                        if (_releasePointerOnPenUp)
                        {
                            shouldReleasePointer = true;
                        }
                        e.Handled = PenUp(pointer, e.Timestamp, e.Root, e.Position, props, keyModifiers, e.InputHitTestResult.firstEnabledAncestor);
                        break;
                    case RawPointerEventType.Move:
                        e.Handled = PenMove(pointer, e.Timestamp, e.Root, e.Position, props, keyModifiers, e.InputHitTestResult.firstEnabledAncestor, e.IntermediatePoints);
                        break;
                }
            }
            finally
            {
                if (shouldReleasePointer)
                {
                    pointer.Dispose();
                    _pointers.Remove(e.RawPointerId);
                }
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
                var settings = ((IInputRoot?)(source as Interactive)?.GetVisualRoot())?.PlatformSettings;
                if (settings is not null)
                {
                    var doubleClickTime = settings.GetDoubleTapTime(PointerType.Pen).TotalMilliseconds;
                    var doubleClickSize = settings.GetDoubleTapSize(PointerType.Pen);

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

                var eventArgs = source.RaiseEvent(
                    InputElement.PointerPressedEvent,
                    static (_, ctx) => new PointerPressedEventArgs(
                        ctx.source,
                        ctx.pointer,
                        ctx.rootVisual,
                        ctx.rootVisualPosition,
                        ctx.timestamp,
                        ctx.properties,
                        ctx.inputModifiers,
                        ctx.clickCount),
                    (source,
                        pointer,
                        rootVisual: (Visual)root,
                        rootVisualPosition: p,
                        timestamp,
                        properties,
                        inputModifiers,
                        clickCount: _clickCount)
                );

                return eventArgs?.Handled ?? false;
            }

            return false;
        }

        private static bool PenMove(Pointer pointer, ulong timestamp,
            IInputRoot root, Point p, PointerPointProperties properties,
            KeyModifiers inputModifiers, IInputElement? hitTest,
            Lazy<IReadOnlyList<RawPointerPoint>?>? intermediatePoints)
        {
            var source = pointer.CapturedGestureRecognizer?.Target ?? pointer.Captured ?? hitTest;

            if (source is not null)
            {
                PointerMovedContext context = (
                    source,
                    pointer,
                    (Visual)root,
                    p,
                    timestamp,
                    properties,
                    inputModifiers,
                    intermediatePoints);

                static PointerEventArgs CreateEventArgs(RoutedEvent<PointerEventArgs> e, PointerMovedContext ctx)
                    => new(
                        e,
                        ctx.source,
                        ctx.pointer,
                        ctx.rootVisual,
                        ctx.rootVisualPosition,
                        ctx.timestamp,
                        ctx.properties,
                        ctx.modifiers,
                        ctx.previousPoints);

                PointerEventArgs? eventArgs;

                if (pointer.CapturedGestureRecognizer is { } gestureRecognizer)
                {
                    eventArgs = CreateEventArgs(InputElement.PointerMovedEvent, context);
                    gestureRecognizer.PointerMovedInternal(eventArgs);
                }
                else
                {
                    eventArgs = source.RaiseEvent(InputElement.PointerMovedEvent, CreateEventArgs, context);
                }

                return eventArgs?.Handled ?? false;
            }

            return false;
        }

        private bool PenUp(Pointer pointer, ulong timestamp,
            IInputElement root, Point p, PointerPointProperties properties,
            KeyModifiers inputModifiers, IInputElement? hitTest)
        {
            var source = pointer.CapturedGestureRecognizer?.Target ?? pointer.Captured ?? hitTest;

            if (source is not null)
            {
                PointerReleasedContext context = (
                    source,
                    pointer,
                    (Visual)root,
                    p,
                    timestamp,
                    properties,
                    inputModifiers,
                    _lastMouseDownButton);

                static PointerReleasedEventArgs CreateEventArgs(RoutedEvent<PointerReleasedEventArgs> e, PointerReleasedContext ctx)
                    => new(
                        ctx.source,
                        ctx.pointer,
                        ctx.rootVisual,
                        ctx.rootVisualPosition,
                        ctx.timestamp,
                        ctx.properties,
                        ctx.modifiers,
                        ctx.initialPressMouseButton);

                PointerReleasedEventArgs? eventArgs;

                try
                {
                    if (pointer.CapturedGestureRecognizer is { } gestureRecognizer)
                    {
                        eventArgs = CreateEventArgs(InputElement.PointerReleasedEvent, context);
                        gestureRecognizer.PointerReleasedInternal(eventArgs);
                    }
                    else
                    {
                        eventArgs = source.RaiseEvent(InputElement.PointerReleasedEvent, CreateEventArgs, context);
                    }
                }
                finally
                {
                    pointer.Capture(null);
                    pointer.CaptureGestureRecognizer(null);
                    pointer.IsGestureRecognitionSkipped = false;
                    _lastMouseDownButton = default;
                }

                return eventArgs?.Handled ?? false;
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
