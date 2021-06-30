using Avalonia.Data;

#nullable enable

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
            : this(sender, property, oldValue, newValue, priority, true)
        {
        }

        internal AvaloniaPropertyChangedEventArgs(
            IAvaloniaObject sender,
            AvaloniaProperty<T> property,
            Optional<T> oldValue,
            BindingValue<T> newValue,
            BindingPriority priority,
            bool isEffectiveValueChange)
            : base(sender, priority)
        {
            Property = property;
            OldValue = oldValue;
            NewValue = newValue;
            IsEffectiveValueChange = isEffectiveValueChange;
        }

        /// <summary>
        /// Gets the property that changed.
        /// </summary>
        /// <value>
        /// The property that changed.
        /// </value>
        public new AvaloniaProperty<T> Property { get; private set; }

        /// <summary>
        /// Gets the old value of the property.
        /// </summary>
        public new Optional<T> OldValue { get; private set; }

        /// <summary>
        /// Gets the new value of the property.
        /// </summary>
        public new BindingValue<T> NewValue { get; private set; }

        internal bool IsEffectiveValueChange { get; private set; }

        protected override AvaloniaProperty GetProperty() => Property;
        protected override object? GetOldValue() => OldValue.GetValueOrDefault(AvaloniaProperty.UnsetValue);
        protected override object? GetNewValue() => NewValue.GetValueOrDefault(AvaloniaProperty.UnsetValue);

        internal void Initialize(
            IAvaloniaObject sender,
            AvaloniaProperty<T> property,
            Optional<T> oldValue,
            BindingValue<T> newValue,
            BindingPriority priority,
            bool isEffectiveValueChange)
        {
            Sender = sender;
            Property = property;
            OldValue = oldValue;
            NewValue = newValue;
            Priority = priority;
            IsEffectiveValueChange = isEffectiveValueChange;
        }

        internal void Recycle()
        {
            Sender = null!;
            Property = null!;
            NewValue = default;
            OldValue = default;
        }

        internal void SetOldValue(Optional<T> value) => OldValue = value;
        internal void SetNewValue(BindingValue<T> value) => NewValue = value;
    }
}
