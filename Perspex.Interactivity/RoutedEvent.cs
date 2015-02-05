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

    [Flags]
    public enum RoutingStrategies
    {
        Direct = 0,
        Tunnel = 1,
        Bubble = 2,
    }

    public class RoutedEvent
    {
        public RoutedEvent(
            string name, 
            RoutingStrategies routingStrategies,
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
            this.RoutingStrategies = routingStrategies;
        }

        public event EventHandler<RoutedEventArgs> Raised;

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
                where TOwner : IInteractive
                where TEventArgs : RoutedEventArgs
        {
            Contract.Requires<NullReferenceException>(name != null);

            return new RoutedEvent<TEventArgs>(name, routingStrategy, typeof(TOwner));
        }

        public static RoutedEvent<TEventArgs> Register<TEventArgs>(
            string name,
            RoutingStrategies routingStrategy,
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
        public RoutedEvent(string name, RoutingStrategies routingStrategies, Type ownerType)
            : base(name, routingStrategies, typeof(TEventArgs), ownerType)
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
