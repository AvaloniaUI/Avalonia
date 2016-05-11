// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Subjects;
using System.Reflection;
using System.Runtime.ExceptionServices;

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
        private Subject<Tuple<object, RoutedEventArgs>> _raised = new Subject<Tuple<object, RoutedEventArgs>>();
        private Subject<RoutedEventArgs> _routeFinished = new Subject<RoutedEventArgs>();

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

        public Type EventArgsType
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        public Type OwnerType
        {
            get;
            private set;
        }

        public RoutingStrategies RoutingStrategies
        {
            get;
            private set;
        }

        public IObservable<Tuple<object, RoutedEventArgs>> Raised => _raised;
        public IObservable<RoutedEventArgs> RouteFinished => _routeFinished;

        public static RoutedEvent<TEventArgs> Register<TOwner, TEventArgs>(
            string name,
            RoutingStrategies routingStrategy)
                where TEventArgs : RoutedEventArgs
        {
            Contract.Requires<ArgumentNullException>(name != null);

            return new RoutedEvent<TEventArgs>(name, routingStrategy, typeof(TOwner));
        }

        public static RoutedEvent<TEventArgs> Register<TEventArgs>(
            string name,
            RoutingStrategies routingStrategy,
            Type ownerType)
                where TEventArgs : RoutedEventArgs
        {
            Contract.Requires<ArgumentNullException>(name != null);

            return new RoutedEvent<TEventArgs>(name, routingStrategy, ownerType);
        }

        public IDisposable AddClassHandler(
            Type targetType,
            EventHandler<RoutedEventArgs> handler,
            RoutingStrategies routes = RoutingStrategies.Direct | RoutingStrategies.Bubble,
            bool handledEventsToo = false)
        {
            return Raised.Subscribe(args =>
            {
                var sender = args.Item1;
                var e = args.Item2;

                if (targetType.GetTypeInfo().IsAssignableFrom(sender.GetType().GetTypeInfo()) &&
                    ((e.Route == RoutingStrategies.Direct) || (e.Route & routes) != 0) &&
                    (!e.Handled || handledEventsToo))
                {
                    try
                    {
                        handler.DynamicInvoke(sender, e);
                    }
                    catch (TargetInvocationException ex)
                    {
                        // Unwrap the inner exception.
                        ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                    }
                }
            });
        }

        internal void InvokeRaised(object sender, RoutedEventArgs e)
        {
            _raised.OnNext(Tuple.Create(sender, e));
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

        public IDisposable AddClassHandler<TTarget>(
            Func<TTarget, Action<TEventArgs>> handler,
            RoutingStrategies routes = RoutingStrategies.Direct | RoutingStrategies.Bubble,
            bool handledEventsToo = false)
                where TTarget : class, IInteractive
        {
            EventHandler<RoutedEventArgs> adapter = (sender, e) =>
            {
                var target = sender as TTarget;
                var args = e as TEventArgs;

                if (target != null && args != null)
                {
                    handler(target)(args);
                }
            };

            return AddClassHandler(typeof(TTarget), adapter, routes, handledEventsToo);
        }
    }
}
