using System;
using Avalonia.Data;

namespace Avalonia.PropertyStore
{
    /// <summary>
    /// Represents the active value for a property in a <see cref="ValueStore"/>.
    /// </summary>
    /// <remarks>
    /// This class is an abstract base for the generic <see cref="EffectiveValue{T}"/>.
    /// </remarks>
    internal abstract class EffectiveValue
    {
        /// <summary>
        /// Gets the current effective value as a boxed value.
        /// </summary>
        public object? Value => GetBoxedValue();

        /// <summary>
        /// Gets the current effective base value as a boxed value, or 
        /// <see cref="AvaloniaProperty.UnsetValue"/> if not set.
        /// </summary>
        public object? BaseValue => GetBoxedBaseValue();

        /// <summary>
        /// Gets the priority of the current effective value.
        /// </summary>
        public BindingPriority Priority { get; protected set; }

        /// <summary>
        /// Gets the priority of the current base value.
        /// </summary>
        public BindingPriority BasePriority { get; protected set; }

        /// <summary>
        /// Sets the value and base value, raising <see cref="AvaloniaObject.PropertyChanged"/>
        /// where necessary.
        /// </summary>
        /// <param name="owner">The associated value store.</param>
        /// <param name="property">The property being changed.</param>
        /// <param name="value">The new value of the property.</param>
        /// <param name="priority">The priority of the new value.</param>
        public abstract void SetAndRaise(
            ValueStore owner,
            AvaloniaProperty property,
            object? value,
            BindingPriority priority);

        /// <summary>
        /// Sets the value and base value, raising <see cref="AvaloniaObject.PropertyChanged"/>
        /// where necessary.
        /// </summary>
        /// <param name="owner">The associated value store.</param>
        /// <param name="property">The property being changed.</param>
        /// <param name="value">The new value of the property.</param>
        /// <param name="priority">The priority of the new value.</param>
        /// <param name="baseValue">The new base value of the property.</param>
        /// <param name="basePriority">The priority of the new base value.</param>
        public abstract void SetAndRaise(
            ValueStore owner,
            AvaloniaProperty property,
            object? value,
            BindingPriority priority,
            object? baseValue,
            BindingPriority basePriority);

        /// <summary>
        /// Sets the value, raising <see cref="AvaloniaObject.PropertyChanged"/>
        /// where necessary.
        /// </summary>
        /// <param name="owner">The associated value store.</param>
        /// <param name="entry">The value entry with the new value of the property.</param>
        /// <param name="priority">The priority of the new value.</param>
        /// <remarks>
        /// This method does not set the base value.
        /// </remarks>
        public abstract void SetAndRaise(
            ValueStore owner,
            IValueEntry entry,
            BindingPriority priority);

        /// <summary>
        /// Set the value priority, but leaves the value unchanged.
        /// </summary>
        public void SetPriority(BindingPriority priority) => Priority = BindingPriority.Unset;

        /// <summary>
        /// Set the base value priority, but leaves the base value unchanged.
        /// </summary>
        public void SetBasePriority(BindingPriority priority) => BasePriority = BindingPriority.Unset;

        /// <summary>
        /// Raises <see cref="AvaloniaObject.PropertyChanged"/> in response to an inherited value
        /// change.
        /// </summary>
        /// <param name="owner">The owner object.</param>
        /// <param name="property">The property being changed.</param>
        /// <param name="oldValue">The old value of the property.</param>
        /// <param name="newValue">The new value of the property.</param>
        public abstract void RaiseInheritedValueChanged(
            AvaloniaObject owner,
            AvaloniaProperty property,
            EffectiveValue? oldValue,
            EffectiveValue? newValue);

        /// <summary>
        /// Coerces the property value.
        /// </summary>
        /// <param name="owner">The associated value store.</param>
        /// <param name="property">The property to coerce.</param>
        public abstract void CoerceValue(ValueStore owner, AvaloniaProperty property);

        /// <summary>
        /// Disposes the effective value, raising <see cref="AvaloniaObject.PropertyChanged"/>
        /// where necessary.
        /// </summary>
        /// <param name="owner">The associated value store.</param>
        /// <param name="property">The property being cleared.</param>
        public abstract void DisposeAndRaiseUnset(ValueStore owner, AvaloniaProperty property);

        protected abstract object? GetBoxedValue();
        protected abstract object? GetBoxedBaseValue();
    }
}
