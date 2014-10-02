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

    public class Interactive : Layoutable, IInteractive
    {
        private Dictionary<RoutedEvent, List<Delegate>> eventHandlers = new Dictionary<RoutedEvent, List<Delegate>>();

        public void AddHandler(RoutedEvent routedEvent, Delegate handler)
        {
            Contract.Requires<NullReferenceException>(routedEvent != null);
            Contract.Requires<NullReferenceException>(handler != null);

            List<Delegate> delegates;

            if (!this.eventHandlers.TryGetValue(routedEvent, out delegates))
            {
                delegates = new List<Delegate>();
                this.eventHandlers.Add(routedEvent, delegates);
            }

            delegates.Add(handler);
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

            List<Delegate> delegates;

            if (this.eventHandlers.TryGetValue(routedEvent, out delegates))
            {
                delegates.Remove(handler);
            }
        }

        public void RaiseEvent(RoutedEventArgs e)
        {
            Contract.Requires<NullReferenceException>(e != null);

            e.Source = e.Source ?? this;
            e.OriginalSource = e.OriginalSource ?? this;

            if (!e.Handled)
            {
                switch (e.RoutedEvent.RoutingStrategy)
                {
                    case RoutingStrategy.Bubble:
                        this.BubbleEvent(e);
                        break;
                    case RoutingStrategy.Direct:
                        this.RaiseEventImpl(e);
                        break;
                    case RoutingStrategy.Tunnel:
                        this.TunnelEvent(e);
                        break;
                }
            }
        }

        private void BubbleEvent(RoutedEventArgs e)
        {
            Contract.Requires<NullReferenceException>(e != null);

            foreach (var target in this.GetSelfAndVisualAncestors().OfType<Interactive>())
            {
                target.RaiseEventImpl(e);

                if (e.Handled)
                {
                    break;
                }
            }
        }

        private void TunnelEvent(RoutedEventArgs e)
        {
            Contract.Requires<NullReferenceException>(e != null);

            foreach (var target in this.GetSelfAndVisualAncestors().OfType<Interactive>().Reverse())
            {
                target.RaiseEventImpl(e);

                if (e.Handled)
                {
                    break;
                }
            }
        }

        private void RaiseEventImpl(RoutedEventArgs e)
        {
            Contract.Requires<NullReferenceException>(e != null);

            List<Delegate> delegates;

            if (this.eventHandlers.TryGetValue(e.RoutedEvent, out delegates))
            {
                foreach (Delegate handler in delegates)
                {
                    handler.DynamicInvoke(this, e);
                }
            }
        }
    }
}
