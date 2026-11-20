using System.Diagnostics;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;

namespace Avalonia.Input;

public partial class MouseDevice
{
    private readonly TouchPadGestureTracker _magnifyTracker = new(
        InputElement.PointerTouchPadGestureMagnifyStartedEvent,
        InputElement.PointerTouchPadGestureMagnifyEvent,
        InputElement.PointerTouchPadGestureMagnifyEndedEvent);

    private readonly TouchPadGestureTracker _rotateTracker = new(
        InputElement.PointerTouchPadGestureRotateStartedEvent,
        InputElement.PointerTouchPadGestureRotateEvent,
        InputElement.PointerTouchPadGestureRotateEndedEvent);

    private readonly TouchPadGestureTracker _swipeTracker = new(
        InputElement.PointerTouchPadGestureSwipeStartedEvent,
        InputElement.PointerTouchPadGestureSwipeEvent,
        InputElement.PointerTouchPadGestureSwipeEndedEvent);

    private readonly WheelGestureTracker _wheelTracker = new();

    private bool ProcessGesture(
        GestureTracker tracker,
        RawPointerEventArgs e,
        PointerPointProperties props,
        KeyModifiers modifiers,
        Vector delta,
        RawPointerGesturePhase phase,
        bool isInertial)
        => tracker.Process(new GestureInput(
            _pointer,
            e.Root,
            e.Position,
            e.Timestamp,
            props,
            modifiers,
            delta,
            phase,
            isInertial,
            e.InputHitTestResult.firstEnabledAncestor));

    private void CancelGestures()
    {
        _magnifyTracker.Cancel();
        _rotateTracker.Cancel();
        _swipeTracker.Cancel();
        _wheelTracker.Cancel();
    }

    private static bool IsAttachedTo(IInputElement element, IInputRoot root)
        => element is Visual visual && visual.GetInputRoot() == root;

    private static bool Raise(IInputElement target, RoutedEventArgs e)
    {
        target.RaiseEvent(e);
        return e.Handled;
    }

    private readonly struct GestureInput(
        Pointer pointer,
        IInputRoot root,
        Point position,
        ulong timestamp,
        PointerPointProperties properties,
        KeyModifiers modifiers,
        Vector delta,
        RawPointerGesturePhase phase,
        bool isInertial,
        IInputElement? hitTest)
    {
        public Pointer Pointer { get; } = pointer;
        public IInputRoot Root { get; } = root;
        public Point Position { get; } = position;
        public ulong Timestamp { get; } = timestamp;
        public PointerPointProperties Properties { get; } = properties;
        public KeyModifiers Modifiers { get; } = modifiers;
        public Vector Delta { get; } = delta;
        public RawPointerGesturePhase Phase { get; } = phase;
        public bool IsInertial { get; } = isInertial;
        public IInputElement? HitTest { get; } = hitTest;
    }

    /// <summary>
    /// Raises the events of one kind of gesture.
    /// </summary>
    /// <remarks>
    /// All the events of the gesture are raised on the element that received the started event
    /// (the gesture is "latched" to it), as long as it stays in the visual tree.
    /// </remarks>
    private abstract class GestureTracker
    {
        private GestureLatch? _latch;

        public virtual bool Process(in GestureInput input)
        {
            switch (input.Phase)
            {
                case RawPointerGesturePhase.Started:
                {
                    Cancel();

                    var source = GetStartTarget(input);
                    if (source is null)
                        return false;

                    _latch = new GestureLatch(source, input);

                    var handled = Raise(source, CreateStartedArgs(input, source));

                    if (input.Delta != default)
                        handled |= Raise(source, CreateDeltaArgs(input, source));

                    return handled;
                }

                case RawPointerGesturePhase.Ended:
                case RawPointerGesturePhase.Canceled:
                {
                    var latch = GetLatch(input);

                    if (latch is null)
                    {
                        // No matching started event was raised: only raise the remaining delta, if any.
                        var target = input.Pointer.Captured ?? input.HitTest;
                        return input.Delta != default && target is not null
                            && Raise(target, CreateDeltaArgs(input, target));
                    }

                    var handled = input.Delta != default && Raise(latch.Target, CreateDeltaArgs(input, latch.Target));

                    _latch = null;
                    var isCanceled = input.Phase == RawPointerGesturePhase.Canceled;

                    handled |= Raise(latch.Target, CreateEndedArgs(input, latch, isCanceled));

                    OnEnded(latch, isCanceled);
                    return handled;
                }

                case RawPointerGesturePhase.None:
                case RawPointerGesturePhase.Updated:
                {
                    if (input.Delta == default)
                        return false;

                    var source = GetLatch(input)?.Target ?? input.Pointer.Captured ?? input.HitTest;
                    return source is not null && Raise(source, CreateDeltaArgs(input, source));
                }

                default:
                    Debug.Fail($"Unexpected phase {input.Phase}");
                    return false;
            }
        }

