// -----------------------------------------------------------------------
// <copyright file="RoutedEvent.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Reflection;

    public enum RoutingStrategy
    {
        Tunnel,
        Bubble,
        Direct,
    }

    public class RoutedEvent
    {
        public RoutedEvent(
            string name, 
            RoutingStrategy routingStrategy,
            Type eventArgsType,
            Type ownerType)
        {
            Contract.Requires<NullReferenceException>(name != null);
            Contract.Requires<NullReferenceException>(eventArgsType != null);
            Contract.Requires<NullReferenceException>(ownerType != null);
            Contract.Requires<InvalidCastException>(typeof(RoutedEventArgs).GetTypeInfo().IsAssignableFrom(eventArgsType.GetTypeInfo()));
            Contract.Requires<InvalidCastException>(typeof(Interactive).GetTypeInfo().IsAssignableFrom(ownerType.GetTypeInfo()));

            this.EventArgsType = eventArgsType;
            this.Name = name;
            this.OwnerType = ownerType;
            this.RoutingStrategy = routingStrategy;
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

        public RoutingStrategy RoutingStrategy 
        { 
            get; 
            private set; 
        }

        public static RoutedEvent<TEventArgs> Register<TOwner, TEventArgs>(
            string name,
            RoutingStrategy routingStrategy)
                where TOwner : Interactive
                where TEventArgs : RoutedEventArgs
        {
            Contract.Requires<NullReferenceException>(name != null);

            return new RoutedEvent<TEventArgs>(name, routingStrategy, typeof(TOwner));
        }
    }

    public class RoutedEvent<TEventArgs> : RoutedEvent
        where TEventArgs : RoutedEventArgs
    {
        public RoutedEvent(string name, RoutingStrategy routingStrategy, Type ownerType)
            : base(name, routingStrategy, typeof(TEventArgs), ownerType)
        {
            Contract.Requires<NullReferenceException>(name != null);
            Contract.Requires<NullReferenceException>(ownerType != null);
            Contract.Requires<InvalidCastException>(typeof(Interactive).GetTypeInfo().IsAssignableFrom(ownerType.GetTypeInfo()));
        }
    }
}
