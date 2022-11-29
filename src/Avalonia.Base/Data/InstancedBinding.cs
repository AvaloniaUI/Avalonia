using System;
using System.Reactive.Subjects;

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
        /// <summary>
        /// Initializes a new instance of the <see cref="InstancedBinding"/> class.
        /// </summary>
        /// <param name="subject">The binding source.</param>
        /// <param name="mode">The binding mode.</param>
        /// <param name="priority">The priority of the binding.</param>
        /// <remarks>
        /// This constructor can be used to create any type of binding and as such requires an
        /// <see cref="ISubject{Object}"/> as the binding source because this is the only binding
        /// source which can be used for all binding modes. If you wish to create an instance with
        /// something other than a subject, use one of the static creation methods on this class.
        /// </remarks>
        public InstancedBinding(ISubject<object?> subject, BindingMode mode, BindingPriority priority)
        {
            Contract.Requires<ArgumentNullException>(subject != null);

            Mode = mode;
            Priority = priority;
            Value = subject;
        }

        private InstancedBinding(object? value, BindingMode mode, BindingPriority priority)
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
        /// Gets the <see cref="Value"/> as a subject.
        /// </summary>
        public ISubject<object?>? Subject => Value as ISubject<object?>;

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
        /// <param name="subject">The binding source.</param>
        /// <param name="priority">The priority of the binding.</param>
        /// <returns>An <see cref="InstancedBinding"/> instance.</returns>
        public static InstancedBinding OneWayToSource(
            ISubject<object?> subject,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            _ = subject ?? throw new ArgumentNullException(nameof(subject));

            return new InstancedBinding(subject, BindingMode.OneWayToSource, priority);
        }

        /// <summary>
        /// Creates a new two-way binding.
        /// </summary>
        /// <param name="subject">The binding source.</param>
        /// <param name="priority">The priority of the binding.</param>
        /// <returns>An <see cref="InstancedBinding"/> instance.</returns>
        public static InstancedBinding TwoWay(
            ISubject<object?> subject,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            _ = subject ?? throw new ArgumentNullException(nameof(subject));

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
