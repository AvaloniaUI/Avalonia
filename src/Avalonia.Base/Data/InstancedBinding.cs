using System;
using System.ComponentModel;
using Avalonia.Data.Core;
using Avalonia.Reactive;
using ObservableEx = Avalonia.Reactive.Observable;

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
    public sealed class InstancedBinding
    {
        private readonly AvaloniaObject? _target;
        private readonly UntypedBindingExpressionBase? _expression;
        private IObservable<object?>? _observable;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstancedBinding"/> class.
        /// </summary>
        /// <param name="source">The binding source.</param>
        /// <param name="mode">The binding mode.</param>
        /// <param name="priority">The priority of the binding.</param>
        /// <remarks>
        /// This constructor can be used to create any type of binding and as such requires an
        /// <see cref="IObservable{Object}"/> as the binding source because this is the only binding
        /// source which can be used for all binding modes. If you wish to create an instance with
        /// something other than a subject, use one of the static creation methods on this class.
        /// </remarks>
        internal InstancedBinding(IObservable<object?> source, BindingMode mode, BindingPriority priority)
        {
            Mode = mode;
            Priority = priority;
            _observable = source ?? throw new ArgumentNullException(nameof(source));
        }

        internal InstancedBinding(
            UntypedBindingExpressionBase source,
            BindingMode mode,
            BindingPriority priority)
        {
            Mode = mode;
            Priority = priority;
            _expression = source ?? throw new ArgumentNullException(nameof(source));
        }

        internal InstancedBinding(
            AvaloniaObject? target,
            UntypedBindingExpressionBase source, 
            BindingMode mode, 
            BindingPriority priority)
        {
            Mode = mode;
            Priority = priority;
            _expression = source ?? throw new ArgumentNullException(nameof(source));
            _target = target;
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
        /// Gets the binding source observable.
        /// </summary>
        public IObservable<object?> Source => _observable ??= _expression!.ToObservable(_target);

        [Obsolete("Use Source property"), EditorBrowsable(EditorBrowsableState.Never)]
        public IObservable<object?> Observable => Source;

        internal UntypedBindingExpressionBase? Expression => _expression;

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
            return new InstancedBinding(ObservableEx.SingleValue(value), BindingMode.OneTime, priority);
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

            return new InstancedBinding((IObservable<object?>)observer, BindingMode.OneWayToSource, priority);
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
            return new InstancedBinding(Source, Mode, priority);
        }
    }
}
