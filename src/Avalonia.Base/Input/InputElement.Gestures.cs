using System;
using Avalonia.Interactivity;

namespace Avalonia.Input
{
    public partial class InputElement
    {
        private bool _isContextMenuOnHolding;

        /// <summary>
        /// Defines the IsHoldingEnabled attached property.
        /// </summary>
        public static readonly AttachedProperty<bool> IsHoldingEnabledProperty =
            AvaloniaProperty.RegisterAttached<StyledElement, bool>("IsHoldingEnabled", typeof(InputElement), true);

        /// <summary>
        /// Defines the IsHoldWithMouseEnabled attached property.
        /// </summary>
        public static readonly AttachedProperty<bool> IsHoldWithMouseEnabledProperty =
            AvaloniaProperty.RegisterAttached<StyledElement, bool>("IsHoldWithMouseEnabled", typeof(InputElement), false);

        public static readonly RoutedEvent<PinchEventArgs> PinchEvent =
            RoutedEvent.Register<PinchEventArgs>(
                "Pinch", RoutingStrategies.Bubble, typeof(InputElement));

        public static readonly RoutedEvent<PinchEndedEventArgs> PinchEndedEvent =
            RoutedEvent.Register<PinchEndedEventArgs>(
                "PinchEnded", RoutingStrategies.Bubble, typeof(InputElement));

        public static readonly RoutedEvent<PullGestureEventArgs> PullGestureEvent =
            RoutedEvent.Register<PullGestureEventArgs>(
                "PullGesture", RoutingStrategies.Bubble, typeof(InputElement));

        public static readonly RoutedEvent<PullGestureEndedEventArgs> PullGestureEndedEvent =
            RoutedEvent.Register<PullGestureEndedEventArgs>(
                "PullGestureEnded", RoutingStrategies.Bubble, typeof(InputElement));

        public static readonly RoutedEvent<ScrollGestureEventArgs> ScrollGestureEvent =
            RoutedEvent.Register<ScrollGestureEventArgs>(
                "ScrollGesture", RoutingStrategies.Bubble, typeof(InputElement));

        public static readonly RoutedEvent<ScrollGestureInertiaStartingEventArgs> ScrollGestureInertiaStartingEvent =
            RoutedEvent.Register<ScrollGestureInertiaStartingEventArgs>(
                "ScrollGestureInertiaStarting", RoutingStrategies.Bubble, typeof(InputElement));

        public static readonly RoutedEvent<ScrollGestureEndedEventArgs> ScrollGestureEndedEvent =
            RoutedEvent.Register<ScrollGestureEndedEventArgs>(
                "ScrollGestureEnded", RoutingStrategies.Bubble, typeof(InputElement));

        public static readonly RoutedEvent<PointerDeltaEventArgs> PointerTouchPadGestureMagnifyEvent =
            RoutedEvent.Register<PointerDeltaEventArgs>(
                "PointerTouchPadGestureMagnify", RoutingStrategies.Bubble, typeof(InputElement));

        public static readonly RoutedEvent<PointerDeltaEventArgs> PointerTouchPadGestureRotateEvent =
            RoutedEvent.Register<PointerDeltaEventArgs>(
                "PointerTouchPadGestureRotate", RoutingStrategies.Bubble, typeof(InputElement));

        public static readonly RoutedEvent<PointerDeltaEventArgs> PointerTouchPadGestureSwipeEvent =
            RoutedEvent.Register<PointerDeltaEventArgs>(
                "PointerTouchPadGestureSwipe", RoutingStrategies.Bubble, typeof(InputElement));

