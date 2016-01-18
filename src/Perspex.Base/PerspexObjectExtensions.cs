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
        /// Binds a property to a subject according to a <see cref="BindingMode"/>.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="property">The property to bind.</param>
        /// <param name="source">The binding source.</param>
        /// <param name="mode">The binding mode.</param>
        /// <param name="priority">The binding priority.</param>
        /// <returns>An <see cref="IDisposable"/> which can be used to cancel the binding.</returns>
        public static IDisposable Bind(
            this IPerspexObject o, 
            PerspexProperty property,
            ISubject<object> source,
            BindingMode mode,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            Contract.Requires<ArgumentNullException>(o != null);
            Contract.Requires<ArgumentNullException>(property != null);
            Contract.Requires<ArgumentNullException>(source != null);

            switch (mode)
            {
                case BindingMode.Default:
                case BindingMode.OneWay:
                    return o.Bind(property, source, priority);
                case BindingMode.TwoWay:
                    return new CompositeDisposable(
                        o.Bind(property, source, priority),
                        o.GetObservable(property).Subscribe(source));
                case BindingMode.OneTime:
                    return source.Take(1).Subscribe(x => o.SetValue(property, x, priority));
                case BindingMode.OneWayToSource:
                    return o.GetObservable(property).Subscribe(source);
                default:
                    throw new ArgumentException("Invalid binding mode.");
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

        class AnonymousSubject<T, U> : ISubject<T, U>
        {
            private readonly IObserver<T> _observer;
            private readonly IObservable<U> _observable;

            public AnonymousSubject(IObserver<T> observer, IObservable<U> observable)
            {
                _observer = observer;
                _observable = observable;
            }

            public void OnCompleted()
            {
                _observer.OnCompleted();
            }

            public void OnError(Exception error)
            {
                if (error == null)
                    throw new ArgumentNullException("error");

                _observer.OnError(error);
            }

            public void OnNext(T value)
            {
                _observer.OnNext(value);
            }

            public IDisposable Subscribe(IObserver<U> observer)
            {
                if (observer == null)
                    throw new ArgumentNullException("observer");

                //
                // [OK] Use of unsafe Subscribe: non-pretentious wrapping of an observable sequence.
                //
                return _observable.Subscribe/*Unsafe*/(observer);
            }
        }

        class AnonymousSubject<T> : AnonymousSubject<T, T>, ISubject<T>
        {
            public AnonymousSubject(IObserver<T> observer, IObservable<T> observable)
                : base(observer, observable)
            {
            }
        }
    }
}
