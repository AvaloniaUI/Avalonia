using System;
using Avalonia.Reactive;
using Avalonia.Data;

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
            return new BindingAdaptor(source.Select(x => (object?)x));
        }

        /// <summary>
        /// Gets an observable for an <see cref="AvaloniaProperty"/>.
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
        public static IObservable<object?> GetObservable(this AvaloniaObject o, AvaloniaProperty property)
        {
            return new AvaloniaPropertyObservable<object?>(
                o ?? throw new ArgumentNullException(nameof(o)), 
                property ?? throw new ArgumentNullException(nameof(property)));
        }

        /// <summary>
        /// Gets an observable for an <see cref="AvaloniaProperty"/>.
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
        public static IObservable<T> GetObservable<T>(this AvaloniaObject o, AvaloniaProperty<T> property)
        {
            return new AvaloniaPropertyObservable<T>(
                o ?? throw new ArgumentNullException(nameof(o)),
                property ?? throw new ArgumentNullException(nameof(property)));
        }

        /// <summary>
        /// Gets an observable for an <see cref="AvaloniaProperty"/>.
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
        public static IObservable<BindingValue<object?>> GetBindingObservable(
            this AvaloniaObject o,
            AvaloniaProperty property)
        {
            return new AvaloniaPropertyBindingObservable<object?>(
                o ?? throw new ArgumentNullException(nameof(o)),
                property ?? throw new ArgumentNullException(nameof(property)));
        }

        /// <summary>
        /// Gets an observable for an <see cref="AvaloniaProperty"/>.
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
            this AvaloniaObject o,
            AvaloniaProperty<T> property)
        {
            return new AvaloniaPropertyBindingObservable<T>(
                o ?? throw new ArgumentNullException(nameof(o)),
                property ?? throw new ArgumentNullException(nameof(property)));

        }

        /// <summary>
        /// Gets an observable that listens for property changed events for an
        /// <see cref="AvaloniaProperty"/>.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="property">The property.</param>
        /// <returns>
        /// An observable which when subscribed pushes the property changed event args
        /// each time a <see cref="AvaloniaObject.PropertyChanged"/> event is raised
        /// for the specified property.
        /// </returns>
        public static IObservable<AvaloniaPropertyChangedEventArgs> GetPropertyChangedObservable(
            this AvaloniaObject o,
            AvaloniaProperty property)
        {
            return new AvaloniaPropertyChangedObservable(
                o ?? throw new ArgumentNullException(nameof(o)),
                property ?? throw new ArgumentNullException(nameof(property)));
        }

        /// <summary>
        /// Binds an <see cref="AvaloniaProperty"/> to an observable.
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
            this AvaloniaObject target,
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
        /// Binds an <see cref="AvaloniaProperty"/> to an observable.
        /// </summary>
        /// <param name="target">The object.</param>
        /// <param name="property">The property.</param>
        /// <param name="source">The observable.</param>
        /// <param name="priority">The priority of the binding.</param>
        /// <returns>
        /// A disposable which can be used to terminate the binding.
        /// </returns>
        public static IDisposable Bind<T>(
            this AvaloniaObject target,
            AvaloniaProperty<T> property,
            IObservable<T> source,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            return property switch
            {
                StyledPropertyBase<T> styled => target.Bind(styled, source, priority),
                DirectPropertyBase<T> direct => target.Bind(direct, source),
                _ => throw new NotSupportedException("Unsupported AvaloniaProperty type."),
            };
        }

        /// <summary>
        /// Binds a property on an <see cref="AvaloniaObject"/> to an <see cref="IBinding"/>.
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
            this AvaloniaObject target,
            AvaloniaProperty property,
            IBinding binding,
            object? anchor = null)
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
        /// Gets a <see cref="AvaloniaProperty"/> value.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="target">The object.</param>
        /// <param name="property">The property.</param>
        /// <returns>The value.</returns>
        public static T GetValue<T>(this AvaloniaObject target, AvaloniaProperty<T> property)
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
        /// Gets an <see cref="AvaloniaProperty"/> base value.
        /// </summary>
        /// <param name="target">The object.</param>
        /// <param name="property">The property.</param>
        /// <remarks>
        /// For styled properties, gets the value of the property excluding animated values, otherwise
        /// <see cref="AvaloniaProperty.UnsetValue"/>. Note that this method does not return
        /// property values that come from inherited or default values.
        /// 
        /// For direct properties returns the current value of the property.
        /// </remarks>
        public static object? GetBaseValue(
            this AvaloniaObject target,
            AvaloniaProperty property)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));
            property = property ?? throw new ArgumentNullException(nameof(property));

            return property.RouteGetBaseValue(target);
        }

        /// <summary>
        /// Gets an <see cref="AvaloniaProperty"/> base value.
        /// </summary>
        /// <param name="target">The object.</param>
        /// <param name="property">The property.</param>
        /// <remarks>
        /// For styled properties, gets the value of the property excluding animated values, otherwise
        /// <see cref="Optional{T}.Empty"/>. Note that this method does not return property values
        /// that come from inherited or default values.
        /// 
        /// For direct properties returns the current value of the property.
        /// </remarks>
        public static Optional<T> GetBaseValue<T>(
            this AvaloniaObject target,
            AvaloniaProperty<T> property)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));
            property = property ?? throw new ArgumentNullException(nameof(property));

            return property switch
            {
                StyledPropertyBase<T> styled => target.GetBaseValue(styled),
                DirectPropertyBase<T> direct => target.GetValue(direct),
                _ => throw new NotSupportedException("Unsupported AvaloniaProperty type.")
            };
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
            return observable.Subscribe(new ClassHandlerObserver<TTarget>(action));
        }

        /// <summary>
        /// Subscribes to a property changed notifications for changes that originate from a
        /// <typeparamref name="TTarget"/>.
        /// </summary>
        /// <typeparam name="TTarget">The type of the property change sender.</typeparam>
        /// /// <typeparam name="TValue">The type of the property..</typeparam>
        /// <param name="observable">The property changed observable.</param>
        /// <param name="action">
        /// The method to call. The parameters are the sender and the event args.
        /// </param>
        /// <returns>A disposable that can be used to terminate the subscription.</returns>
        public static IDisposable AddClassHandler<TTarget, TValue>(
            this IObservable<AvaloniaPropertyChangedEventArgs<TValue>> observable,
            Action<TTarget, AvaloniaPropertyChangedEventArgs<TValue>> action) where TTarget : AvaloniaObject
        {
            return observable.Subscribe(new ClassHandlerObserver<TTarget, TValue>(action));
        }

        private class BindingAdaptor : IBinding
        {
            private IObservable<object?> _source;

            public BindingAdaptor(IObservable<object?> source)
            {
                this._source = source;
            }

            public InstancedBinding? Initiate(
                AvaloniaObject target,
                AvaloniaProperty? targetProperty,
                object? anchor = null,
                bool enableDataValidation = false)
            {
                return InstancedBinding.OneWay(_source);
            }
        }
        
        private class ClassHandlerObserver<TTarget, TValue> : IObserver<AvaloniaPropertyChangedEventArgs<TValue>>
        {
            private readonly Action<TTarget, AvaloniaPropertyChangedEventArgs<TValue>> _action;

            public ClassHandlerObserver(Action<TTarget, AvaloniaPropertyChangedEventArgs<TValue>> action)
            {
                _action = action;
            }

            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
            }

            public void OnNext(AvaloniaPropertyChangedEventArgs<TValue> value)
            {
                if (value.Sender is TTarget target)
                {
                    _action(target, value);
                }
            }
        }

        private class ClassHandlerObserver<TTarget> : IObserver<AvaloniaPropertyChangedEventArgs>
        {
            private readonly Action<TTarget, AvaloniaPropertyChangedEventArgs> _action;

            public ClassHandlerObserver(Action<TTarget, AvaloniaPropertyChangedEventArgs> action)
            {
                _action = action;
            }

            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
            }

            public void OnNext(AvaloniaPropertyChangedEventArgs value)
            {
                if (value.Sender is TTarget target)
                {
                    _action(target, value);
                }
            }
        }
    }
}
