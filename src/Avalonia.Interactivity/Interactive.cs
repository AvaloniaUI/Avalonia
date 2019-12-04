// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace Avalonia.Interactivity
{
    /// <summary>
    /// Base class for objects that raise routed events.
    /// </summary>
    public class Interactive : Layoutable, IInteractive
    {
        private Dictionary<RoutedEvent, List<EventSubscription>> _eventHandlers;

        private static readonly Dictionary<Type, HandlerInvokeSignature> s_invokeHandlerCache = new Dictionary<Type, HandlerInvokeSignature>();

        /// <summary>
        /// Gets the interactive parent of the object for bubbling and tunneling events.
        /// </summary>
        IInteractive IInteractive.InteractiveParent => ((IVisual)this).VisualParent as IInteractive;

        private Dictionary<RoutedEvent, List<EventSubscription>> EventHandlers => _eventHandlers ?? (_eventHandlers = new Dictionary<RoutedEvent, List<EventSubscription>>());

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
            Contract.Requires<ArgumentNullException>(routedEvent != null);
            Contract.Requires<ArgumentNullException>(handler != null);

            var subscription = new EventSubscription
            {
                Handler = handler,
                Routes = routes,
                AlsoIfHandled = handledEventsToo,
            };

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
            Contract.Requires<ArgumentNullException>(routedEvent != null);
            Contract.Requires<ArgumentNullException>(handler != null);

            // EventHandler delegate is not covariant, this forces us to create small wrapper
            // that will cast our type erased instance and invoke it.
            Type eventArgsType = routedEvent.EventArgsType;

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

            var subscription = new EventSubscription
            {
                InvokeAdapter = invokeAdapter,
                Handler = handler,
                Routes = routes,
                AlsoIfHandled = handledEventsToo,
            };

            return AddEventSubscription(routedEvent, subscription);
        }

        /// <summary>
        /// Removes a handler for the specified routed event.
        /// </summary>
        /// <param name="routedEvent">The routed event.</param>
        /// <param name="handler">The handler.</param>
        public void RemoveHandler(RoutedEvent routedEvent, Delegate handler)
        {
            Contract.Requires<ArgumentNullException>(routedEvent != null);
            Contract.Requires<ArgumentNullException>(handler != null);

            List<EventSubscription> subscriptions = null;

            if (_eventHandlers?.TryGetValue(routedEvent, out subscriptions) == true)
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
            Contract.Requires<ArgumentNullException>(e != null);

            e.Source = e.Source ?? this;

            if (e.RoutedEvent.RoutingStrategies == RoutingStrategies.Direct)
            {
                e.Route = RoutingStrategies.Direct;
                RaiseEventImpl(e);
                e.RoutedEvent.InvokeRouteFinished(e);
            }

            if ((e.RoutedEvent.RoutingStrategies & RoutingStrategies.Tunnel) != 0)
            {
                TunnelEvent(e);
                e.RoutedEvent.InvokeRouteFinished(e);
            }

            if ((e.RoutedEvent.RoutingStrategies & RoutingStrategies.Bubble) != 0)
            {
                BubbleEvent(e);
                e.RoutedEvent.InvokeRouteFinished(e);
            }
        }

        /// <summary>
        /// Bubbles an event.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void BubbleEvent(RoutedEventArgs e)
        {
            Contract.Requires<ArgumentNullException>(e != null);

            e.Route = RoutingStrategies.Bubble;

            var traverser = HierarchyTraverser<RaiseEventTraverse, NopTraverse>.Create(e);

            traverser.Traverse(this);
        }

        /// <summary>
        /// Tunnels an event.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void TunnelEvent(RoutedEventArgs e)
        {
            Contract.Requires<ArgumentNullException>(e != null);

            e.Route = RoutingStrategies.Tunnel;

            var traverser = HierarchyTraverser<NopTraverse, RaiseEventTraverse>.Create(e);

            traverser.Traverse(this);
        }

        /// <summary>
        /// Carries out the actual invocation of an event on this object.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void RaiseEventImpl(RoutedEventArgs e)
        {
            Contract.Requires<ArgumentNullException>(e != null);

            e.RoutedEvent.InvokeRaised(this, e);

            List<EventSubscription> subscriptions = null;

            if (_eventHandlers?.TryGetValue(e.RoutedEvent, out subscriptions) == true)
            {
                foreach (var sub in subscriptions.ToList())
                {
                    bool correctRoute = (e.Route & sub.Routes) != 0;
                    bool notFinished = !e.Handled || sub.AlsoIfHandled;

                    if (correctRoute && notFinished)
                    {
                        if (sub.InvokeAdapter != null)
                        {
                            sub.InvokeAdapter(sub.Handler, this, e);
                        }
                        else
                        {
                            sub.Handler.DynamicInvoke(this, e);
                        }
                    }
                }
            }
        }

        private List<EventSubscription> GetEventSubscriptions(RoutedEvent routedEvent)
        {
            if (!EventHandlers.TryGetValue(routedEvent, out var subscriptions))
            {
                subscriptions = new List<EventSubscription>();
                EventHandlers.Add(routedEvent, subscriptions);
            }

            return subscriptions;
        }

        private IDisposable AddEventSubscription(RoutedEvent routedEvent, EventSubscription subscription)
        {
            List<EventSubscription> subscriptions = GetEventSubscriptions(routedEvent);

            subscriptions.Add(subscription);

            return new UnsubscribeDisposable(subscriptions, subscription);
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

        private interface ITraverse
        {
            void Execute(IInteractive target, RoutedEventArgs e);
        }

        private struct NopTraverse : ITraverse
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Execute(IInteractive target, RoutedEventArgs e)
            {
            }
        }

        private struct RaiseEventTraverse : ITraverse
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Execute(IInteractive target, RoutedEventArgs e)
            {
                ((Interactive)target).RaiseEventImpl(e);
            }
        }

        /// <summary>
        /// Traverses interactive hierarchy allowing for raising events.
        /// </summary>
        /// <typeparam name="TPreTraverse">Called before parent is traversed.</typeparam>
        /// <typeparam name="TPostTraverse">Called after parent has been traversed.</typeparam>
        private struct HierarchyTraverser<TPreTraverse, TPostTraverse>
            where TPreTraverse : struct, ITraverse
            where TPostTraverse : struct, ITraverse
        {
            private TPreTraverse _preTraverse;
            private TPostTraverse _postTraverse;
            private readonly RoutedEventArgs _args;

            private HierarchyTraverser(TPreTraverse preTraverse, TPostTraverse postTraverse, RoutedEventArgs args)
            {
                _preTraverse = preTraverse;
                _postTraverse = postTraverse;
                _args = args;
            }

            public static HierarchyTraverser<TPreTraverse, TPostTraverse> Create(RoutedEventArgs args)
            {
                return new HierarchyTraverser<TPreTraverse, TPostTraverse>(new TPreTraverse(), new TPostTraverse(), args);
            }

            public void Traverse(IInteractive target)
            {
                _preTraverse.Execute(target, _args);

                IInteractive parent = target.InteractiveParent;

                if (parent != null)
                {
                    Traverse(parent);
                }

                _postTraverse.Execute(target, _args);
            }
        }
    }
}
