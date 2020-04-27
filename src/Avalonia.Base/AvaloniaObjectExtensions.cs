using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Data;
using Avalonia.Reactive;
using Avalonia.Utilities;

namespace Avalonia
{
    /// <summary>
    /// Provides extension methods for <see cref="AvaloniaObject"/> and related classes.
    /// </summary>
    public static class AvaloniaObjectExtensions
    {
        /// <summary>
        /// Converts an <see cref="IObservable{T}"/> to an <see cref="IBinding"/>.
        /// </summary>
        /// <typeparam name="T">The type produced by the observable.</typeparam>
        /// <param name="source">The observable</param>
        /// <returns>An <see cref="IBinding"/>.</returns>
        public static IBinding ToBinding<T>(this IObservable<T> source)
        {
            return new BindingAdaptor(source.Select(x => (object)x));
        }

        /// <summary>
        /// Gets an observable for a <see cref="AvaloniaProperty"/>.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="property">The property.</param>
        /// <returns>
        /// An observable which fires immediately with the current value of the property on the
        /// object and subsequently each time the property value changes.
        /// </returns>
        /// <remarks>
        /// The subscription to <paramref name="o"/> is created using a weak reference.
        /// </remarks>
        public static IObservable<object> GetObservable(this IAvaloniaObject o, AvaloniaProperty property)
        {
            Contract.Requires<ArgumentNullException>(o != null);
            Contract.Requires<ArgumentNullException>(property != null);

            return GetObservableRouter.Instance.Route(o, property);
        }

        /// <summary>
        /// Gets an observable for a <see cref="AvaloniaProperty"/>.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <typeparam name="T">The property type.</typeparam>
        /// <param name="property">The property.</param>
        /// <returns>
        /// An observable which fires immediately with the current value of the property on the
        /// object and subsequently each time the property value changes.
        /// </returns>
        /// <remarks>
        /// The subscription to <paramref name="o"/> is created using a weak reference.
        /// </remarks>
        public static IObservable<T> GetObservable<T>(this IAvaloniaObject o, AvaloniaProperty<T> property)
        {
            o = o ?? throw new ArgumentNullException(nameof(o));
            property = property ?? throw new ArgumentNullException(nameof(property));

            var listener = property switch
            {
                StyledPropertyBase<T> s => o.Listen(s),
                DirectPropertyBase<T> d => o.Listen(d),
                _ => throw new NotSupportedException("Unexpected AvaloniaProperty type."),
            };

            if (listener is AvaloniaPropertyObservable<T> apo)
            {
                return apo.ValueAdapter;
            }
            else
            {
                return listener.Where(x => x.IsActiveValueChange && !x.IsOutdated)
                    .Select(x => x.NewValue.Value)
                    .StartWith(o.GetValue(property));
            }
        }

        /// <summary>
        /// Gets an observable for a <see cref="AvaloniaProperty"/>.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <typeparam name="T">The property type.</typeparam>
        /// <param name="property">The property.</param>
        /// <returns>
        /// An observable which fires immediately with the current value of the property on the
        /// object and subsequently each time the property value changes.
        /// </returns>
        /// <remarks>
        /// The subscription to <paramref name="o"/> is created using a weak reference.
        /// </remarks>
        public static IObservable<BindingValue<T>> GetBindingObservable<T>(
            this IAvaloniaObject o,
            AvaloniaProperty<T> property)
        {
            o = o ?? throw new ArgumentNullException(nameof(o));
            property = property ?? throw new ArgumentNullException(nameof(property));

            var listener = property switch
            {
                StyledPropertyBase<T> s => o.Listen(s),
                DirectPropertyBase<T> d => o.Listen(d),
                _ => throw new NotSupportedException("Unexpected AvaloniaProperty type."),
            };

            if (listener is AvaloniaPropertyObservable<T> apo)
            {
                return apo.BindingValueAdapter;
            }
            else
            {
                return listener.Where(x => x.IsActiveValueChange && !x.IsOutdated)
                    .Select(x => x.NewValue)
                    .StartWith(o.GetValue(property));
            }
        }

        /// <summary>
        /// Gets a subject for a <see cref="AvaloniaProperty"/>.
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
            this IAvaloniaObject o,
            AvaloniaProperty property,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            return Subject.Create<object>(
                Observer.Create<object>(x => o.SetValue(property, x, priority)),
                o.GetObservable(property));
        }

