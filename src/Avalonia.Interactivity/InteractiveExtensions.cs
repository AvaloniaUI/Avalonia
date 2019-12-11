// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace Avalonia.Interactivity
{
    /// <summary>
    /// Provides extension methods for the <see cref="IInteractive"/> interface.
    /// </summary>
    public static class InteractiveExtensions
    {
        /// <summary>
        /// Gets an observable for a <see cref="RoutedEvent{TEventArgs}"/>.
        /// </summary>
        /// <param name="o">The object to listen for events on.</param>
        /// <param name="routedEvent">The routed event.</param>
        /// <param name="routes">The routing strategies to listen to.</param>
        /// <param name="handledEventsToo">Whether handled events should also be listened for.</param>
        /// <returns>
        /// An observable which fires each time the event is raised.
        /// </returns>
        public static IObservable<TEventArgs> GetObservable<TEventArgs>(
            this IInteractive o,
            RoutedEvent<TEventArgs> routedEvent,
            RoutingStrategies routes = RoutingStrategies.Direct | RoutingStrategies.Bubble,
            bool handledEventsToo = false)
                where TEventArgs : RoutedEventArgs
        {
            return Observable.Create<TEventArgs>(x => o.AddHandler(
                routedEvent, 
                (_, e) => x.OnNext(e), 
                routes,
                handledEventsToo));
        }
    }
}
