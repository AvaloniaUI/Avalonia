// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Subjects;
using System.Reflection;

namespace Avalonia.Interactivity
{
    [Flags]
    public enum RoutingStrategies
    {
        Direct = 0x01,
        Tunnel = 0x02,
        Bubble = 0x04,
    }

    public class RoutedEvent
    {
        private readonly Subject<(object, RoutedEventArgs)> _raised = new Subject<(object, RoutedEventArgs)>();
        private readonly Subject<RoutedEventArgs> _routeFinished = new Subject<RoutedEventArgs>();

        public RoutedEvent(
            string name,
            RoutingStrategies routingStrategies,
            Type eventArgsType,
            Type ownerType)
        {
            Contract.Requires<ArgumentNullException>(name != null);
            Contract.Requires<ArgumentNullException>(eventArgsType != null);
            Contract.Requires<ArgumentNullException>(ownerType != null);
            Contract.Requires<InvalidCastException>(typeof(RoutedEventArgs).GetTypeInfo().IsAssignableFrom(eventArgsType.GetTypeInfo()));

            EventArgsType = eventArgsType;
            Name = name;
            OwnerType = ownerType;
            RoutingStrategies = routingStrategies;
        }

        public Type EventArgsType { get; }

        public string Name { get; }

        public Type OwnerType { get; }

        public RoutingStrategies RoutingStrategies { get; }

        public IObservable<(object, RoutedEventArgs)> Raised => _raised;
        public IObservable<RoutedEventArgs> RouteFinished => _routeFinished;

        public static RoutedEvent<TEventArgs> Register<TOwner, TEventArgs>(
            string name,
            RoutingStrategies routingStrategy)
                where TEventArgs : RoutedEventArgs
        {
            Contract.Requires<ArgumentNullException>(name != null);

            var routedEvent = new RoutedEvent<TEventArgs>(name, routingStrategy, typeof(TOwner));
            RoutedEventRegistry.Instance.Register(typeof(TOwner), routedEvent);
            return routedEvent;
        }

        public static RoutedEvent<TEventArgs> Register<TEventArgs>(
            string name,
            RoutingStrategies routingStrategy,
            Type ownerType)
                where TEventArgs : RoutedEventArgs
        {
            Contract.Requires<ArgumentNullException>(name != null);

            var routedEvent = new RoutedEvent<TEventArgs>(name, routingStrategy, ownerType);
            RoutedEventRegistry.Instance.Register(ownerType, routedEvent);
            return routedEvent;
        }

        public IDisposable AddClassHandler(
            Type targetType,
            EventHandler<RoutedEventArgs> handler,
            RoutingStrategies routes = RoutingStrategies.Direct | RoutingStrategies.Bubble,
            bool handledEventsToo = false)
        {
            return Raised.Subscribe(args =>
            {
                (object sender, RoutedEventArgs e) = args;

                if (targetType.IsInstanceOfType(sender) &&
                    (e.Route == RoutingStrategies.Direct || (e.Route & routes) != 0) &&
                    (!e.Handled || handledEventsToo))
                {
                    handler(sender, e);
                }
            });
        }

        internal void InvokeRaised(object sender, RoutedEventArgs e)
        {
            _raised.OnNext((sender, e));
        }

        internal void InvokeRouteFinished(RoutedEventArgs e)
        {
            _routeFinished.OnNext(e);
        }
    }

    public class RoutedEvent<TEventArgs> : RoutedEvent
        where TEventArgs : RoutedEventArgs
    {
        public RoutedEvent(string name, RoutingStrategies routingStrategies, Type ownerType)
            : base(name, routingStrategies, typeof(TEventArgs), ownerType)
        {
            Contract.Requires<ArgumentNullException>(name != null);
            Contract.Requires<ArgumentNullException>(ownerType != null);
        }

        [Obsolete("Use overload taking Action<TTarget, TEventArgs>.")]
        public IDisposable AddClassHandler<TTarget>(
            Func<TTarget, Action<TEventArgs>> handler,
            RoutingStrategies routes = RoutingStrategies.Direct | RoutingStrategies.Bubble,
            bool handledEventsToo = false)
            where TTarget : class, IInteractive
        {
            void Adapter(object sender, RoutedEventArgs e)
            {
                if (sender is TTarget target && e is TEventArgs args)
                {
                    handler(target)(args);
                }
            }

            return AddClassHandler(typeof(TTarget), Adapter, routes, handledEventsToo);
        }

        public IDisposable AddClassHandler<TTarget>(
            Action<TTarget, TEventArgs> handler,
            RoutingStrategies routes = RoutingStrategies.Direct | RoutingStrategies.Bubble,
            bool handledEventsToo = false) where TTarget : class, IInteractive
        {
            void Adapter(object sender, RoutedEventArgs e)
            {
                if (sender is TTarget target && e is TEventArgs args)
                {
                    handler(target, args);
                }
            }

            return AddClassHandler(typeof(TTarget), Adapter, routes, handledEventsToo);
        }
    }
}
