// -----------------------------------------------------------------------
// <copyright file="IInteractive.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Interactivity
{
    using System;

    public interface IInteractive : IVisual
    {
        IDisposable AddHandler(
            RoutedEvent routedEvent,
            Delegate handler,
            RoutingStrategies routes = RoutingStrategies.Direct | RoutingStrategies.Bubble,
            bool handledEventsToo = false);

        IDisposable AddHandler<TEventArgs>(
            RoutedEvent<TEventArgs> routedEvent,
            EventHandler<TEventArgs> handler,
            RoutingStrategies routes = RoutingStrategies.Direct | RoutingStrategies.Bubble,
            bool handledEventsToo = false) where TEventArgs : RoutedEventArgs;

        void RemoveHandler(RoutedEvent routedEvent, Delegate handler);

        void RemoveHandler<TEventArgs>(RoutedEvent<TEventArgs> routedEvent, EventHandler<TEventArgs> handler)
            where TEventArgs : RoutedEventArgs;

        void RaiseEvent(RoutedEventArgs e);
    }
}