        public void Cancel()
        {
            if (_latch is not { } latch)
                return;

            _latch = null;

            if (IsAttachedTo(latch.Target, latch.Root))
                Raise(latch.Target, CreateEndedArgs(latch.LastInput, latch, isCanceled: true));

            OnEnded(latch, isCanceled: true);
        }

        protected virtual IInputElement? GetStartTarget(in GestureInput input)
            => input.Pointer.Captured ?? input.HitTest;

        protected virtual void OnEnded(GestureLatch latch, bool isCanceled)
        {
        }

        protected abstract RoutedEventArgs CreateStartedArgs(in GestureInput input, IInputElement source);

        protected abstract RoutedEventArgs CreateDeltaArgs(in GestureInput input, IInputElement source);

        protected abstract RoutedEventArgs CreateEndedArgs(in GestureInput input, GestureLatch latch, bool isCanceled);

        private GestureLatch? GetLatch(in GestureInput input)
        {
            if (_latch is not { } latch || latch.Root != input.Root)
                return null;

            // The element left the visual tree during the gesture.
            if (!IsAttachedTo(latch.Target, input.Root))
            {
                _latch = null;
                return null;
            }

            latch.LastInput = input;
            return latch;
        }
    }

    private sealed class TouchPadGestureTracker(
        RoutedEvent<PointerTouchPadGestureStartedEventArgs> startedEvent,
        RoutedEvent<PointerDeltaEventArgs> deltaEvent,
        RoutedEvent<PointerTouchPadGestureEndedEventArgs> endedEvent)
        : GestureTracker
    {
        protected override RoutedEventArgs CreateStartedArgs(in GestureInput input, IInputElement source)
            => new PointerTouchPadGestureStartedEventArgs(
                startedEvent,
                source,
                input.Pointer,
                input.Root.RootElement,
                input.Position,
                input.Timestamp,
                input.Properties,
                input.Modifiers);

        protected override RoutedEventArgs CreateDeltaArgs(in GestureInput input, IInputElement source)
            => new PointerDeltaEventArgs(
                deltaEvent,
                source,
                input.Pointer,
                input.Root.RootElement,
                input.Position,
                input.Timestamp,
                input.Properties,
                input.Modifiers,
                input.Delta);

        protected override RoutedEventArgs CreateEndedArgs(in GestureInput input, GestureLatch latch, bool isCanceled)
            => new PointerTouchPadGestureEndedEventArgs(
                endedEvent,
                latch.Target,
                input.Pointer,
                input.Root.RootElement,
                input.Position,
                input.Timestamp,
                input.Properties,
                input.Modifiers)
            {
                IsCanceled = isCanceled
            };
    }

    private sealed class WheelGestureTracker : GestureTracker
    {
        private IInputElement? _lastNonInertialTarget;
        private IInputElement? _currentInertiaCandidate;

        public override bool Process(in GestureInput input)
        {
            _currentInertiaCandidate = _lastNonInertialTarget;
            _lastNonInertialTarget = null;

            try
            {
                return base.Process(input);
            }
            finally
            {
                _currentInertiaCandidate = null;
            }
        }

        protected override IInputElement? GetStartTarget(in GestureInput input)
            => input.IsInertial && _currentInertiaCandidate is not null && IsAttachedTo(_currentInertiaCandidate, input.Root) ?
                _currentInertiaCandidate :
                base.GetStartTarget(input);

        protected override void OnEnded(GestureLatch latch, bool isCanceled)
            => _lastNonInertialTarget = latch.IsInertial || isCanceled ? null : latch.Target;

        protected override RoutedEventArgs CreateStartedArgs(in GestureInput input, IInputElement source)
            => new PointerWheelGestureStartedEventArgs(
                source,
                input.Pointer,
                input.Root.RootElement,
                input.Position,
                input.Timestamp,
                input.Properties,
                input.Modifiers,
                input.IsInertial);

        protected override RoutedEventArgs CreateDeltaArgs(in GestureInput input, IInputElement source)
            => new PointerWheelEventArgs(
                source,
                input.Pointer,
                input.Root.RootElement,
                input.Position,
                input.Timestamp,
                input.Properties,
                input.Modifiers,
                input.Delta)
            {
                IsInertial = input.IsInertial
            };

        protected override RoutedEventArgs CreateEndedArgs(in GestureInput input, GestureLatch latch, bool isCanceled)
            => new PointerWheelGestureEndedEventArgs(
                latch.Target,
                input.Pointer,
                input.Root.RootElement,
                input.Position,
                input.Timestamp,
                input.Properties,
                input.Modifiers)
            {
                IsInertial = latch.IsInertial,
                IsCanceled = isCanceled
            };
    }

    /// <summary>
    /// Stores the element receiving all the events of a gesture, from its started event to its ended event.
    /// </summary>
    private sealed class GestureLatch(IInputElement target, GestureInput startInput)
    {
        public IInputElement Target { get; } = target;
        public IInputRoot Root { get; } = startInput.Root;
        public bool IsInertial { get; } = startInput.IsInertial;
        public GestureInput LastInput { get; set; } = startInput;
    }
}
