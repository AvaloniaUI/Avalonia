// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Perspex.Layout;
using Perspex.VisualTree;

namespace Perspex.Interactivity
{
    /// <summary>
    /// Base class for objects that raise routed events.
    /// </summary>
    public class Interactive : Layoutable, IInteractive
    {
        private Dictionary<RoutedEvent, List<EventSubscription>> _eventHandlers =
            new Dictionary<RoutedEvent, List<EventSubscription>>();

        /// <summary>
        /// Gets the interactive parent of the object for bubbling and tunnelling events.
        /// </summary>
        IInteractive IInteractive.InteractiveParent
        {
            get { return ((IVisual)this).VisualParent as IInteractive; }
        }

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
            Contract.Requires<NullReferenceException>(routedEvent != null);
            Contract.Requires<NullReferenceException>(handler != null);

            List<EventSubscription> subscriptions;

            if (!_eventHandlers.TryGetValue(routedEvent, out subscriptions))
            {
                subscriptions = new List<EventSubscription>();
                _eventHandlers.Add(routedEvent, subscriptions);
            }

            var sub = new EventSubscription
            {
                Handler = handler,
                Routes = routes,
                AlsoIfHandled = handledEventsToo,
            };

            subscriptions.Add(sub);

            return Disposable.Create(() => subscriptions.Remove(sub));
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
            return this.AddHandler(routedEvent, (Delegate)handler, routes, handledEventsToo);
        }

        /// <summary>
        /// Removes a handler for the specified routed event.
        /// </summary>
        /// <param name="routedEvent">The routed event.</param>
        /// <param name="handler">The handler.</param>
        public void RemoveHandler(RoutedEvent routedEvent, Delegate handler)
        {
            Contract.Requires<NullReferenceException>(routedEvent != null);
            Contract.Requires<NullReferenceException>(handler != null);

            List<EventSubscription> subscriptions;

            if (_eventHandlers.TryGetValue(routedEvent, out subscriptions))
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
            this.RemoveHandler(routedEvent, (Delegate)handler);
        }

        /// <summary>
        /// Raises a routed event.
        /// </summary>
        /// <param name="e">The event args.</param>
        public void RaiseEvent(RoutedEventArgs e)
        {
            Contract.Requires<NullReferenceException>(e != null);

            e.Source = e.Source ?? this;

            if (e.RoutedEvent.RoutingStrategies == RoutingStrategies.Direct)
            {
                e.Route = RoutingStrategies.Direct;
                this.RaiseEventImpl(e);
            }

            if ((e.RoutedEvent.RoutingStrategies & RoutingStrategies.Tunnel) != 0)
            {
                this.TunnelEvent(e);
            }

            if ((e.RoutedEvent.RoutingStrategies & RoutingStrategies.Bubble) != 0)
            {
                this.BubbleEvent(e);
            }
        }

        /// <summary>
        /// Bubbles an event.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void BubbleEvent(RoutedEventArgs e)
        {
            Contract.Requires<NullReferenceException>(e != null);

            e.Route = RoutingStrategies.Bubble;

            foreach (var target in this.GetBubbleEventRoute())
            {
                ((Interactive)target).RaiseEventImpl(e);
            }
        }

        /// <summary>
        /// Tunnels an event.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void TunnelEvent(RoutedEventArgs e)
        {
            Contract.Requires<NullReferenceException>(e != null);

            e.Route = RoutingStrategies.Tunnel;

            foreach (var target in this.GetTunnelEventRoute())
            {
                ((Interactive)target).RaiseEventImpl(e);
            }
        }

        /// <summary>
        /// Carries out the actual invocation of an event on this object.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void RaiseEventImpl(RoutedEventArgs e)
        {
            Contract.Requires<NullReferenceException>(e != null);

            e.RoutedEvent.InvokeClassHandlers(this, e);

            List<EventSubscription> subscriptions;

            if (_eventHandlers.TryGetValue(e.RoutedEvent, out subscriptions))
            {
                foreach (var sub in subscriptions.ToList())
                {
                    bool correctRoute = (e.Route & sub.Routes) != 0;
                    bool notFinished = !e.Handled || sub.AlsoIfHandled;

                    if (correctRoute && notFinished)
                    {
                        sub.Handler.DynamicInvoke(this, e);
                    }
                }
            }
        }
    }
}
