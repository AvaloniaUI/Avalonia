// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Perspex.Data;
using Perspex.Reactive;

namespace Perspex
{
    /// <summary>
    /// Provides extension methods for <see cref="PerspexObject"/> and related classes.
    /// </summary>
    public static class PerspexObjectExtensions
    {
        /// <summary>
        /// Gets an observable for a <see cref="PerspexProperty"/>.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="property">The property.</param>
        /// <returns>An observable.</returns>
        public static IObservable<object> GetObservable(this IPerspexObject o, PerspexProperty property)
        {
            Contract.Requires<ArgumentNullException>(o != null);
            Contract.Requires<ArgumentNullException>(property != null);

            return new PerspexObservable<object>(
                observer =>
                {
                    EventHandler<PerspexPropertyChangedEventArgs> handler = (s, e) =>
                    {
                        if (e.Property == property)
                        {
                            observer.OnNext(e.NewValue);
                        }
                    };

                    observer.OnNext(o.GetValue(property));

                    o.PropertyChanged += handler;

                    return Disposable.Create(() =>
                    {
                        o.PropertyChanged -= handler;
                    });
                },
                GetDescription(o, property));
        }

        /// <summary>
        /// Gets an observable for a <see cref="PerspexProperty"/>.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <typeparam name="T">The property type.</typeparam>
        /// <param name="property">The property.</param>
        /// <returns>An observable.</returns>
        public static IObservable<T> GetObservable<T>(this IPerspexObject o, PerspexProperty<T> property)
        {
            Contract.Requires<ArgumentNullException>(o != null);
            Contract.Requires<ArgumentNullException>(property != null);

            return o.GetObservable((PerspexProperty)property).Cast<T>();
        }

        /// <summary>
        /// Gets an observable for a <see cref="PerspexProperty"/>.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <returns>
        /// An observable which when subscribed pushes the old and new values of the property each
        /// time it is changed.
        /// </returns>
        public static IObservable<Tuple<T, T>> GetObservableWithHistory<T>(
            this IPerspexObject o, 
            PerspexProperty<T> property)
        {
            Contract.Requires<ArgumentNullException>(o != null);
            Contract.Requires<ArgumentNullException>(property != null);

            return new PerspexObservable<Tuple<T, T>>(
                observer =>
                {
                    EventHandler<PerspexPropertyChangedEventArgs> handler = (s, e) =>
                    {
                        if (e.Property == property)
                        {
                            observer.OnNext(Tuple.Create((T)e.OldValue, (T)e.NewValue));
                        }
                    };

                    o.PropertyChanged += handler;

                    return Disposable.Create(() =>
                    {
                        o.PropertyChanged -= handler;
                    });
                },
                GetDescription(o, property));
        }

        /// <summary>
        /// Gets a subject for a <see cref="PerspexProperty"/>.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="property">The property.</param>
        /// <param name="priority">
        /// The priority with which binding values are written to the object.
        /// </param>
        /// <returns>
        /// An <see cref="ISubject{Object}"/> which can be used for two-way binding to/from the 
        /// property.
        /// </returns>
        public static ISubject<object> GetSubject(
            this IPerspexObject o,
            PerspexProperty property,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            // TODO: Subject.Create<T> is not yet in stable Rx : once it is, remove the 
            // AnonymousSubject classes and use Subject.Create<T>.
            var output = new Subject<object>();
            var result = new AnonymousSubject<object>(
                Observer.Create<object>(
                    x => output.OnNext(x),
                    e => output.OnError(e),
                    () => output.OnCompleted()),
                o.GetObservable(property));
            o.Bind(property, output, priority);
            return result;
        }

        /// <summary>
        /// Gets a subject for a <see cref="PerspexProperty"/>.
        /// </summary>
        /// <typeparam name="T">The property type.</typeparam>
        /// <param name="o">The object.</param>
        /// <param name="property">The property.</param>
        /// <param name="priority">
        /// The priority with which binding values are written to the object.
        /// </param>
        /// <returns>
        /// An <see cref="ISubject{T}"/> which can be used for two-way binding to/from the 
        /// property.
        /// </returns>
        public static ISubject<T> GetSubject<T>(
            this IPerspexObject o,
            PerspexProperty<T> property,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            // TODO: Subject.Create<T> is not yet in stable Rx : once it is, remove the 
            // AnonymousSubject classes from this file and use Subject.Create<T>.
            var output = new Subject<T>();
            var result = new AnonymousSubject<T>(
                Observer.Create<T>(
                    x => output.OnNext(x),
                    e => output.OnError(e),
                    () => output.OnCompleted()),
                o.GetObservable(property));
            o.Bind(property, output, priority);
            return result;
        }

        /// <summary>
        /// Gets a weak observable for a <see cref="PerspexProperty"/>.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="property">The property.</param>
        /// <returns>An observable.</returns>
        public static IObservable<object> GetWeakObservable(this IPerspexObject o, PerspexProperty property)
        {
            Contract.Requires<ArgumentNullException>(o != null);
            Contract.Requires<ArgumentNullException>(property != null);

            return new WeakPropertyChangedObservable(
                new WeakReference<IPerspexObject>(o), 
                property, 
                GetDescription(o, property));
        }

        /// <summary>
        /// Binds a property on an <see cref="IPerspexObject"/> to an <see cref="IBinding"/>.
        /// </summary>
        /// <param name="target">The object.</param>
        /// <param name="property">The property to bind.</param>
        /// <param name="binding">The binding.</param>
        /// <param name="anchor">
        /// An optional anchor from which to locate required context. When binding to objects that
        /// are not in the logical tree, certain types of binding need an anchor into the tree in 
        /// order to locate named controls or resources. The <paramref name="anchor"/> parameter 
        /// can be used to provice this context.
        /// </param>
        /// <returns>An <see cref="IDisposable"/> which can be used to cancel the binding.</returns>
        public static IDisposable Bind(
            this IPerspexObject target,
            PerspexProperty property,
            IBinding binding,
            object anchor = null)
        {
            Contract.Requires<ArgumentNullException>(target != null);
            Contract.Requires<ArgumentNullException>(property != null);
            Contract.Requires<ArgumentNullException>(binding != null);

            var result = binding.Initiate(target, property, anchor);

            if (result != null)
            {
                return BindingOperations.Apply(target, property, result, anchor);
            }
            else
            {
                return Disposable.Empty;
            }
        }

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
        /// Gets a description of a property that van be used in observables.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="property">The property</param>
        /// <returns>The description.</returns>
        private static string GetDescription(IPerspexObject o, PerspexProperty property)
        {
            return $"{o.GetType().Name}.{property.Name}";
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