        /// <summary>
        /// Gets a subject for a <see cref="AvaloniaProperty"/>.
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
            this IAvaloniaObject o,
            AvaloniaProperty<T> property,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            return Subject.Create<T>(
                Observer.Create<T>(x => o.SetValue(property, x, priority)),
                o.GetObservable(property));
        }

        /// <summary>
        /// Binds a <see cref="AvaloniaProperty"/> to an observable.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="target">The object.</param>
        /// <param name="property">The property.</param>
        /// <param name="source">The observable.</param>
        /// <param name="priority">The priority of the binding.</param>
        /// <returns>
        /// A disposable which can be used to terminate the binding.
        /// </returns>
        public static IDisposable Bind<T>(
            this IAvaloniaObject target,
            AvaloniaProperty<T> property,
            IObservable<BindingValue<T>> source,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));
            property = property ?? throw new ArgumentNullException(nameof(property));
            source = source ?? throw new ArgumentNullException(nameof(source));

            return property switch
            {
                StyledPropertyBase<T> styled => target.Bind(styled, source, priority),
                DirectPropertyBase<T> direct => target.Bind(direct, source),
                _ => throw new NotSupportedException("Unsupported AvaloniaProperty type."),
            };
        }

        /// <summary>
        /// Binds a <see cref="AvaloniaProperty"/> to an observable.
        /// </summary>
        /// <param name="target">The object.</param>
        /// <param name="property">The property.</param>
        /// <param name="source">The observable.</param>
        /// <param name="priority">The priority of the binding.</param>
        /// <returns>
        /// A disposable which can be used to terminate the binding.
        /// </returns>
        public static IDisposable Bind(
            this IAvaloniaObject target,
            AvaloniaProperty property,
            IObservable<object> source,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));
            property = property ?? throw new ArgumentNullException(nameof(property));
            source = source ?? throw new ArgumentNullException(nameof(source));

            return property.RouteBind(target, source, priority);
        }

        /// <summary>
        /// Binds a <see cref="AvaloniaProperty"/> to an observable.
        /// </summary>
        /// <param name="target">The object.</param>
        /// <param name="property">The property.</param>
        /// <param name="source">The observable.</param>
        /// <param name="priority">The priority of the binding.</param>
        /// <returns>
        /// A disposable which can be used to terminate the binding.
        /// </returns>
        public static IDisposable Bind<T>(
            this IAvaloniaObject target,
            AvaloniaProperty<T> property,
            IObservable<T> source,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));
            property = property ?? throw new ArgumentNullException(nameof(property));
            source = source ?? throw new ArgumentNullException(nameof(source));

            return target.Bind(
                property,
                source.ToBindingValue(),
                priority);
        }

        /// <summary>
        /// Binds a property on an <see cref="IAvaloniaObject"/> to an <see cref="IBinding"/>.
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
            this IAvaloniaObject target,
            AvaloniaProperty property,
            IBinding binding,
            object anchor = null)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));
            property = property ?? throw new ArgumentNullException(nameof(property));
            binding = binding ?? throw new ArgumentNullException(nameof(binding));

            var metadata = property.GetMetadata(target.GetType()) as IDirectPropertyMetadata;

            var result = binding.Initiate(
                target,
                property,
                anchor,
                metadata?.EnableDataValidation ?? false);

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
        /// Clears a <see cref="AvaloniaProperty"/>'s local value.
        /// </summary>
        /// <param name="target">The object.</param>
        /// <param name="property">The property.</param>
        public static void ClearValue(this IAvaloniaObject target, AvaloniaProperty property)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));
            property = property ?? throw new ArgumentNullException(nameof(property));

            property.RouteClearValue(target);
        }

        /// <summary>
        /// Clears a <see cref="AvaloniaProperty"/>'s local value.
        /// </summary>
        /// <param name="target">The object.</param>
        /// <param name="property">The property.</param>
        public static void ClearValue<T>(this IAvaloniaObject target, AvaloniaProperty<T> property)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));
            property = property ?? throw new ArgumentNullException(nameof(property));

            switch (property)
            {
                case StyledPropertyBase<T> styled:
                    target.ClearValue(styled);
                    break;
                case DirectPropertyBase<T> direct:
                    target.ClearValue(direct);
                    break;
                default:
                    throw new NotSupportedException("Unsupported AvaloniaProperty type.");
            }
        }

        /// <summary>
        /// Gets a <see cref="AvaloniaProperty"/> value.
        /// </summary>
        /// <param name="target">The object.</param>
        /// <param name="property">The property.</param>
        /// <returns>The value.</returns>
        public static object GetValue(this IAvaloniaObject target, AvaloniaProperty property)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));
            property = property ?? throw new ArgumentNullException(nameof(property));

            return property.RouteGetValue(target);
        }

        /// <summary>
        /// Gets a <see cref="AvaloniaProperty"/> value.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="target">The object.</param>
        /// <param name="property">The property.</param>
        /// <returns>The value.</returns>
        public static T GetValue<T>(this IAvaloniaObject target, AvaloniaProperty<T> property)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));
            property = property ?? throw new ArgumentNullException(nameof(property));

            return property switch
            {
                StyledPropertyBase<T> styled => target.GetValue(styled),
                DirectPropertyBase<T> direct => target.GetValue(direct),
                _ => throw new NotSupportedException("Unsupported AvaloniaProperty type.")
            };
        }

        /// <summary>
        /// Sets a <see cref="AvaloniaProperty"/> value.
        /// </summary>
        /// <param name="target">The object.</param>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        /// <param name="priority">The priority of the value.</param>
        /// <returns>
        /// An <see cref="IDisposable"/> if setting the property can be undone, otherwise null.
        /// </returns>
        public static IDisposable SetValue(
            this IAvaloniaObject target,
            AvaloniaProperty property,
            object value,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));
            property = property ?? throw new ArgumentNullException(nameof(property));

            return property.RouteSetValue(target, value, priority);
        }

        /// <summary>
        /// Sets a <see cref="AvaloniaProperty"/> value.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="target">The object.</param>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        /// <param name="priority">The priority of the value.</param>
        /// <returns>
        /// An <see cref="IDisposable"/> if setting the property can be undone, otherwise null.
        /// </returns>
        public static IDisposable SetValue<T>(
            this IAvaloniaObject target,
            AvaloniaProperty<T> property,
            T value,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));
            property = property ?? throw new ArgumentNullException(nameof(property));

            switch (property)
            {
                case StyledPropertyBase<T> styled:
                    return target.SetValue(styled, value, priority);
                case DirectPropertyBase<T> direct:
                    target.SetValue(direct, value);
                    return null;
                default:
                    throw new NotSupportedException("Unsupported AvaloniaProperty type.");
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
            this IObservable<AvaloniaPropertyChangedEventArgs> observable,
            Action<TTarget, AvaloniaPropertyChangedEventArgs> action)
            where TTarget : AvaloniaObject
        {
            return observable.Subscribe(e =>
            {
                if (e.Sender is TTarget target)
                {
                    action(target, e);
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
        [Obsolete("Use overload taking Action<TTarget, AvaloniaPropertyChangedEventArgsdEventArgs>.")]
        public static IDisposable AddClassHandler<TTarget>(
            this IObservable<AvaloniaPropertyChangedEventArgs> observable,
            Func<TTarget, Action<AvaloniaPropertyChangedEventArgs>> handler)
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
        private static string GetDescription(IAvaloniaObject o, AvaloniaProperty property)
        {
            return $"{o.GetType().Name}.{property.Name}";
        }

        /// <summary>
        /// Observer method for <see cref="AddClassHandler{TTarget}(IObservable{AvaloniaPropertyChangedEventArgs},
        /// Func{TTarget, Action{AvaloniaPropertyChangedEventArgs}})"/>.
        /// </summary>
        /// <typeparam name="TTarget">The sender type to accept.</typeparam>
        /// <param name="e">The event args.</param>
        /// <param name="handler">Given a TTarget, returns the handler.</param>
        private static void SubscribeAdapter<TTarget>(
            AvaloniaPropertyChangedEventArgs e,
            Func<TTarget, Action<AvaloniaPropertyChangedEventArgs>> handler)
            where TTarget : class
        {
            if (e.Sender is TTarget target)
            {
                handler(target)(e);
            }
        }

        private class BindingAdaptor : IBinding
        {
            private IObservable<object> _source;

            public BindingAdaptor(IObservable<object> source)
            {
                this._source = source;
            }

            public InstancedBinding Initiate(
                IAvaloniaObject target,
                AvaloniaProperty targetProperty,
                object anchor = null,
                bool enableDataValidation = false)
            {
                return InstancedBinding.OneWay(_source);
            }
        }

        private class GetObservableRouter : IAvaloniaPropertyVisitor<GetObservableRouter.Data>
        {
            public static readonly GetObservableRouter Instance = new GetObservableRouter();

            public IObservable<object> Route(IAvaloniaObject o, AvaloniaProperty p)
            {
                var data = new Data { Source = o };
                p.Accept(this, ref data);
                return data.Result;
            }

            public void Visit<T>(StyledPropertyBase<T> property, ref Data data)
            {
                var listener = data.Source.Listen(property);

                if (listener is AvaloniaPropertyObservable<T> apo)
                {
                    data.Result = apo.UntypedValueAdapter;
                }
                else
                {
                    data.Result = listener.Where(x => x.IsActiveValueChange && !x.IsOutdated)
                        .Select(x => x.NewValue.ToUntyped())
                        .StartWith(data.Source.GetValue(property));

                }
            }

            public void Visit<T>(DirectPropertyBase<T> property, ref Data data)
            {
                var listener = data.Source.Listen(property);

                if (listener is AvaloniaPropertyObservable<T> apo)
                {
                    data.Result = apo.UntypedValueAdapter;
                }
                else
                {
                    data.Result = listener.Where(x => x.IsActiveValueChange && !x.IsOutdated)
                        .Select(x => x.NewValue.ToUntyped())
                        .StartWith(data.Source.GetValue(property));

                }
            }

            public struct Data
            {
                public IAvaloniaObject Source;
                public IObservable<object> Result;
            }
        }
    }
}
