// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Interactivity
{
    internal delegate void HandlerInvokeSignature(Delegate baseHandler, object sender, RoutedEventArgs args);

    internal class EventSubscription
    {
        public EventSubscription(
            Delegate handler,
            RoutingStrategies routes,
            bool handledEventsToo,
            HandlerInvokeSignature? invokeAdapter = null)
        {
            Handler = handler;
            Routes = routes;
            HandledEventsToo = handledEventsToo;
            InvokeAdapter = invokeAdapter;
        }

        public HandlerInvokeSignature? InvokeAdapter { get; }

        public Delegate Handler { get; }

        public RoutingStrategies Routes { get; }

        public bool HandledEventsToo { get; }
    }
}
