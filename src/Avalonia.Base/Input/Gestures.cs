using System;
using System.Threading;
using Avalonia.Interactivity;
using Avalonia.Reactive;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    internal static class Gestures
    {
        public static event EventHandler<HoldingRoutedEventArgs>? Holding;
        public static event EventHandler<TappedEventArgs>? Tapped;
        public static event EventHandler<TappedEventArgs>? RightTapped;
        public static event EventHandler<TappedEventArgs>? DoubleTapped;

        private record struct GestureState(GestureStateType Type, IPointer Pointer);
        private enum GestureStateType
        {
            Pending,
            Holding,
            DoubleTapped
        }

        private static GestureState? s_gestureState = null;
        private static readonly WeakReference<object?> s_lastPress = new WeakReference<object?>(null);
        private static Point s_lastPressPoint;
        private static CancellationTokenSource? s_holdCancellationToken;

        static Gestures()
        {
            InputElement.PointerPressedEvent.RouteFinished.Subscribe(PointerPressed);
            InputElement.PointerReleasedEvent.RouteFinished.Subscribe(PointerReleased);
            InputElement.PointerMovedEvent.RouteFinished.Subscribe(PointerMoved);
           //InputElement.PointerCaptureLostEvent.RouteFinished.Subscribe(PointerCaptureLost);
        }

        private static object? GetCaptured(RoutedEventArgs? args)
        {
            if (args is not PointerEventArgs pointerEventArgs)
                return null;
            return pointerEventArgs.Pointer?.Captured ?? pointerEventArgs.Source;
        }

        private static void PointerPressed(RoutedEventArgs ev)
        {
            if (GetCaptured(ev) is not { } source)
            {
                return;
            }

            if (ev.Route == RoutingStrategies.Bubble)
            {
                var e = (PointerPressedEventArgs)ev;
                var visual = (Visual)source;

                if (s_gestureState != null)
                {
                    if (s_gestureState.Value.Type == GestureStateType.Holding && source is Interactive i)
                    {
                        Holding?.Invoke(i, new HoldingRoutedEventArgs(HoldingState.Cancelled, s_lastPressPoint, s_gestureState.Value.Pointer.Type, e));
                    }
                    s_holdCancellationToken?.Cancel();
                    s_holdCancellationToken?.Dispose();
                    s_holdCancellationToken = null;

                    s_gestureState = null;
                }

                if (e.ClickCount % 2 == 1)
                {
                    s_gestureState = new GestureState(GestureStateType.Pending, e.Pointer);
                    s_lastPress.SetTarget(source);
                    s_lastPressPoint = e.GetPosition((Visual)source);
                    s_holdCancellationToken = new CancellationTokenSource();
                    var token = s_holdCancellationToken.Token;
                    var settings = visual.GetPlatformSettings();

                    if (settings != null)
                    {
                        DispatcherTimer.RunOnce(() =>
                        {
                            if (s_gestureState != null && !token.IsCancellationRequested && source is InputElement i && InputElement.GetIsHoldingEnabled(i) &&
                            (e.Pointer.Type != PointerType.Mouse || InputElement.GetIsHoldWithMouseEnabled(i)))
                            {
                                s_gestureState = new GestureState(GestureStateType.Holding, s_gestureState.Value.Pointer);
                                Holding?.Invoke(i, new HoldingRoutedEventArgs(HoldingState.Started, s_lastPressPoint, s_gestureState.Value.Pointer.Type, e));
                            }
                        }, settings.HoldWaitDuration);
                    }
                }
                else if (e.ClickCount % 2 == 0 && e.GetCurrentPoint(visual).Properties.IsLeftButtonPressed)
                {
                    if (s_lastPress.TryGetTarget(out var target) &&
                        target == source &&
                        source is Interactive i)
                    {
                        s_gestureState = new GestureState(GestureStateType.DoubleTapped, e.Pointer);
                        DoubleTapped?.Invoke(i, new TappedEventArgs(InputElement.DoubleTappedEvent, e));
                    }
                }
            }
        }

        private static void PointerReleased(RoutedEventArgs ev)
        {
            if (ev.Route == RoutingStrategies.Bubble)
            {
                var e = (PointerReleasedEventArgs)ev;

                var source = GetCaptured(ev);

                if (s_lastPress.TryGetTarget(out var target) &&
                target == source &&
                e.InitialPressMouseButton is MouseButton.Left or MouseButton.Right &&
                source is Interactive i)
                {
                    var point = e.GetCurrentPoint((Visual)target);
                    var settings = i.GetPlatformSettings();
                    var tapSize = settings?.GetTapSize(point.Pointer.Type) ?? new Size(4, 4);
                    var tapRect = new Rect(s_lastPressPoint, new Size())
                        .Inflate(new Thickness(tapSize.Width, tapSize.Height));

                    if (tapRect.ContainsExclusive(point.Position))
                    {
                        if (s_gestureState?.Type == GestureStateType.Holding)
                        {
                            Holding?.Invoke(i, new HoldingRoutedEventArgs(HoldingState.Completed, s_lastPressPoint, s_gestureState.Value.Pointer.Type, e));

                            RightTapped?.Invoke(i, new TappedEventArgs(InputElement.RightTappedEvent, e));
                        }
                        else if (e.InitialPressMouseButton == MouseButton.Right)
                        {
                            RightTapped?.Invoke(i, new TappedEventArgs(InputElement.RightTappedEvent, e));
                        }
                        //GestureStateType.DoubleTapped needed here to prevent invoking Tapped event when DoubleTapped is called.
                        //This behaviour matches UWP behaviour.
                        else if (s_gestureState?.Type != GestureStateType.DoubleTapped)
                        {
                            Tapped?.Invoke(i, new TappedEventArgs(InputElement.TappedEvent, e));
                        }
                    }
                    s_gestureState = null;
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
                var source = GetCaptured(e);
                if (s_lastPress.TryGetTarget(out var target))
                {
                    if (e.Pointer == s_gestureState?.Pointer && source is Interactive i)
                    {
                        var point = e.GetCurrentPoint((Visual)target);
                        var holdSize = new Size(4, 4);
                        var holdRect = new Rect(s_lastPressPoint, new Size())
                            .Inflate(new Thickness(holdSize.Width, holdSize.Height));

                        if (holdRect.ContainsExclusive(point.Position))
                        {
                            return;
                        }

                        if (s_gestureState.Value.Type == GestureStateType.Holding)
                        {
                            Holding?.Invoke(i, new HoldingRoutedEventArgs(HoldingState.Cancelled, s_lastPressPoint, s_gestureState.Value.Pointer.Type, e));
                        }

                        s_holdCancellationToken?.Cancel();
                        s_holdCancellationToken?.Dispose();
                        s_holdCancellationToken = null;
                        s_gestureState = null;
                    }
                }
            }
        }

       /* private static void PointerCaptureLost(RoutedEventArgs args)
        {
            if (args is PointerCaptureLostEventArgs && s_lastPress.TryGetTarget(out var target))
            {
                if (target == args.Source)
                {
                    if (s_gestureState?.Type == GestureStateType.Holding)
                    {
                        Holding?.Invoke(target, new HoldingRoutedEventArgs(HoldingState.Cancelled, s_lastPressPoint, s_gestureState.Value.Pointer.Type));
                    }

                    s_holdCancellationToken?.Cancel();
                    s_holdCancellationToken?.Dispose();
                    s_holdCancellationToken = null;
                    s_gestureState = null;
                }
            }
        }*/
    }
}