        /// <summary>
        /// Defines the <see cref="Tapped"/> event.
        /// </summary>
        public static readonly RoutedEvent<TappedEventArgs> TappedEvent =
            RoutedEvent.Register<InputElement, TappedEventArgs>(
                nameof(Tapped),
                RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="RightTapped"/> event.
        /// </summary>
        public static readonly RoutedEvent<TappedEventArgs> RightTappedEvent =
            RoutedEvent.Register<InputElement, TappedEventArgs>(
                nameof(RightTapped),
                RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="Holding"/> event.
        /// </summary>
        public static readonly RoutedEvent<HoldingRoutedEventArgs> HoldingEvent =
            RoutedEvent.Register<InputElement, HoldingRoutedEventArgs>(
                nameof(Holding),
                RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="DoubleTapped"/> event.
        /// </summary>
        public static readonly RoutedEvent<TappedEventArgs> DoubleTappedEvent =
            RoutedEvent.Register<InputElement, TappedEventArgs>(
                nameof(DoubleTapped),
                RoutingStrategies.Bubble);

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

        public static void AddPinchHandler(Interactive element, EventHandler<PinchEventArgs> handler) =>
            element.AddHandler(PinchEvent, handler);

        public static void AddPinchEndedHandler(Interactive element, EventHandler<PinchEndedEventArgs> handler) =>
            element.AddHandler(PinchEndedEvent, handler);

        public static void AddPullGestureHandler(Interactive element, EventHandler<PullGestureEventArgs> handler) =>
            element.AddHandler(PullGestureEvent, handler);

        public static void AddPullGestureEndedHandler(Interactive element, EventHandler<PullGestureEndedEventArgs> handler) =>
            element.AddHandler(PullGestureEndedEvent, handler);

        public static void RemovePinchHandler(Interactive element, EventHandler<PinchEventArgs> handler) =>
            element.RemoveHandler(PinchEvent, handler);

        public static void RemovePinchEndedHandler(Interactive element, EventHandler<PinchEndedEventArgs> handler) =>
            element.RemoveHandler(PinchEndedEvent, handler);

        public static void RemovePullGestureHandler(Interactive element, EventHandler<PullGestureEventArgs> handler) =>
            element.RemoveHandler(PullGestureEvent, handler);

        public static void RemovePullGestureEndedHandler(Interactive element, EventHandler<PullGestureEndedEventArgs> handler) =>
            element.RemoveHandler(PullGestureEndedEvent, handler);

        public static void AddPointerTouchPadGestureMagnifyHandler(Interactive element, EventHandler<PointerDeltaEventArgs> handler) =>
            element.AddHandler(PointerTouchPadGestureMagnifyEvent, handler);

        public static void AddPointerTouchPadGestureRotateHandler(Interactive element, EventHandler<PointerDeltaEventArgs> handler) =>
            element.AddHandler(PointerTouchPadGestureRotateEvent, handler);

        public static void AddPointerTouchPadGestureSwipeHandler(Interactive element, EventHandler<PointerDeltaEventArgs> handler) =>
            element.AddHandler(PointerTouchPadGestureSwipeEvent, handler);

        public static void AddScrollGestureHandler(Interactive element, EventHandler<ScrollGestureEventArgs> handler) =>
            element.AddHandler(ScrollGestureEvent, handler);

        public static void AddScrollGestureEndedHandler(Interactive element, EventHandler<ScrollGestureEndedEventArgs> handler) =>
            element.AddHandler(ScrollGestureEndedEvent, handler);

        public static void AddScrollGestureInertiaStartingHandler(Interactive element, EventHandler<ScrollGestureInertiaStartingEventArgs> handler) =>
            element.AddHandler(ScrollGestureInertiaStartingEvent, handler);

        public static void RemovePointerTouchPadGestureMagnifyHandler(Interactive element, EventHandler<PointerDeltaEventArgs> handler) =>
            element.RemoveHandler(PointerTouchPadGestureMagnifyEvent, handler);

        public static void RemovePointerTouchPadGestureRotateHandler(Interactive element, EventHandler<PointerDeltaEventArgs> handler) =>
            element.RemoveHandler(PointerTouchPadGestureRotateEvent, handler);

        public static void RemovePointerTouchPadGestureSwipeHandler(Interactive element, EventHandler<PointerDeltaEventArgs> handler) =>
            element.RemoveHandler(PointerTouchPadGestureSwipeEvent, handler);

        public static void RemoveScrollGestureHandler(Interactive element, EventHandler<ScrollGestureEventArgs> handler) =>
            element.RemoveHandler(ScrollGestureEvent, handler);

        public static void RemoveScrollGestureEndedHandler(Interactive element, EventHandler<ScrollGestureEndedEventArgs> handler) =>
            element.RemoveHandler(ScrollGestureEndedEvent, handler);

        public static void RemoveScrollGestureInertiaStartingHandler(Interactive element, EventHandler<ScrollGestureInertiaStartingEventArgs> handler) =>
            element.RemoveHandler(ScrollGestureInertiaStartingEvent, handler);

        /// <summary>
        /// Occurs when a tap gesture occurs on the control.
        /// </summary>
        public event EventHandler<TappedEventArgs>? Tapped
        {
            add { AddHandler(TappedEvent, value); }
            remove { RemoveHandler(TappedEvent, value); }
        }

        /// <summary>
        /// Occurs when a right tap gesture occurs on the control.
        /// </summary>
        public event EventHandler<TappedEventArgs>? RightTapped
        {
            add { AddHandler(RightTappedEvent, value); }
            remove { RemoveHandler(RightTappedEvent, value); }
        }

        /// <summary>
        /// Occurs when a hold gesture occurs on the control.
        /// </summary>
        public event EventHandler<HoldingRoutedEventArgs>? Holding
        {
            add { AddHandler(HoldingEvent, value); }
            remove { RemoveHandler(HoldingEvent, value); }
        }

        /// <summary>
        /// Occurs when a double-tap gesture occurs on the control.
        /// </summary>
        public event EventHandler<TappedEventArgs>? DoubleTapped
        {
            add { AddHandler(DoubleTappedEvent, value); }
            remove { RemoveHandler(DoubleTappedEvent, value); }
        }

        private static void OnPreviewHolding(object? sender, HoldingRoutedEventArgs e)
        {
            if (sender is InputElement inputElement)
            {
                inputElement.RaiseEvent(e);

                if (!e.Handled && e.HoldingState == HoldingState.Started)
                {
                    var contextEvent = new ContextRequestedEventArgs(e.PointerEventArgs);
                    inputElement.RaiseEvent(contextEvent);
                    e.Handled = contextEvent.Handled;

                    if (contextEvent.Handled)
                    {
                        inputElement._isContextMenuOnHolding = true;
                    }
                }
                else if (e.HoldingState == HoldingState.Canceled && inputElement._isContextMenuOnHolding)
                {
                    inputElement.RaiseEvent(new RoutedEventArgs(InputElement.ContextCanceledEvent)
                    {
                        Source = inputElement
                    });

                    inputElement._isContextMenuOnHolding = false;
                }
                else if (e.HoldingState == HoldingState.Completed)
                {
                    inputElement._isContextMenuOnHolding = false;
                }
            }
        }
    }
}
