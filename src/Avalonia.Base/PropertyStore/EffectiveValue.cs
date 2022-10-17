using System.Diagnostics;
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
        private IValueEntry? _valueEntry;
        private IValueEntry? _baseValueEntry;

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
        /// Begins a reevaluation pass on the effective value.
        /// </summary>
        /// <param name="clearLocalValue">
        /// Determines whether any current local value should be cleared.
        /// </param>
        /// <remarks>
        /// This method resets the <see cref="Priority"/> and <see cref="BasePriority"/> properties
        /// to Unset, pending reevaluation.
        /// </remarks>
        public void BeginReevaluation(bool clearLocalValue = false)
        {
            if (clearLocalValue || Priority != BindingPriority.LocalValue)
                Priority = BindingPriority.Unset;
            if (clearLocalValue || BasePriority != BindingPriority.LocalValue)
                BasePriority = BindingPriority.Unset;
        }

        public void EndReevaluation()
        {
            if (Priority == BindingPriority.Unset)
            {
                _valueEntry?.Unsubscribe();
                _valueEntry = null;
            }

            if (BasePriority == BindingPriority.Unset)
            {
                _baseValueEntry?.Unsubscribe();
                _baseValueEntry = null;
            }
        }

        /// <summary>
        /// Sets the value and base value for a non-LocalValue priority, raising 
        /// <see cref="AvaloniaObject.PropertyChanged"/> where necessary.
        /// </summary>
        /// <param name="owner">The associated value store.</param>
        /// <param name="value">The new value of the property.</param>
        /// <param name="priority">The priority of the new value.</param>
        public abstract void SetAndRaise(
            ValueStore owner,
            IValueEntry value,
            BindingPriority priority);

        /// <summary>
        /// Sets the value and base value for a non-LocalValue priority, raising 
        /// <see cref="AvaloniaObject.PropertyChanged"/> where necessary.
        /// </summary>
        /// <param name="owner">The associated value store.</param>
        /// <param name="value">The new value of the property.</param>
        /// <param name="priority">The priority of the new value.</param>
        /// <param name="baseValue">The new base value of the property.</param>
        /// <param name="basePriority">The priority of the new base value.</param>
        public abstract void SetAndRaise(
            ValueStore owner,
            IValueEntry value,
            BindingPriority priority,
            IValueEntry baseValue,
            BindingPriority basePriority);

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
        /// Removes the current animation value and reverts to the base value, raising
        /// <see cref="AvaloniaObject.PropertyChanged"/> where necessary.
        /// </summary>
        /// <param name="owner">The associated value store.</param>
        /// <param name="property">The property being changed.</param>
        public abstract void RemoveAnimationAndRaise(ValueStore owner, AvaloniaProperty property);

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

        protected void UpdateValueEntry(IValueEntry? entry, BindingPriority priority)
        {
            Debug.Assert(priority != BindingPriority.LocalValue);

            if (priority <= BindingPriority.Animation)
            {
                // If we've received an animation value and the current value is a non-animation
                // value, then the current entry becomes our base entry.
                if (Priority > BindingPriority.LocalValue && Priority < BindingPriority.Inherited)
                {
                    Debug.Assert(_valueEntry is not null);
                    _baseValueEntry = _valueEntry;
                    _valueEntry = null;
                }

                if (_valueEntry != entry)
                {
                    _valueEntry?.Unsubscribe();
                    _valueEntry = entry;
                }
            }
            else if (Priority <= BindingPriority.Animation)
            {
                // We've received a non-animation value and have an active animation value, so the
                // new entry becomes our base entry.
                if (_baseValueEntry != entry)
                {
                    _baseValueEntry?.Unsubscribe();
                    _baseValueEntry = entry;
                }
            }
            else if (_valueEntry != entry)
            {
                // Both the current value and the new value are non-animation values, so the new
                // entry replaces the existing entry.
                _valueEntry?.Unsubscribe();
                _valueEntry = entry;
            }
        }

        protected void UnsubscribeValueEntries()
        {
            _valueEntry?.Unsubscribe();
            _baseValueEntry?.Unsubscribe();
        }
    }
}
