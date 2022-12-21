using System;
using Avalonia.Reactive;

namespace Avalonia.Data
{
    /// <summary>
    /// Holds the result of calling <see cref="IBinding.Initiate"/>.
    /// </summary>
    /// <remarks>
    /// Whereas an <see cref="IBinding"/> holds a description of a binding such as "Bind to the X
    /// property on a control's DataContext"; this class represents a binding that has been 
    /// *instanced* by calling <see cref="IBinding.Initiate(AvaloniaObject, AvaloniaProperty, object, bool)"/>
    /// on a target object.
    /// </remarks>
    public class InstancedBinding
    {
        internal InstancedBinding(object? value, BindingMode mode, BindingPriority priority)
        {
            Mode = mode;
            Priority = priority;
            Value = value;
        }

        /// <summary>
        /// Gets the binding mode with which the binding was initiated.
        /// </summary>
        public BindingMode Mode { get; }

        /// <summary>
        /// Gets the binding priority.
        /// </summary>
        public BindingPriority Priority { get; }

        /// <summary>
        /// Gets the value or source of the binding.
        /// </summary>
        public object? Value { get; }

        /// <summary>
        /// Gets the <see cref="Value"/> as an observable.
        /// </summary>
        public IObservable<object?>? Observable => Value as IObservable<object?>;

        /// <summary>
        /// Gets the <see cref="Value"/> as an observer.
        /// </summary>
        public IObserver<object?>? Observer => Value as IObserver<object?>;

        /// <summary>
        /// Gets the <see cref="Subject"/> as an subject.
        /// </summary>
        internal IAvaloniaSubject<object?>? Subject => Value as IAvaloniaSubject<object?>;

        /// <summary>
        /// Creates a new one-time binding with a fixed value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="priority">The priority of the binding.</param>
        /// <returns>An <see cref="InstancedBinding"/> instance.</returns>
        public static InstancedBinding OneTime(
            object value,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            return new InstancedBinding(value, BindingMode.OneTime, priority);
        }

        /// <summary>
        /// Creates a new one-time binding.
        /// </summary>
        /// <param name="observable">The source observable.</param>
        /// <param name="priority">The priority of the binding.</param>
        /// <returns>An <see cref="InstancedBinding"/> instance.</returns>
        public static InstancedBinding OneTime(
            IObservable<object?> observable,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            _ = observable ?? throw new ArgumentNullException(nameof(observable));

            return new InstancedBinding(observable, BindingMode.OneTime, priority);
        }

        /// <summary>
        /// Creates a new one-way binding.
        /// </summary>
        /// <param name="observable">The source observable.</param>
        /// <param name="priority">The priority of the binding.</param>
        /// <returns>An <see cref="InstancedBinding"/> instance.</returns>
        public static InstancedBinding OneWay(
            IObservable<object?> observable,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            _ = observable ?? throw new ArgumentNullException(nameof(observable));

            return new InstancedBinding(observable, BindingMode.OneWay, priority);
        }

        /// <summary>
        /// Creates a new one-way to source binding.
        /// </summary>
        /// <param name="observer">The binding source.</param>
        /// <param name="priority">The priority of the binding.</param>
        /// <returns>An <see cref="InstancedBinding"/> instance.</returns>
        public static InstancedBinding OneWayToSource(
            IObserver<object?> observer,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            _ = observer ?? throw new ArgumentNullException(nameof(observer));

            return new InstancedBinding(observer, BindingMode.OneWayToSource, priority);
        }

        /// <summary>
        /// Creates a new two-way binding.
        /// </summary>
        /// <param name="observable">The binding source.</param>
        /// <param name="observer">The binding source.</param>
        /// <param name="priority">The priority of the binding.</param>
        /// <returns>An <see cref="InstancedBinding"/> instance.</returns>
        public static InstancedBinding TwoWay(
            IObservable<object?> observable,
            IObserver<object?> observer,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            _ = observable ?? throw new ArgumentNullException(nameof(observable));
            _ = observer ?? throw new ArgumentNullException(nameof(observer));

            var subject = new CombinedSubject<object?>(observer, observable);
            return new InstancedBinding(subject, BindingMode.TwoWay, priority);
        }

        /// <summary>
        /// Creates a copy of the <see cref="InstancedBinding"/> with a different priority.
        /// </summary>
        /// <param name="priority">The priority of the binding.</param>
        /// <returns>An <see cref="InstancedBinding"/> instance.</returns>
        public InstancedBinding WithPriority(BindingPriority priority)
        {
            return new InstancedBinding(Value, Mode, priority);
        }
    }
}
