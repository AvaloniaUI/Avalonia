// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Interactivity;

namespace Perspex.Input
{
    public static class Gestures
    {
        public static readonly RoutedEvent<RoutedEventArgs> TappedEvent = RoutedEvent.Register<RoutedEventArgs>(
            "Tapped",
            RoutingStrategies.Bubble,
            typeof(Gestures));

        public static readonly RoutedEvent<RoutedEventArgs> DoubleTappedEvent = RoutedEvent.Register<RoutedEventArgs>(
            "DoubleTapped",
            RoutingStrategies.Bubble,
            typeof(Gestures));

        private static IInteractive s_lastPress;

        static Gestures()
        {
            InputElement.PointerPressedEvent.RouteFinished.Subscribe(PointerPressed);
            InputElement.PointerReleasedEvent.RouteFinished.Subscribe(PointerReleased);
        }

        private static void PointerPressed(RoutedEventArgs ev)
        {
            if (ev.Route == RoutingStrategies.Bubble)
            {
                var e = (PointerPressedEventArgs)ev;

                if (e.ClickCount <= 1)
                {
                    s_lastPress = e.Source;
                }
                else if (e.ClickCount == 2 && s_lastPress == e.Source)
                {
                    e.Source.RaiseEvent(new RoutedEventArgs(DoubleTappedEvent));
                }
            }
        }

        private static void PointerReleased(RoutedEventArgs ev)
        {
            if (ev.Route == RoutingStrategies.Bubble)
            {
                var e = (PointerReleasedEventArgs)ev;

                if (s_lastPress == e.Source)
                {
                    s_lastPress.RaiseEvent(new RoutedEventArgs(TappedEvent));
                }
            }
        }
    }
}
