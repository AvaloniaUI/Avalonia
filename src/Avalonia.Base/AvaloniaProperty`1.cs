using System;
using System.Reactive.Subjects;
using Avalonia.Data;
using Avalonia.Utilities;

namespace Avalonia
{
    /// <summary>
    /// A typed avalonia property.
    /// </summary>
    /// <typeparam name="TValue">The value type of the property.</typeparam>
    public abstract class AvaloniaProperty<TValue> : AvaloniaProperty
    {
        private readonly Subject<AvaloniaPropertyChangedEventArgs<TValue>> _changed;

        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaProperty{TValue}"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="ownerType">The type of the class that registers the property.</param>
        /// <param name="metadata">The property metadata.</param>
        /// <param name="notifying">A <see cref="AvaloniaProperty.Notifying"/> callback.</param>
        protected AvaloniaProperty(
            string name,
            Type ownerType,
            AvaloniaPropertyMetadata metadata,
            Action<IAvaloniaObject, bool>? notifying = null)
            : base(name, typeof(TValue), ownerType, metadata, notifying)
        {
            _changed = new Subject<AvaloniaPropertyChangedEventArgs<TValue>>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaProperty{TValue}"/> class.
        /// </summary>
        /// <param name="source">The property to copy.</param>
        /// <param name="ownerType">The new owner type.</param>
        /// <param name="metadata">Optional overridden metadata.</param>
        protected AvaloniaProperty(
            AvaloniaProperty<TValue> source,
            Type ownerType,
            AvaloniaPropertyMetadata? metadata)
            : base(source, ownerType, metadata)
        {
            _changed = source._changed;
        }

        /// <summary>
        /// Gets an observable that is fired when this property changes on any
        /// <see cref="AvaloniaObject"/> instance.
        /// </summary>
        /// <value>
        /// An observable that is fired when this property changes on any
        /// <see cref="AvaloniaObject"/> instance.
        /// </value>

        public new IObservable<AvaloniaPropertyChangedEventArgs<TValue>> Changed => _changed;

        /// <summary>
        /// Notifies the <see cref="Changed"/> observable.
        /// </summary>
        /// <param name="e">The observable arguments.</param>
        internal void NotifyChanged(AvaloniaPropertyChangedEventArgs<TValue> e)
        {
            _changed.OnNext(e);
        }

        protected override IObservable<AvaloniaPropertyChangedEventArgs> GetChanged() => Changed;

        protected BindingValue<object?> TryConvert(object? value)
        {
            if (value == UnsetValue)
            {
                return BindingValue<object?>.Unset;
            }
            else if (value == BindingOperations.DoNothing)
            {
                return BindingValue<object?>.DoNothing;
            }

            if (!TypeUtilities.TryConvertImplicit(PropertyType, value, out var converted))
            {
                var error = new ArgumentException(string.Format(
                    "Invalid value for Property '{0}': '{1}' ({2})",
                    Name,
                    value,
                    value?.GetType().FullName ?? "(null)"));
                return BindingValue<object?>.BindingError(error);
            }

            return converted;
        }
    }
}
