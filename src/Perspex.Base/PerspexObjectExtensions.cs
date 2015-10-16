// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Linq;

namespace Perspex
{
    /// <summary>
    /// Provides extension methods for <see cref="PerspexObject"/> and related classes.
    /// </summary>
    public static class PerspexObjectExtensions
    {
        /// <summary>
        /// Subscribes to a property changed notifications for changes that originate from a
        /// <typeparamref name="TTarget"/>.
        /// </summary>
        /// <typeparam name="TTarget">The type of the property change sender.</typeparam>
        /// <param name="observable">The property changed observable.</param>
        /// <param name="action">
        /// The method to call. The parameters are the sender and the event args.
        /// </param>
        /// <returns>A disposable that can be used to terminate the subscription.</returns>
        public static IDisposable AddClassHandler<TTarget>(
            this IObservable<PerspexPropertyChangedEventArgs> observable,
            Action<TTarget, PerspexPropertyChangedEventArgs> action)
            where TTarget : PerspexObject
        {
            return observable.Subscribe(e =>
            {
                if (e.Sender is TTarget)
                {
                    action((TTarget)e.Sender, e);
                }
            });
        }

        /// <summary>
        /// Subscribes to a property changed notifications for changes that originate from a
        /// <typeparamref name="TTarget"/>.
        /// </summary>
        /// <typeparam name="TTarget">The type of the property change sender.</typeparam>
        /// <param name="observable">The property changed observable.</param>
        /// <param name="handler">Given a TTarget, returns the handler.</param>
        /// <returns>A disposable that can be used to terminate the subscription.</returns>
        public static IDisposable AddClassHandler<TTarget>(
            this IObservable<PerspexPropertyChangedEventArgs> observable,
            Func<TTarget, Action<PerspexPropertyChangedEventArgs>> handler)
            where TTarget : class
        {
            return observable.Subscribe(e => SubscribeAdapter(e, handler));
        }

        /// <summary>
        /// Finds a registered property on a <see cref="PerspexObject"/> by name.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="name">
        /// The property name. If an attached property it should be in the form 
        /// "OwnerType.PropertyName".
        /// </param>
        /// <returns>
        /// The registered property or null if no matching property found.
        /// </returns>
        public static PerspexProperty FindRegistered(this PerspexObject o, string name)
        {
            Contract.Requires<ArgumentNullException>(o != null);
            Contract.Requires<ArgumentNullException>(name != null);

            var parts = name.Split('.');

            if (parts.Length < 1 || parts.Length > 2)
            {
                throw new ArgumentException("Invalid property name.");
            }

            if (parts.Length == 1)
            {
                var result =  o.GetRegisteredProperties()
                    .FirstOrDefault(x => !x.IsAttached && x.Name == parts[0]);

                if (result != null)
                {
                    return result;
                }

                // A type can .AddOwner an attached property.
                return o.GetRegisteredProperties()
                    .FirstOrDefault(x => x.Name == parts[0]);
            }
            else
            {
                return o.GetRegisteredProperties()
                    .FirstOrDefault(x => x.IsAttached && x.OwnerType.Name == parts[0] && x.Name == parts[1]);
            }
        }

        /// <summary>
        /// Observer method for <see cref="AddClassHandler{TTarget}(IObservable{PerspexPropertyChangedEventArgs},
        /// Func{TTarget, Action{PerspexPropertyChangedEventArgs}})"/>.
        /// </summary>
        /// <typeparam name="TTarget">The sender type to accept.</typeparam>
        /// <param name="e">The event args.</param>
        /// <param name="handler">Given a TTarget, returns the handler.</param>
        private static void SubscribeAdapter<TTarget>(
            PerspexPropertyChangedEventArgs e,
            Func<TTarget, Action<PerspexPropertyChangedEventArgs>> handler)
            where TTarget : class
        {
            var target = e.Sender as TTarget;

            if (target != null)
            {
                handler(target)(e);
            }
        }
    }
}
