using Avalonia.Data;

namespace Avalonia
{
    /// <summary>
    /// Provides information for an Avalonia property change.
    /// </summary>
    public class AvaloniaPropertyChangedEventArgs<T> : AvaloniaPropertyChangedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaPropertyChangedEventArgs"/> class.
        /// </summary>
        /// <param name="sender">The object that the property changed on.</param>
        /// <param name="property">The property that changed.</param>
        /// <param name="oldValue">The old value of the property.</param>
        /// <param name="newValue">The new value of the property.</param>
        /// <param name="priority">The priority of the binding that produced the value.</param>
        public AvaloniaPropertyChangedEventArgs(
            IAvaloniaObject sender,
            AvaloniaProperty<T> property,
            Optional<T> oldValue,
            BindingValue<T> newValue,
            BindingPriority priority)
            : base(sender, priority)
        {
            Property = property;
            OldValue = oldValue;
            NewValue = newValue;
        }

        /// <summary>
        /// Gets the property that changed.
        /// </summary>
        /// <value>
        /// The property that changed.
        /// </value>
        public new AvaloniaProperty<T> Property { get; }

        /// <summary>
        /// Gets the old value of the property.
        /// </summary>
        /// <remarks>
        /// When <see cref="AvaloniaPropertyChangedEventArgs.IsEffectiveValueChange"/> is true, returns the
        /// old value of the property on the object. 
        /// When <see cref="AvaloniaPropertyChangedEventArgs.IsEffectiveValueChange"/> is false, returns
        /// <see cref="Optional{T}.Empty"/>.
        /// </remarks>
        public new Optional<T> OldValue { get; private set; }

        /// <summary>
        /// Gets the new value of the property.
        /// </summary>
        /// <remarks>
        /// When <see cref="AvaloniaPropertyChangedEventArgs.IsEffectiveValueChange"/> is true, returns the
        /// value of the property on the object.
        /// When <see cref="AvaloniaPropertyChangedEventArgs.IsEffectiveValueChange"/> is false returns the
        /// changed value, or <see cref="Optional{T}.Empty"/> if the value was removed.
        /// </remarks>
        public new BindingValue<T> NewValue { get; private set; }

        internal void SetOldValue(Optional<T> value) => OldValue = value;
        internal void SetNewValue(BindingValue<T> value) => NewValue = value;

        protected override AvaloniaProperty GetProperty() => Property;

        protected override object? GetOldValue() => OldValue.GetValueOrDefault(AvaloniaProperty.UnsetValue);

        protected override object? GetNewValue() => NewValue.GetValueOrDefault(AvaloniaProperty.UnsetValue);
    }
}
