using System;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    public static class Gestures
    {
        private static bool s_isDoubleTapped = false;
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

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        private static readonly WeakReference<IInteractive> s_lastPress = new WeakReference<IInteractive>(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        static Gestures()
        {
            InputElement.PointerPressedEvent.RouteFinished.Subscribe(PointerPressed);
            InputElement.PointerReleasedEvent.RouteFinished.Subscribe(PointerReleased);
        }

        public static void AddTappedHandler(IInteractive element, EventHandler<RoutedEventArgs> handler)
        {
            element.AddHandler(TappedEvent, handler);
        }

        public static void AddDoubleTappedHandler(IInteractive element, EventHandler<RoutedEventArgs> handler)
        {
            element.AddHandler(DoubleTappedEvent, handler);
        }

        public static void AddRightTappedHandler(IInteractive element, EventHandler<RoutedEventArgs> handler)
        {
            element.AddHandler(RightTappedEvent, handler);
        }

        public static void RemoveTappedHandler(IInteractive element, EventHandler<RoutedEventArgs> handler)
        {
            element.RemoveHandler(TappedEvent, handler);
        }

        public static void RemoveDoubleTappedHandler(IInteractive element, EventHandler<RoutedEventArgs> handler)
        {
            element.RemoveHandler(DoubleTappedEvent, handler);
        }

        public static void RemoveRightTappedHandler(IInteractive element, EventHandler<RoutedEventArgs> handler)
        {
            element.RemoveHandler(RightTappedEvent, handler);
        }

        private static void PointerPressed(RoutedEventArgs ev)
        {
            if (ev.Source is null)
            {
                return;
            }

            if (ev.Route == RoutingStrategies.Bubble)
            {
                var e = (PointerPressedEventArgs)ev;
                var visual = (IVisual)ev.Source;

                if (e.ClickCount <= 1)
                {
                    s_isDoubleTapped = false;
                    s_lastPress.SetTarget(ev.Source);
                }
                else if (e.ClickCount % 2 == 0 && e.GetCurrentPoint(visual).Properties.IsLeftButtonPressed)
                {
                    if (s_lastPress.TryGetTarget(out var target) && target == e.Source)
                    {
                        s_isDoubleTapped = true;
                        e.Source.RaiseEvent(new TappedEventArgs(DoubleTappedEvent, e));
                    }
                }
                else
                {
                    s_isDoubleTapped = false;
                }
            }
        }

        private static void PointerReleased(RoutedEventArgs ev)
        {
            if (ev.Route == RoutingStrategies.Bubble)
            {
                var e = (PointerReleasedEventArgs)ev;

                if (s_lastPress.TryGetTarget(out var target) && target == e.Source)
                {
                    if (e.InitialPressMouseButton == MouseButton.Left || e.InitialPressMouseButton == MouseButton.Right)
                    {
                        if (e.InitialPressMouseButton == MouseButton.Right)
                        {
                            e.Source.RaiseEvent(new TappedEventArgs(RightTappedEvent, e));
                        }
                        //s_isDoubleTapped needed here to prevent invoking Tapped event when DoubleTapped is called.
                        //This behaviour matches UWP behaviour.
                        else if (s_isDoubleTapped == false)
                        {
                            e.Source.RaiseEvent(new TappedEventArgs(TappedEvent, e));
                        }
                    }
                }
            }
        }
    }
}
