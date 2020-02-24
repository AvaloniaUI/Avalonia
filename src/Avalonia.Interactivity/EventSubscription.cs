// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Interactivity
{
    internal class EventSubscription
    {
        public EventSubscription(
            Delegate handler,
            RoutingStrategies routes,
            bool handledEventsToo,
            Action<Delegate, object, RoutedEventArgs>? invokeAdapter = null)
        {
            Handler = handler;
            Routes = routes;
            HandledEventsToo = handledEventsToo;
            InvokeAdapter = invokeAdapter;
        }

        public Action<Delegate, object, RoutedEventArgs>? InvokeAdapter { get; }

        public Delegate Handler { get; }

        public RoutingStrategies Routes { get; }

        public bool HandledEventsToo { get; }
    }
}
