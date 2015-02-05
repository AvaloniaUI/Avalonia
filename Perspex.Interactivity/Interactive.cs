// -----------------------------------------------------------------------
// <copyright file="Interactive.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Interactivity
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using Perspex.Layout;
    using Perspex.VisualTree;

    public class Interactive : Layoutable, IInteractive
    {
        private Dictionary<RoutedEvent, List<Subscription>> eventHandlers = 
            new Dictionary<RoutedEvent, List<Subscription>>();

        public void AddHandler(
            RoutedEvent routedEvent, 
            Delegate handler,
            RoutingStrategies routes = RoutingStrategies.Direct | RoutingStrategies.Bubble,
            bool handledEventsToo = false)
        {
            Contract.Requires<NullReferenceException>(routedEvent != null);
            Contract.Requires<NullReferenceException>(handler != null);

            List<Subscription> subscriptions;

            if (!this.eventHandlers.TryGetValue(routedEvent, out subscriptions))
            {
                subscriptions = new List<Subscription>();
                this.eventHandlers.Add(routedEvent, subscriptions);
            }

            subscriptions.Add(new Subscription
            {
                Handler = handler,
                Routes = routes,
                AlsoIfHandled = handledEventsToo,
            });
        }

        public IObservable<EventPattern<T>> GetObservable<T>(RoutedEvent<T> routedEvent) where T : RoutedEventArgs
        {
            Contract.Requires<NullReferenceException>(routedEvent != null);

            return Observable.FromEventPattern<T>(
                handler => this.AddHandler(routedEvent, handler),
                handler => this.RemoveHandler(routedEvent, handler));
        }

        public void RemoveHandler(RoutedEvent routedEvent, Delegate handler)
        {
            Contract.Requires<NullReferenceException>(routedEvent != null);
            Contract.Requires<NullReferenceException>(handler != null);

            List<Subscription> subscriptions;

            if (this.eventHandlers.TryGetValue(routedEvent, out subscriptions))
            {
                subscriptions.RemoveAll(x => x.Handler == handler);
            }
        }

        public void RaiseEvent(RoutedEventArgs e)
        {
            Contract.Requires<NullReferenceException>(e != null);

            e.Source = e.Source ?? this;
            e.OriginalSource = e.OriginalSource ?? this;

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

        private void BubbleEvent(RoutedEventArgs e)
        {
            Contract.Requires<NullReferenceException>(e != null);

            e.Route = RoutingStrategies.Bubble;

            foreach (var target in this.GetSelfAndVisualAncestors().OfType<Interactive>())
            {
                target.RaiseEventImpl(e);
            }
        }

        private void TunnelEvent(RoutedEventArgs e)
        {
            Contract.Requires<NullReferenceException>(e != null);

            e.Route = RoutingStrategies.Tunnel;

            foreach (var target in this.GetSelfAndVisualAncestors().OfType<Interactive>().Reverse())
            {
                target.RaiseEventImpl(e);
            }
        }

        private void RaiseEventImpl(RoutedEventArgs e)
        {
            Contract.Requires<NullReferenceException>(e != null);

            List<Subscription> subscriptions;

            if (this.eventHandlers.TryGetValue(e.RoutedEvent, out subscriptions))
            {
                foreach (var sub in subscriptions)
                {
                    bool correctRoute =
                        (e.Route == RoutingStrategies.Direct && sub.Routes == RoutingStrategies.Direct) ||
                        (e.Route != RoutingStrategies.Direct && (e.Route & sub.Routes) != 0);
                    bool notFinished = !e.Handled || sub.AlsoIfHandled;

                    if (correctRoute && notFinished)
                    {
                        sub.Handler.DynamicInvoke(this, e);
                    }
                }
            }
        }

        private class Subscription
        {
            public Delegate Handler { get; set; }

            public RoutingStrategies Routes { get; set; }

            public bool AlsoIfHandled { get; set; }
        }
    }
}
