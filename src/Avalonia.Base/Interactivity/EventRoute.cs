using System;
using Avalonia.Utilities;

namespace Avalonia.Interactivity
{
    /// <summary>
    /// Holds the route for a routed event and supports raising an event on that route.
    /// </summary>
    public class EventRoute : IDisposable
    {
        private LightweightEventRoute _lightweightEventRoute;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoutedEvent"/> class.
        /// </summary>
        /// <param name="e">The routed event to be raised.</param>
        public EventRoute(RoutedEvent e)
        {
            ThrowHelper.ThrowIfNull(e, nameof(e));

            _lightweightEventRoute = new LightweightEventRoute(e);
        }

        internal EventRoute(LightweightEventRoute lightweightEventRoute)
            => _lightweightEventRoute = lightweightEventRoute;

        /// <summary>
        /// Gets a value indicating whether the route has any handlers.
        /// </summary>
        public bool HasHandlers => _lightweightEventRoute.HasHandlers;

        /// <summary>
        /// Adds a handler to the route.
        /// </summary>
        /// <param name="target">The target on which the event should be raised.</param>
        /// <param name="handler">The handler for the event.</param>
        /// <param name="routes">The routing strategies to listen to.</param>
        /// <param name="handledEventsToo">
        /// If true the handler will be raised even when the routed event is marked as handled.
        /// </param>
        /// <param name="adapter">
        /// An optional adapter which if supplied, will be called with <paramref name="handler"/>
        /// and the parameters for the event. This adapter can be used to avoid calling
        /// `DynamicInvoke` on the handler.
        /// </param>
        public void Add(
            Interactive target,
            Delegate handler,
            RoutingStrategies routes,
            bool handledEventsToo = false,
            Action<Delegate, object, RoutedEventArgs>? adapter = null)
        {
            ThrowHelper.ThrowIfNull(target, nameof(target));
            ThrowHelper.ThrowIfNull(handler, nameof(handler));

            _lightweightEventRoute.Add(target, handler, routes, handledEventsToo, adapter);
        }

        /// <summary>
        /// Adds a class handler to the route.
        /// </summary>
        /// <param name="target">The target on which the event should be raised.</param>
        public void AddClassHandler(Interactive target)
        {
            ThrowHelper.ThrowIfNull(target, nameof(target));

            _lightweightEventRoute.AddClassHandler(target);
        }

        /// <summary>
        /// Raises an event along the route.
        /// </summary>
        /// <param name="source">The event source.</param>
        /// <param name="e">The event args.</param>
        public void RaiseEvent(Interactive source, RoutedEventArgs e)
        {
            ThrowHelper.ThrowIfNull(source, nameof(source));
            ThrowHelper.ThrowIfNull(e, nameof(e));

            _lightweightEventRoute.RaiseEventWithArgs(source, e);
        }

        /// <summary>
        /// Disposes of the event route.
        /// </summary>
        public void Dispose()
            => _lightweightEventRoute.Dispose();
    }
}
