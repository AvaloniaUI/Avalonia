// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace Avalonia.Interactivity
{
    /// <summary>
    /// Base class for objects that raise routed events.
    /// </summary>
    public class Interactive : Layoutable, IInteractive
    {
        private Dictionary<RoutedEvent, List<EventSubscription>>? _eventHandlers;

        private static readonly Dictionary<Type, Action<Delegate, object, RoutedEventArgs>> s_invokeHandlerCache
            = new Dictionary<Type, Action<Delegate, object, RoutedEventArgs>>();

        /// <summary>
        /// Gets the interactive parent of the object for bubbling and tunneling events.
        /// </summary>
        IInteractive? IInteractive.InteractiveParent => ((IVisual)this).VisualParent as IInteractive;

        /// <summary>
        /// Adds a handler for the specified routed event.
        /// </summary>
        /// <param name="routedEvent">The routed event.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="routes">The routing strategies to listen to.</param>
        /// <param name="handledEventsToo">Whether handled events should also be listened for.</param>
        /// <returns>A disposable that terminates the event subscription.</returns>
        public IDisposable AddHandler(
            RoutedEvent routedEvent,
            Delegate handler,
            RoutingStrategies routes = RoutingStrategies.Direct | RoutingStrategies.Bubble,
            bool handledEventsToo = false)
        {
            routedEvent = routedEvent ?? throw new ArgumentNullException(nameof(routedEvent));
            handler = handler ?? throw new ArgumentNullException(nameof(handler));

            var subscription = new EventSubscription(handler, routes, handledEventsToo);
            return AddEventSubscription(routedEvent, subscription);
        }

        /// <summary>
        /// Adds a handler for the specified routed event.
        /// </summary>
        /// <typeparam name="TEventArgs">The type of the event's args.</typeparam>
        /// <param name="routedEvent">The routed event.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="routes">The routing strategies to listen to.</param>
        /// <param name="handledEventsToo">Whether handled events should also be listened for.</param>
        /// <returns>A disposable that terminates the event subscription.</returns>
        public IDisposable AddHandler<TEventArgs>(
            RoutedEvent<TEventArgs> routedEvent,
            EventHandler<TEventArgs> handler,
            RoutingStrategies routes = RoutingStrategies.Direct | RoutingStrategies.Bubble,
            bool handledEventsToo = false) where TEventArgs : RoutedEventArgs
        {
            routedEvent = routedEvent ?? throw new ArgumentNullException(nameof(routedEvent));
            handler = handler ?? throw new ArgumentNullException(nameof(handler));

            // EventHandler delegate is not covariant, this forces us to create small wrapper
            // that will cast our type erased instance and invoke it.
            var eventArgsType = routedEvent.EventArgsType;

            if (!s_invokeHandlerCache.TryGetValue(eventArgsType, out var invokeAdapter))
            {
                void InvokeAdapter(Delegate baseHandler, object sender, RoutedEventArgs args)
                {
                    var typedHandler = (EventHandler<TEventArgs>)baseHandler;
                    var typedArgs = (TEventArgs)args;

                    typedHandler(sender, typedArgs);
                }

                invokeAdapter = InvokeAdapter;

                s_invokeHandlerCache.Add(eventArgsType, invokeAdapter);
            }

            var subscription = new EventSubscription(handler, routes, handledEventsToo, invokeAdapter);
            return AddEventSubscription(routedEvent, subscription);
        }

        /// <summary>
        /// Removes a handler for the specified routed event.
        /// </summary>
        /// <param name="routedEvent">The routed event.</param>
        /// <param name="handler">The handler.</param>
        public void RemoveHandler(RoutedEvent routedEvent, Delegate handler)
        {
            routedEvent = routedEvent ?? throw new ArgumentNullException(nameof(routedEvent));
            handler = handler ?? throw new ArgumentNullException(nameof(handler));

            if (_eventHandlers is object &&
                _eventHandlers.TryGetValue(routedEvent, out var subscriptions) == true)
            {
                subscriptions.RemoveAll(x => x.Handler == handler);
            }
        }

        /// <summary>
        /// Removes a handler for the specified routed event.
        /// </summary>
        /// <typeparam name="TEventArgs">The type of the event's args.</typeparam>
        /// <param name="routedEvent">The routed event.</param>
        /// <param name="handler">The handler.</param>
        public void RemoveHandler<TEventArgs>(RoutedEvent<TEventArgs> routedEvent, EventHandler<TEventArgs> handler)
            where TEventArgs : RoutedEventArgs
        {
            RemoveHandler(routedEvent, (Delegate)handler);
        }

        /// <summary>
        /// Raises a routed event.
        /// </summary>
        /// <param name="e">The event args.</param>
        public void RaiseEvent(RoutedEventArgs e)
        {
            e = e ?? throw new ArgumentNullException(nameof(e));

            if (e.RoutedEvent == null)
            {
                throw new ArgumentException("Cannot raise an event whose RoutedEvent is null.");
            }

            using var route = BuildEventRoute(e.RoutedEvent);
            route.RaiseEvent(this, e);
        }

        void IInteractive.AddToEventRoute(RoutedEvent routedEvent, EventRoute route)
        {
            routedEvent = routedEvent ?? throw new ArgumentNullException(nameof(routedEvent));
            route = route ?? throw new ArgumentNullException(nameof(route));

            if (_eventHandlers != null &&
                _eventHandlers.TryGetValue(routedEvent, out var subscriptions))
            {
                foreach (var sub in subscriptions)
                {
                    route.Add(this, sub.Handler, sub.Routes, sub.HandledEventsToo, sub.InvokeAdapter);
                }
            }
        }

        /// <summary>
        /// Builds an event route for a routed event.
        /// </summary>
        /// <param name="e">The routed event.</param>
        /// <returns>An <see cref="EventRoute"/> describing the route.</returns>
        /// <remarks>
        /// Usually, calling <see cref="RaiseEvent(RoutedEventArgs)"/> is sufficent to raise a routed
        /// event, however there are situations in which the construction of the event args is expensive
        /// and should be avoided if there are no handlers for an event. In these cases you can call
        /// this method to build the event route and check the <see cref="EventRoute.HasHandlers"/>
        /// property to see if there are any handlers registered on the route. If there are, call
        /// <see cref="EventRoute.RaiseEvent(IInteractive, RoutedEventArgs)"/> to raise the event.
        /// </remarks>
        protected EventRoute BuildEventRoute(RoutedEvent e)
        {
            e = e ?? throw new ArgumentNullException(nameof(e));

            var result = new EventRoute(e);
            var hasClassHandlers = e.HasRaisedSubscriptions;

            if (e.RoutingStrategies.HasFlagCustom(RoutingStrategies.Bubble) ||
                e.RoutingStrategies.HasFlagCustom(RoutingStrategies.Tunnel))
            {
                IInteractive? element = this;

                while (element != null)
                {
                    if (hasClassHandlers)
                    {
                        result.AddClassHandler(element);
                    }

                    element.AddToEventRoute(e, result);
                    element = element.InteractiveParent;
                }
            }
            else
            {
                if (hasClassHandlers)
                {
                    result.AddClassHandler(this);
                }

                ((IInteractive)this).AddToEventRoute(e, result);
            }

            return result;
        }

        private IDisposable AddEventSubscription(RoutedEvent routedEvent, EventSubscription subscription)
        {
            _eventHandlers ??= new Dictionary<RoutedEvent, List<EventSubscription>>();

            if (!_eventHandlers.TryGetValue(routedEvent, out var subscriptions))
            {
                subscriptions = new List<EventSubscription>();
                _eventHandlers.Add(routedEvent, subscriptions);
            }

            subscriptions.Add(subscription);

            return new UnsubscribeDisposable(subscriptions, subscription);
        }

        private sealed class EventSubscription
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

        private sealed class UnsubscribeDisposable : IDisposable
        {
            private readonly List<EventSubscription> _subscriptions;
            private readonly EventSubscription _subscription;

            public UnsubscribeDisposable(List<EventSubscription> subscriptions, EventSubscription subscription)
            {
                _subscriptions = subscriptions;
                _subscription = subscription;
            }

            public void Dispose()
            {
                _subscriptions.Remove(_subscription);
            }
        }
    }
}
