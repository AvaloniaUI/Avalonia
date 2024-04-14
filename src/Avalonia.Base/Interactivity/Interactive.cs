using System;
using System.Collections.Generic;
using Avalonia.Layout;
using Avalonia.Utilities;

namespace Avalonia.Interactivity
{
    /// <summary>
    /// Base class for objects that raise routed events.
    /// </summary>
    public class Interactive : Layoutable
    {
        private Dictionary<RoutedEvent, List<EventSubscription>>? _eventHandlers;

        /// <summary>
        /// Gets the interactive parent of the object for bubbling and tunneling events.
        /// </summary>
        internal virtual Interactive? InteractiveParent => VisualParent as Interactive;

        /// <summary>
        /// Adds a handler for the specified routed event.
        /// </summary>
        /// <param name="routedEvent">The routed event.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="routes">The routing strategies to listen to.</param>
        /// <param name="handledEventsToo">Whether handled events should also be listened for.</param>
        public void AddHandler(
            RoutedEvent routedEvent,
            Delegate handler,
            RoutingStrategies routes = RoutingStrategies.Direct | RoutingStrategies.Bubble,
            bool handledEventsToo = false)
        {
            ThrowHelper.ThrowIfNull(routedEvent, nameof(routedEvent));
            ThrowHelper.ThrowIfNull(handler, nameof(handler));

            var subscription = new EventSubscription(handler, routes, handledEventsToo);

            AddEventSubscription(routedEvent, subscription);
        }

        /// <summary>
        /// Adds a handler for the specified routed event.
        /// </summary>
        /// <typeparam name="TEventArgs">The type of the event's args.</typeparam>
        /// <param name="routedEvent">The routed event.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="routes">The routing strategies to listen to.</param>
        /// <param name="handledEventsToo">Whether handled events should also be listened for.</param>
        public void AddHandler<TEventArgs>(
            RoutedEvent<TEventArgs> routedEvent,
            EventHandler<TEventArgs>? handler,
            RoutingStrategies routes = RoutingStrategies.Direct | RoutingStrategies.Bubble,
            bool handledEventsToo = false) where TEventArgs : RoutedEventArgs
        {
            ThrowHelper.ThrowIfNull(routedEvent, nameof(routedEvent));

            if (handler is null)
                return;

            static void InvokeAdapter(Delegate baseHandler, object sender, RoutedEventArgs args)
            {
                var typedHandler = (EventHandler<TEventArgs>)baseHandler;
                var typedArgs = (TEventArgs)args;

                typedHandler(sender, typedArgs);
            }

            var subscription = new EventSubscription(handler, routes, handledEventsToo, (baseHandler, sender, args) => InvokeAdapter(baseHandler, sender, args));

            AddEventSubscription(routedEvent, subscription);
        }

