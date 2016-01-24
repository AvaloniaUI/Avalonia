// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Perspex.Interactivity
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
        private readonly List<ClassEventSubscription> _subscriptions = new List<ClassEventSubscription>();

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

        public void AddClassHandler(Type type, EventHandler<RoutedEventArgs> handler, RoutingStrategies routes)
        {
            _subscriptions.Add(new ClassEventSubscription
            {
                TargetType = type,
                Handler = handler,
                Routes = routes,
            });
        }

        internal void InvokeClassHandlers(object sender, RoutedEventArgs e)
        {
            foreach (var sub in _subscriptions)
            {
                if (sub.TargetType.GetTypeInfo().IsAssignableFrom(sender.GetType().GetTypeInfo()) &&
                    ((e.Route == RoutingStrategies.Direct) || (e.Route & sub.Routes) != 0) &&
                    (!e.Handled || sub.AlsoIfHandled))
                {
                    try
                    {
                        sub.Handler.DynamicInvoke(sender, e);
                    }
                    catch (TargetInvocationException ex)
                    {
                        // Unwrap the inner exception.
                        ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                    }
                }
            }
        }

        private class ClassEventSubscription : EventSubscription
        {
            public Type TargetType { get; set; }
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

        public void AddClassHandler<TTarget>(
            Func<TTarget, Action<TEventArgs>> handler,
            RoutingStrategies routes = RoutingStrategies.Direct | RoutingStrategies.Bubble)
            where TTarget : class
        {
            AddClassHandler(typeof(TTarget), (s, e) => ClassHandlerAdapter<TTarget>(s, e, handler), routes);
        }

        private static void ClassHandlerAdapter<TTarget>(
            object sender,
            RoutedEventArgs e,
            Func<TTarget, Action<TEventArgs>> handler) where TTarget : class
        {
            var target = sender as TTarget;
            var args = e as TEventArgs;

            if (target != null && args != null)
            {
                handler(target)(args);
            }
        }
    }
}
