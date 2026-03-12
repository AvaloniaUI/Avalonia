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

        /// <summary>
        /// Defines the <see cref="Pinch"/> event.
        /// </summary>
        public static readonly RoutedEvent<PinchEventArgs> PinchEvent =
            RoutedEvent.Register<InputElement, PinchEventArgs>(
                nameof(Pinch), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="PinchEnded"/> event.
        /// </summary>
        public static readonly RoutedEvent<PinchEndedEventArgs> PinchEndedEvent =
            RoutedEvent.Register<InputElement, PinchEndedEventArgs>(
                nameof(PinchEnded), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="PullGesture"/> event.
        /// </summary>
        public static readonly RoutedEvent<PullGestureEventArgs> PullGestureEvent =
            RoutedEvent.Register<InputElement, PullGestureEventArgs>(
                nameof(PullGesture), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="PullGestureEnded"/> event.
        /// </summary>
        public static readonly RoutedEvent<PullGestureEndedEventArgs> PullGestureEndedEvent =
            RoutedEvent.Register<InputElement, PullGestureEndedEventArgs>(
                nameof(PullGestureEnded), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="SwipeGesture"/> event.
        /// </summary>
        public static readonly RoutedEvent<SwipeGestureEventArgs> SwipeGestureEvent =
            RoutedEvent.Register<InputElement, SwipeGestureEventArgs>(
                nameof(SwipeGesture), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="SwipeGestureEnded"/> event.
        /// </summary>
        public static readonly RoutedEvent<SwipeGestureEndedEventArgs> SwipeGestureEndedEvent =
            RoutedEvent.Register<InputElement, SwipeGestureEndedEventArgs>(
                nameof(SwipeGestureEnded), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="ScrollGesture"/> event.
        /// </summary>
        public static readonly RoutedEvent<ScrollGestureEventArgs> ScrollGestureEvent =
            RoutedEvent.Register<InputElement, ScrollGestureEventArgs>(
                nameof(ScrollGesture), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="ScrollGestureInertiaStarting"/> event.
        /// </summary>
        public static readonly RoutedEvent<ScrollGestureInertiaStartingEventArgs> ScrollGestureInertiaStartingEvent =
            RoutedEvent.Register<InputElement, ScrollGestureInertiaStartingEventArgs>(
                nameof(ScrollGestureInertiaStarting), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="ScrollGestureEnded"/> event.
        /// </summary>
        public static readonly RoutedEvent<ScrollGestureEndedEventArgs> ScrollGestureEndedEvent =
            RoutedEvent.Register<InputElement, ScrollGestureEndedEventArgs>(
                nameof(ScrollGestureEnded), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="PointerTouchPadGestureMagnify"/> event.
        /// </summary>
        public static readonly RoutedEvent<PointerDeltaEventArgs> PointerTouchPadGestureMagnifyEvent =
            RoutedEvent.Register<InputElement, PointerDeltaEventArgs>(
                nameof(PointerTouchPadGestureMagnify), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="PointerTouchPadGestureRotate"/> event.
        /// </summary>
        public static readonly RoutedEvent<PointerDeltaEventArgs> PointerTouchPadGestureRotateEvent =
            RoutedEvent.Register<InputElement, PointerDeltaEventArgs>(
                nameof(PointerTouchPadGestureRotate), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="PointerTouchPadGestureSwipe"/> event.
        /// </summary>
        public static readonly RoutedEvent<PointerDeltaEventArgs> PointerTouchPadGestureSwipeEvent =
            RoutedEvent.Register<InputElement, PointerDeltaEventArgs>(
                nameof(PointerTouchPadGestureSwipe), RoutingStrategies.Bubble);

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

        /// <summary>
        /// Occurs when a pinch gesture occurs on the control.
        /// </summary>
        public event EventHandler<PinchEventArgs>? Pinch
        {
            add { AddHandler(PinchEvent, value); }
            remove { RemoveHandler(PinchEvent, value); }
        }

        /// <summary>
        /// Occurs when a pinch gesture ends on the control.
        /// </summary>
        public event EventHandler<PinchEndedEventArgs>? PinchEnded
        {
            add { AddHandler(PinchEndedEvent, value); }
            remove { RemoveHandler(PinchEndedEvent, value); }
        }

        /// <summary>
        /// Occurs when a pull gesture occurs on the control.
        /// </summary>
        public event EventHandler<PullGestureEventArgs>? PullGesture
        {
            add { AddHandler(PullGestureEvent, value); }
            remove { RemoveHandler(PullGestureEvent, value); }
        }

        /// <summary>
        /// Occurs when a pull gesture ends on the control.
        /// </summary>
        public event EventHandler<PullGestureEndedEventArgs>? PullGestureEnded
        {
            add { AddHandler(PullGestureEndedEvent, value); }
            remove { RemoveHandler(PullGestureEndedEvent, value); }
        }

        /// <summary>
        /// Occurs when a scroll gesture occurs on the control.
        /// </summary>
        public event EventHandler<ScrollGestureEventArgs>? ScrollGesture
        {
            add { AddHandler(ScrollGestureEvent, value); }
            remove { RemoveHandler(ScrollGestureEvent, value); }
        }

        /// <summary>
        /// Occurs when a scroll gesture inertia starts on the control.
        /// </summary>
        public event EventHandler<ScrollGestureInertiaStartingEventArgs>? ScrollGestureInertiaStarting
        {
            add { AddHandler(ScrollGestureInertiaStartingEvent, value); }
            remove { RemoveHandler(ScrollGestureInertiaStartingEvent, value); }
        }

        /// <summary>
        /// Occurs when a scroll gesture ends on the control.
        /// </summary>
        public event EventHandler<ScrollGestureEndedEventArgs>? ScrollGestureEnded
        {
            add { AddHandler(ScrollGestureEndedEvent, value); }
            remove { RemoveHandler(ScrollGestureEndedEvent, value); }
        }

        /// <summary>
        /// Occurs when a touchpad magnify gesture occurs on the control.
        /// </summary>
        public event EventHandler<PointerDeltaEventArgs>? PointerTouchPadGestureMagnify
        {
            add { AddHandler(PointerTouchPadGestureMagnifyEvent, value); }
            remove { RemoveHandler(PointerTouchPadGestureMagnifyEvent, value); }
        }

        /// <summary>
        /// Occurs when a touchpad rotate gesture occurs on the control.
        /// </summary>
        public event EventHandler<PointerDeltaEventArgs>? PointerTouchPadGestureRotate
        {
            add { AddHandler(PointerTouchPadGestureRotateEvent, value); }
            remove { RemoveHandler(PointerTouchPadGestureRotateEvent, value); }
        }

        /// <summary>
        /// Occurs when a swipe gesture occurs on the control.
        /// </summary>
        public event EventHandler<SwipeGestureEventArgs>? SwipeGesture
        {
            add { AddHandler(SwipeGestureEvent, value); }
            remove { RemoveHandler(SwipeGestureEvent, value); }
        }

        /// <summary>
        /// Occurs when a swipe gesture ends on the control.
        /// </summary>
        public event EventHandler<SwipeGestureEndedEventArgs>? SwipeGestureEnded
        {
            add { AddHandler(SwipeGestureEndedEvent, value); }
            remove { RemoveHandler(SwipeGestureEndedEvent, value); }
        }

        /// <summary>
        /// Occurs when a touchpad swipe gesture occurs on the control.
        /// </summary>
        public event EventHandler<PointerDeltaEventArgs>? PointerTouchPadGestureSwipe
        {
            add { AddHandler(PointerTouchPadGestureSwipeEvent, value); }
            remove { RemoveHandler(PointerTouchPadGestureSwipeEvent, value); }
        }

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