        /// <summary>
        /// Removes a handler for the specified routed event.
        /// </summary>
        /// <param name="routedEvent">The routed event.</param>
        /// <param name="handler">The handler.</param>
        public void RemoveHandler(RoutedEvent routedEvent, Delegate handler)
        {
            ThrowHelper.ThrowIfNull(routedEvent, nameof(routedEvent));
            ThrowHelper.ThrowIfNull(handler, nameof(handler));

            if (_eventHandlers is not null &&
                _eventHandlers.TryGetValue(routedEvent, out var subscriptions))
            {
                for (var i = subscriptions.Count - 1; i >= 0; i--)
                {
                    if (subscriptions[i].Handler == handler)
                    {
                        subscriptions.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        /// Removes a handler for the specified routed event.
        /// </summary>
        /// <typeparam name="TEventArgs">The type of the event's args.</typeparam>
        /// <param name="routedEvent">The routed event.</param>
        /// <param name="handler">The handler.</param>
        public void RemoveHandler<TEventArgs>(RoutedEvent<TEventArgs> routedEvent, EventHandler<TEventArgs>? handler)
            where TEventArgs : RoutedEventArgs
        {
            if (handler is not null)
                RemoveHandler(routedEvent, (Delegate)handler);
        }

        /// <summary>
        /// Raises a routed event.
        /// </summary>
        /// <param name="e">The event args.</param>
        public void RaiseEvent(RoutedEventArgs e)
        {
            ThrowHelper.ThrowIfNull(e, nameof(e));

            if (e.RoutedEvent == null)
            {
                throw new ArgumentException($"Cannot raise an event whose {nameof(RoutedEventArgs.RoutedEvent)} is null.");
            }

            using var route = BuildLightweightEventRoute(e.RoutedEvent);
            route.RaiseEventWithArgs(this, e);
        }

        /// <summary>
        /// Raises a routed event by creating the <see cref="RoutedEventArgs"/> only if they're needed.
        /// Prefer this method over <see cref="RaiseEvent(RoutedEventArgs)"/> to avoid allocations if there's no event listeners.
        /// </summary>
        /// <typeparam name="TContext">The arbitrary context type, used to create the event arguments.</typeparam>
        /// <param name="routedEvent">The routed event.</param>
        /// <param name="eventArgsFactory">A factory used to create the <see cref="RoutedEventArgs"/>.</param>
        /// <param name="context">
        /// An arbitrary object used as a context to create the event arguments.
        /// Use this argument to avoid closures.
        /// </param>
        /// <returns>
        /// The created <see cref="RoutedEventArgs"/> used to raise the event,
        /// or null if the event wasn't raised because it didn't have any listeners.
        /// </returns>
        public RoutedEventArgs? RaiseEvent<TContext>(
            RoutedEvent routedEvent,
            Func<RoutedEvent, TContext, RoutedEventArgs> eventArgsFactory,
            TContext context)
        {
            ThrowHelper.ThrowIfNull(routedEvent, nameof(routedEvent));
            ThrowHelper.ThrowIfNull(eventArgsFactory, nameof(eventArgsFactory));

            using var route = BuildLightweightEventRoute(routedEvent);
            return route.RaiseEvent(this, eventArgsFactory, context);
        }

        /// <summary>
        /// Raises a routed event by creating the <see cref="RoutedEventArgs"/> only if they're needed.
        /// Prefer this method over <see cref="RaiseEvent(RoutedEventArgs)"/> to avoid allocations if there's no event listeners.
        /// </summary>
        /// <param name="routedEvent">The routed event.</param>
        /// <param name="eventArgsFactory">A factory used to create the <see cref="RoutedEventArgs"/>.</param>
        /// <returns>
        /// The created <see cref="RoutedEventArgs"/> used to raise the event,
        /// or null if the event wasn't raised because it didn't have any listeners.
        /// </returns>
        public RoutedEventArgs? RaiseEvent(
            RoutedEvent routedEvent,
            Func<RoutedEvent, RoutedEventArgs> eventArgsFactory)
        {
            ThrowHelper.ThrowIfNull(routedEvent, nameof(routedEvent));
            ThrowHelper.ThrowIfNull(eventArgsFactory, nameof(eventArgsFactory));

            using var route = BuildLightweightEventRoute(routedEvent);
            return route.RaiseEvent(this, eventArgsFactory);
        }

        /// <summary>
        /// Raises a routed event by creating the <see cref="RoutedEventArgs"/> only if they're needed.
        /// Prefer this method over <see cref="RaiseEvent(RoutedEventArgs)"/> to avoid allocations if there's no event listeners.
        /// </summary>
        /// <typeparam name="TEventArgs">The type of event arguments.</typeparam>
        /// <typeparam name="TContext">The arbitrary context type, used to create the event arguments.</typeparam>
        /// <param name="routedEvent">The routed event.</param>
        /// <param name="eventArgsFactory">A factory used to create the <see cref="RoutedEventArgs"/>.</param>
        /// <param name="context">
        /// An arbitrary object used as a context to create the event arguments.
        /// Use this argument to avoid closures.
        /// </param>
        /// <returns>
        /// The created <see cref="RoutedEventArgs"/> used to raise the event,
        /// or null if the event wasn't raised because it didn't have any listeners.
        /// </returns>
        public TEventArgs? RaiseEvent<TEventArgs, TContext>(
            RoutedEvent<TEventArgs> routedEvent,
            Func<RoutedEvent<TEventArgs>, TContext, TEventArgs> eventArgsFactory,
            TContext context)
            where TEventArgs : RoutedEventArgs
        {
            ThrowHelper.ThrowIfNull(routedEvent, nameof(routedEvent));
            ThrowHelper.ThrowIfNull(eventArgsFactory, nameof(eventArgsFactory));

            using var route = BuildLightweightEventRoute(routedEvent);
            return route.RaiseEvent(this, eventArgsFactory, context);
        }

        /// <summary>
        /// Raises a routed event by creating the <see cref="RoutedEventArgs"/> only if they're needed.
        /// Prefer this method over <see cref="RaiseEvent(RoutedEventArgs)"/> to avoid allocations if there's no event listeners.
        /// </summary>
        /// <typeparam name="TEventArgs">The type of event arguments.</typeparam>
        /// <param name="routedEvent">The routed event.</param>
        /// <param name="eventArgsFactory">A factory used to create the <see cref="RoutedEventArgs"/>.</param>
        /// <returns>
        /// The created <see cref="RoutedEventArgs"/> used to raise the event,
        /// or null if the event wasn't raised because it didn't have any listeners.
        /// </returns>
        public TEventArgs? RaiseEvent<TEventArgs>(
            RoutedEvent<TEventArgs> routedEvent,
            Func<RoutedEvent<TEventArgs>, TEventArgs> eventArgsFactory)
            where TEventArgs : RoutedEventArgs
        {
            ThrowHelper.ThrowIfNull(routedEvent, nameof(routedEvent));
            ThrowHelper.ThrowIfNull(eventArgsFactory, nameof(eventArgsFactory));

            using var route = BuildLightweightEventRoute(routedEvent);
            return route.RaiseEvent(this, eventArgsFactory);
        }

        /// <summary>
        /// Raises a routed event by creating the <see cref="RoutedEventArgs"/> only if they're needed.
        /// Prefer this method over <see cref="RaiseEvent(RoutedEventArgs)"/> to avoid allocations if there's no event listeners.
        /// </summary>
        /// <param name="routedEvent">The routed event.</param>
        /// <returns>
        /// The created <see cref="RoutedEventArgs"/> used to raise the event,
        /// or null if the event wasn't raised because it didn't have any listeners.
        /// </returns>
        public RoutedEventArgs? RaiseEvent(RoutedEvent<RoutedEventArgs> routedEvent)
            => RaiseEvent(routedEvent, static e => new RoutedEventArgs(e));

        /// <summary>
        /// Builds an event route for a routed event.
        /// </summary>
        /// <param name="e">The routed event.</param>
        /// <returns>An <see cref="EventRoute"/> describing the route.</returns>
        /// <remarks>
        /// Usually, calling <see cref="RaiseEvent(RoutedEventArgs)"/> is sufficient to raise a routed
        /// event, however there are situations in which the construction of the event args is expensive
        /// and should be avoided if there are no handlers for an event. In these cases you can call
        /// this method to build the event route and check the <see cref="EventRoute.HasHandlers"/>
        /// property to see if there are any handlers registered on the route. If there are, call
        /// <see cref="EventRoute.RaiseEvent(Interactive, RoutedEventArgs)"/> to raise the event.
        ///
        /// Alternatively, use one of the following overloads to avoid creating both the event args
        /// and the event route unless necessary:
        /// <list type="bullet">
        /// <item><term><see cref="RaiseEvent{TContext}(Avalonia.Interactivity.RoutedEvent,Func{Avalonia.Interactivity.RoutedEvent,TContext,Avalonia.Interactivity.RoutedEventArgs},TContext)"/></term></item>
        /// <item><term><see cref="RaiseEvent(Avalonia.Interactivity.RoutedEvent,Func{Avalonia.Interactivity.RoutedEvent,Avalonia.Interactivity.RoutedEventArgs})"/></term></item>
        /// <item><term><see cref="RaiseEvent{TEventArgs,TContext}(Avalonia.Interactivity.RoutedEvent{TEventArgs},Func{Avalonia.Interactivity.RoutedEvent{TEventArgs},TContext,TEventArgs},TContext)"/></term></item>
        /// <item><term><see cref="RaiseEvent{TEventArgs}(Avalonia.Interactivity.RoutedEvent{TEventArgs},Func{Avalonia.Interactivity.RoutedEvent{TEventArgs},TEventArgs})"/></term></item>
        /// <item><term><see cref="RaiseEvent(Avalonia.Interactivity.RoutedEvent{Avalonia.Interactivity.RoutedEventArgs})"/></term></item>
        /// </list>
        /// </remarks>
        protected EventRoute BuildEventRoute(RoutedEvent e)
        {
            ThrowHelper.ThrowIfNull(e, nameof(e));

            return new EventRoute(BuildLightweightEventRoute(e));
        }

        private LightweightEventRoute BuildLightweightEventRoute(RoutedEvent e)
        {
            var result = new LightweightEventRoute(e);
            var hasClassHandlers = e.HasRaisedSubscriptions;

            if (e.RoutingStrategies.HasAnyFlag(RoutingStrategies.Bubble | RoutingStrategies.Tunnel))
            {
                var element = this;

                while (element is not null)
                {
                    if (hasClassHandlers)
                    {
                        result.AddClassHandler(element);
                    }

                    element.AddToEventRoute(e, ref result);
                    element = element.InteractiveParent;
                }
            }
            else
            {
                if (hasClassHandlers)
                {
                    result.AddClassHandler(this);
                }

                AddToEventRoute(e, ref result);
            }

            return result;
        }

        private void AddEventSubscription(RoutedEvent routedEvent, EventSubscription subscription)
        {
            _eventHandlers ??= new Dictionary<RoutedEvent, List<EventSubscription>>();

            if (!_eventHandlers.TryGetValue(routedEvent, out var subscriptions))
            {
                subscriptions = new List<EventSubscription>();
                _eventHandlers.Add(routedEvent, subscriptions);
            }

            subscriptions.Add(subscription);
        }

        private void AddToEventRoute(RoutedEvent routedEvent, ref LightweightEventRoute route)
        {
            if (_eventHandlers is not null &&
                _eventHandlers.TryGetValue(routedEvent, out var subscriptions))
            {
                foreach (var sub in subscriptions)
                {
                    route.Add(this, sub.Handler, sub.Routes, sub.HandledEventsToo, sub.InvokeAdapter);
                }
            }
        }

        private readonly struct EventSubscription
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
}
