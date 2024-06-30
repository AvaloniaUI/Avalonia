using System;
using System.Collections.Generic;
using System.Linq;
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
                var hit = args.InputHitTestResult.firstEnabledAncestor;

                _pointers[args.RawPointerId] = pointer = new Pointer(Pointer.GetNextFreeId(),
                    PointerType.Touch, _pointers.Count == 0);
                pointer.Capture(hit);
            }

            var target = pointer.Captured ?? args.InputHitTestResult.firstEnabledAncestor ?? args.Root;
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

                target.RaiseEvent(
                    InputElement.PointerPressedEvent,
                    static (_, ctx) => new PointerPressedEventArgs(
                        ctx.source,
                        ctx.pointer,
                        ctx.rootVisual,
                        ctx.rootVisualPosition,
                        ctx.timestamp,
                        ctx.properties,
                        ctx.modifiers,
                        ctx.clickCount),
                    (source: target,
                        pointer,
                        rootVisual: (Visual)args.Root,
                        rootVisualPosition: args.Position,
                        timestamp: args.Timestamp,
                        properties: new PointerPointProperties(GetModifiers(args.InputModifiers, true), updateKind, args.Point),
                        modifiers: keyModifier,
                        clickCount: _clickCount));
            }

            else if (args.Type == RawPointerEventType.TouchEnd)
            {
                _pointers.Remove(args.RawPointerId);
                using (pointer)
                {
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

                    target = gestureTarget ?? target;

                    PointerReleasedContext context = (
                        target,
                        pointer,
                        (Visual)args.Root,
                        args.Position,
                        args.Timestamp,
                        new PointerPointProperties(GetModifiers(args.InputModifiers, false), updateKind, args.Point),
                        keyModifier,
                        MouseButton.Left);

                    if (gestureTarget != null)
                    {
                        var eventArgs = CreateEventArgs(InputElement.PointerReleasedEvent, context);
                        pointer.CapturedGestureRecognizer?.PointerReleasedInternal(eventArgs);
                    }
                    else
                    {
                        target.RaiseEvent(InputElement.PointerReleasedEvent, CreateEventArgs, context);
                    }
                }
            }

            else if (args.Type == RawPointerEventType.TouchCancel)
            {
                _pointers.Remove(args.RawPointerId);
                using (pointer)
                {
                    pointer?.Capture(null);
                    pointer?.CaptureGestureRecognizer(null);
                    if (pointer != null)
                        pointer.IsGestureRecognitionSkipped = false;
                }
            }

            else if (args.Type == RawPointerEventType.TouchUpdate)
            {
                target = gestureTarget ?? target;

                PointerMovedContext context = (
                    target,
                    pointer,
                    (Visual)args.Root,
                    args.Position,
                    args.Timestamp,
                    new PointerPointProperties(GetModifiers(args.InputModifiers, true), updateKind, args.Point),
                    keyModifier,
                    args.IntermediatePoints);

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

                if (gestureTarget != null)
                {
                    var eventArgs = CreateEventArgs(InputElement.PointerMovedEvent, context);
                    pointer.CapturedGestureRecognizer?.PointerMovedInternal(eventArgs);
                }
                else
                {
                    target.RaiseEvent(InputElement.PointerMovedEvent, CreateEventArgs, context);
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
