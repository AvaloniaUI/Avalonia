using System;
using System.Threading;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.Reactive;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    public static class Gestures
    {
        private static bool s_isDoubleTapped = false;
        private static bool s_isHolding;
        private static CancellationTokenSource? s_holdCancellationToken;

        /// <summary>
        /// Defines the IsHoldingEnabled attached property.
        /// </summary>
        public static readonly AttachedProperty<bool> IsHoldingEnabledProperty =
            AvaloniaProperty.RegisterAttached<StyledElement, bool>("IsHoldingEnabled", typeof(Gestures), true);

        /// <summary>
        /// Defines the IsHoldWithMouseEnabled attached property.
        /// </summary>
        public static readonly AttachedProperty<bool> IsHoldWithMouseEnabledProperty =
            AvaloniaProperty.RegisterAttached<StyledElement, bool>("IsHoldWithMouseEnabled", typeof(Gestures), false);

        public static readonly RoutedEvent<TappedEventArgs> TappedEvent = RoutedEvent.Register<TappedEventArgs>(
            "Tapped",
            RoutingStrategies.Bubble,
            typeof(Gestures));

        public static readonly RoutedEvent<TappedEventArgs> DoubleTappedEvent = RoutedEvent.Register<TappedEventArgs>(
            "DoubleTapped",
            RoutingStrategies.Bubble,
            typeof(Gestures));

        public static readonly RoutedEvent<TappedEventArgs> RightTappedEvent = RoutedEvent.Register<TappedEventArgs>(
            "RightTapped",
            RoutingStrategies.Bubble,
            typeof(Gestures));

        public static readonly RoutedEvent<ScrollGestureEventArgs> ScrollGestureEvent =
            RoutedEvent.Register<ScrollGestureEventArgs>(
                "ScrollGesture", RoutingStrategies.Bubble, typeof(Gestures));

        public static readonly RoutedEvent<ScrollGestureInertiaStartingEventArgs> ScrollGestureInertiaStartingEvent =
            RoutedEvent.Register<ScrollGestureInertiaStartingEventArgs>(
                "ScrollGestureInertiaStarting", RoutingStrategies.Bubble, typeof(Gestures));

        public static readonly RoutedEvent<ScrollGestureEndedEventArgs> ScrollGestureEndedEvent =
            RoutedEvent.Register<ScrollGestureEndedEventArgs>(
                "ScrollGestureEnded", RoutingStrategies.Bubble, typeof(Gestures));
        
        public static readonly RoutedEvent<PointerDeltaEventArgs> PointerTouchPadGestureMagnifyEvent =
            RoutedEvent.Register<PointerDeltaEventArgs>(
                "PointerMagnifyGesture", RoutingStrategies.Bubble, typeof(Gestures));
        
        public static readonly RoutedEvent<PointerDeltaEventArgs> PointerTouchPadGestureRotateEvent =
            RoutedEvent.Register<PointerDeltaEventArgs>(
                "PointerRotateGesture", RoutingStrategies.Bubble, typeof(Gestures));
        
        public static readonly RoutedEvent<PointerDeltaEventArgs> PointerTouchPadGestureSwipeEvent =
            RoutedEvent.Register<PointerDeltaEventArgs>(
                "PointerSwipeGesture", RoutingStrategies.Bubble, typeof(Gestures));

        private static readonly WeakReference<object?> s_lastPress = new WeakReference<object?>(null);
        private static Point s_lastPressPoint;
        private static IPointer? s_lastHeldPointer;

        public static readonly RoutedEvent<PinchEventArgs> PinchEvent =
            RoutedEvent.Register<PinchEventArgs>(
                "PinchEvent", RoutingStrategies.Bubble, typeof(Gestures));

        public static readonly RoutedEvent<PinchEndedEventArgs> PinchEndedEvent =
            RoutedEvent.Register<PinchEndedEventArgs>(
                "PinchEndedEvent", RoutingStrategies.Bubble, typeof(Gestures));

        public static readonly RoutedEvent<PullGestureEventArgs> PullGestureEvent =
            RoutedEvent.Register<PullGestureEventArgs>(
                "PullGesture", RoutingStrategies.Bubble, typeof(Gestures));

        /// <summary>
        /// Occurs when a user performs a press and hold gesture (with a single touch, mouse, or pen/stylus contact).
        /// </summary>
        public static readonly RoutedEvent<HoldingRoutedEventArgs> HoldingEvent =
            RoutedEvent.Register<HoldingRoutedEventArgs>(
                "Holding", RoutingStrategies.Bubble, typeof(Gestures));

        public static readonly RoutedEvent<PullGestureEndedEventArgs> PullGestureEndedEvent =
            RoutedEvent.Register<PullGestureEndedEventArgs>(
                "PullGestureEnded", RoutingStrategies.Bubble, typeof(Gestures));

        public static bool GetIsHoldingEnabled(StyledElement element)
        {
            return element.GetValue(IsHoldingEnabledProperty);
        }
        public static void SetIsHoldingEnabled(StyledElement element, bool value)
        {
            element.SetValue(IsHoldingEnabledProperty, value);
        }

        public static bool GetIsHoldWithMouseEnabled(StyledElement element)
        {
            return element.GetValue(IsHoldWithMouseEnabledProperty);
        }
        public static void SetIsHoldWithMouseEnabled(StyledElement element, bool value)
        {
            element.SetValue(IsHoldWithMouseEnabledProperty, value);
        }

        static Gestures()
        {
            InputElement.PointerPressedEvent.RouteFinished.Subscribe(PointerPressed);
            InputElement.PointerReleasedEvent.RouteFinished.Subscribe(PointerReleased);
            InputElement.PointerMovedEvent.RouteFinished.Subscribe(PointerMoved);
        }

        public static void AddTappedHandler(Interactive element, EventHandler<RoutedEventArgs> handler)
        {
            element.AddHandler(TappedEvent, handler);
        }

        public static void AddDoubleTappedHandler(Interactive element, EventHandler<RoutedEventArgs> handler)
        {
            element.AddHandler(DoubleTappedEvent, handler);
        }

        public static void AddRightTappedHandler(Interactive element, EventHandler<RoutedEventArgs> handler)
        {
            element.AddHandler(RightTappedEvent, handler);
        }

        public static void AddHoldingHandler(Interactive element, EventHandler<HoldingRoutedEventArgs> handler) =>
            element.AddHandler(HoldingEvent, handler);

        public static void AddPinchHandler(Interactive element, EventHandler<PinchEventArgs> handler) =>
            element.AddHandler(PinchEvent, handler);

        public static void AddPinchEndedHandler(Interactive element, EventHandler<PinchEndedEventArgs> handler) =>
            element.AddHandler(PinchEndedEvent, handler);

        public static void AddPullGestureHandler(Interactive element, EventHandler<PullGestureEventArgs> handler) =>
            element.AddHandler(PullGestureEvent, handler);

        public static void AddPullGestureEndedHandler(Interactive element, EventHandler<PullGestureEndedEventArgs> handler) =>
            element.AddHandler(PullGestureEndedEvent, handler);

        public static void AddPointerTouchPadGestureMagnifyHandler(Interactive element, EventHandler<PointerDeltaEventArgs> handler) =>
            element.AddHandler(PointerTouchPadGestureMagnifyEvent, handler);

        public static void AddPointerTouchPadGestureRotateHandler(Interactive element, EventHandler<PointerDeltaEventArgs> handler) =>
            element.AddHandler(PointerTouchPadGestureRotateEvent, handler);

        public static void AddPointerTouchPadGestureSwipeHandler(Interactive element, EventHandler<PointerDeltaEventArgs> handler) =>
            element.AddHandler(PointerTouchPadGestureSwipeEvent, handler);

        public static void AddScrollGestureHandler(Interactive element, EventHandler<RoutedEventArgs> handler) =>
            element.AddHandler(ScrollGestureEvent, handler);

        public static void AddScrollGestureEndedHandler(Interactive element, EventHandler<ScrollGestureEndedEventArgs> handler) =>
            element.AddHandler(ScrollGestureEndedEvent, handler);

        public static void AddScrollGestureInertiaStartingHandler(Interactive element, EventHandler<ScrollGestureInertiaStartingEventArgs> handler) =>
            element.AddHandler(ScrollGestureInertiaStartingEvent, handler);

        public static void RemoveTappedHandler(Interactive element, EventHandler<RoutedEventArgs> handler)
        {
            element.RemoveHandler(TappedEvent, handler);
        }

        public static void RemoveDoubleTappedHandler(Interactive element, EventHandler<RoutedEventArgs> handler)
        {
            element.RemoveHandler(DoubleTappedEvent, handler);
        }

        public static void RemoveRightTappedHandler(Interactive element, EventHandler<RoutedEventArgs> handler)
        {
            element.RemoveHandler(RightTappedEvent, handler);
        }

        public static void RemoveHoldingHandler(Interactive element, EventHandler<RoutedEventArgs> handler) =>
            element.RemoveHandler(HoldingEvent, handler);

        public static void RemovePinchHandler(Interactive element, EventHandler<PinchEventArgs> handler) =>
            element.RemoveHandler(PinchEvent, handler);

        public static void RemovePinchEndedHandler(Interactive element, EventHandler<PinchEndedEventArgs> handler) =>
            element.RemoveHandler(PinchEndedEvent, handler);

        public static void RemovePullGestureHandler(Interactive element, EventHandler<PullGestureEventArgs> handler) =>
            element.RemoveHandler(PullGestureEvent, handler);

        public static void RemovePullGestureEndedHandler(Interactive element, EventHandler<PullGestureEndedEventArgs> handler) =>
            element.RemoveHandler(PullGestureEndedEvent, handler);

        public static void RemovePointerTouchPadGestureMagnifyHandler(Interactive element, EventHandler<PointerDeltaEventArgs> handler) =>
            element.RemoveHandler(PointerTouchPadGestureMagnifyEvent, handler);

        public static void RemovePointerTouchPadGestureRotateHandler(Interactive element, EventHandler<PointerDeltaEventArgs> handler) =>
            element.RemoveHandler(PointerTouchPadGestureRotateEvent, handler);

        public static void RemovePointerTouchPadGestureSwipeHandler(Interactive element, EventHandler<PointerDeltaEventArgs> handler) =>
            element.RemoveHandler(PointerTouchPadGestureSwipeEvent, handler);

        public static void RemoveScrollGestureHandler(Interactive element, EventHandler<ScrollGestureEventArgs> handler) =>
            element.RemoveHandler(ScrollGestureEvent,handler);

        public static void RemoveScrollGestureEndedHandler(Interactive element,EventHandler<ScrollGestureEndedEventArgs> handler) =>
            element.RemoveHandler(ScrollGestureEndedEvent,handler);

        public static void RemoveScrollGestureInertiaStartingHandler(Interactive element, EventHandler<ScrollGestureInertiaStartingEventArgs> handler) =>
            element.RemoveHandler(ScrollGestureInertiaStartingEvent, handler);

        private static void PointerPressed(RoutedEventArgs ev)
        {
            if (ev.Source is null)
            {
                return;
            }

            if (ev.Route == RoutingStrategies.Bubble)
            {
                var e = (PointerPressedEventArgs)ev;
                var visual = (Visual)ev.Source;

                if(s_lastHeldPointer != null)
                {
                    if(s_isHolding && ev.Source is Interactive i)
                    {
                        i.RaiseEvent(new HoldingRoutedEventArgs(HoldingState.Cancelled, s_lastPressPoint, s_lastHeldPointer.Type, e));
                    }
                    s_holdCancellationToken?.Cancel();
                    s_holdCancellationToken?.Dispose();
                    s_holdCancellationToken = null;

                    s_lastHeldPointer = null;
                }

                s_isHolding = false;

                if (e.ClickCount % 2 == 1)
                {
                    s_isDoubleTapped = false;
                    s_lastPress.SetTarget(ev.Source);
                    s_lastHeldPointer = e.Pointer;
                    s_lastPressPoint = e.GetPosition((Visual)ev.Source);
                    s_holdCancellationToken = new CancellationTokenSource();
                    var token = s_holdCancellationToken.Token;
                    var settings = ((IInputRoot?)visual.GetVisualRoot())?.PlatformSettings;

                    if (settings != null)
                    {
                        DispatcherTimer.RunOnce(() =>
                        {
                            if (!token.IsCancellationRequested && e.Source is InputElement i && GetIsHoldingEnabled(i) && (e.Pointer.Type != PointerType.Mouse || GetIsHoldWithMouseEnabled(i)))
                            {
                                s_isHolding = true;
                                i.RaiseEvent(new HoldingRoutedEventArgs(HoldingState.Started, s_lastPressPoint, s_lastHeldPointer.Type, e));
                            }
                        }, settings.HoldWaitDuration);
                    }
                }
                else if (e.ClickCount % 2 == 0 && e.GetCurrentPoint(visual).Properties.IsLeftButtonPressed)
                {
                    if (s_lastPress.TryGetTarget(out var target) && 
                        target == e.Source && 
                        e.Source is Interactive i)
                    {
                        s_isDoubleTapped = true;
                        i.RaiseEvent(new TappedEventArgs(DoubleTappedEvent, e));
                    }
                }
            }
        }

        private static void PointerReleased(RoutedEventArgs ev)
        {
            if (ev.Route == RoutingStrategies.Bubble)
            {
                var e = (PointerReleasedEventArgs)ev;

                if (s_lastPress.TryGetTarget(out var target) &&
                    target == e.Source &&
                    e.InitialPressMouseButton is MouseButton.Left or MouseButton.Right &&
                    e.Source is Interactive i)
                {
                    var point = e.GetCurrentPoint((Visual)target);
                    var settings = ((IInputRoot?)i.GetVisualRoot())?.PlatformSettings;
                    var tapSize = settings?.GetTapSize(point.Pointer.Type) ?? new Size(4, 4);
                    var tapRect = new Rect(s_lastPressPoint, new Size())
                        .Inflate(new Thickness(tapSize.Width, tapSize.Height));

                    if (tapRect.ContainsExclusive(point.Position))
                    {
                        if (s_isHolding)
                        {
                            s_isHolding = false;
                            i.RaiseEvent(new HoldingRoutedEventArgs(HoldingState.Completed, s_lastPressPoint, s_lastHeldPointer!.Type, e));
                        }
                        else if (e.InitialPressMouseButton == MouseButton.Right)
                        {
                            i.RaiseEvent(new TappedEventArgs(RightTappedEvent, e));
                        }
                        //s_isDoubleTapped needed here to prevent invoking Tapped event when DoubleTapped is called.
                        //This behaviour matches UWP behaviour.
                        else if (s_isDoubleTapped == false)
                        {
                            i.RaiseEvent(new TappedEventArgs(TappedEvent, e));
                        }
                    }
                    s_lastHeldPointer = null;
                }

                s_holdCancellationToken?.Cancel();
                s_holdCancellationToken?.Dispose();
                s_holdCancellationToken = null;
            }
        }

        private static void PointerMoved(RoutedEventArgs ev)
        {
            if (ev.Route == RoutingStrategies.Bubble)
            {
                var e = (PointerEventArgs)ev;
                if (s_lastPress.TryGetTarget(out var target))
                {
                    if (e.Pointer == s_lastHeldPointer && ev.Source is Interactive i)
                    {
                        var point = e.GetCurrentPoint((Visual)target);
                        var settings = ((IInputRoot?)i.GetVisualRoot())?.PlatformSettings;
                        var holdSize = new Size(4, 4);
                        var holdRect = new Rect(s_lastPressPoint, new Size())
                            .Inflate(new Thickness(holdSize.Width, holdSize.Height));

                        if (holdRect.ContainsExclusive(point.Position))
                        {
                            return;
                        }

                        if (s_isHolding)
                        {
                            i.RaiseEvent(new HoldingRoutedEventArgs(HoldingState.Cancelled, s_lastPressPoint, s_lastHeldPointer!.Type, e));
                            s_lastHeldPointer = null;
                        }
                    }
                }

                s_holdCancellationToken?.Cancel();
                s_holdCancellationToken?.Dispose();
                s_holdCancellationToken = null;
                s_isHolding = false;
            }
        }
    }
}
