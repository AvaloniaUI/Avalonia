// -----------------------------------------------------------------------
// <copyright file="RoutedEvent.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Interactivity
{
    using System;
    using System.Linq.Expressions;
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
            Contract.Requires<InvalidCastException>(typeof(IInteractive).GetTypeInfo().IsAssignableFrom(ownerType.GetTypeInfo()));

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

        public event EventHandler<RoutedEventArgs> Raised;

        public static RoutedEvent<TEventArgs> Register<TOwner, TEventArgs>(
            string name,
            RoutingStrategy routingStrategy)
                where TOwner : IInteractive
                where TEventArgs : RoutedEventArgs
        {
            Contract.Requires<NullReferenceException>(name != null);

            return new RoutedEvent<TEventArgs>(name, routingStrategy, typeof(TOwner));
        }

        public static RoutedEvent<TEventArgs> Register<TEventArgs>(
            string name,
            RoutingStrategy routingStrategy,
            Type ownerType) 
                where TEventArgs : RoutedEventArgs
        {
            Contract.Requires<NullReferenceException>(name != null);

            return new RoutedEvent<TEventArgs>(name, routingStrategy, ownerType);
        }

        internal void InvokeRaised(object sender, RoutedEventArgs e)
        {
            if (this.Raised != null)
            {
                this.Raised(sender, e);
            }
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
            Contract.Requires<InvalidCastException>(typeof(IInteractive).GetTypeInfo().IsAssignableFrom(ownerType.GetTypeInfo()));
        }

        public void AddClassHandler<TTarget>(Func<TTarget, Action<TEventArgs>> handler) where TTarget : class
        {
            this.Raised += (s, e) =>
            {
                var target = s as TTarget;
                var args = e as TEventArgs;

                if (target != null)
                {
                    handler(target)(args);
                }
            };
        }
    }
}
