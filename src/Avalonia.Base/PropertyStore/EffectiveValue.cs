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
        /// <summary>
        /// Gets the current effective value as a boxed value.
        /// </summary>
        public object? Value => GetBoxedValue();

        /// <summary>
        /// Gets the priority of the current effective value.
        /// </summary>
        public BindingPriority Priority { get; protected set; }

        /// <summary>
        /// Gets the priority of the current base value.
        /// </summary>
        public BindingPriority BasePriority { get; protected set; }

        /// <summary>
        /// Gets the active value entry for the current effective value.
        /// </summary>
        public IValueEntry? ValueEntry { get; private set; }

        /// <summary>
        /// Gets the active value entry for the current base value.
        /// </summary>
        public IValueEntry? BaseValueEntry { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the property has a coercion function.
        /// </summary>
        public bool HasCoercion { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether the <see cref="Value"/> was overridden by a call to 
        /// <see cref="AvaloniaObject.SetCurrentValue{T}"/>.
        /// </summary>
        public bool IsOverridenCurrentValue { get; set; }

        /// <summary>
        /// Gets a value indicating whether the <see cref="Value"/> is the result of the 
        /// 
        /// </summary>
        public bool IsCoercedDefaultValue { get; set; }

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
            if (clearLocalValue || (Priority != BindingPriority.LocalValue && !IsOverridenCurrentValue))
                Priority = BindingPriority.Unset;
            if (clearLocalValue || (BasePriority != BindingPriority.LocalValue && !IsOverridenCurrentValue))
                BasePriority = BindingPriority.Unset;
        }

        /// <summary>
        /// Ends a reevaluation pass on the effective value.
        /// </summary>
        /// <param name="owner">The associated value store.</param>
        /// <param name="property">The property being reevaluated.</param>
        /// <remarks>
        /// Handles coercing the default value if necessary.
        /// </remarks>
        public void EndReevaluation(ValueStore owner, AvaloniaProperty property)
        {
            if (Priority == BindingPriority.Unset && HasCoercion)
                CoerceDefaultValueAndRaise(owner, property);
        }

        /// <summary>
        /// Gets a value indicating whether the effective value represents the default value of the
        /// property and can be removed.
        /// </summary>
        /// <returns>True if the effective value van be removed; otherwise false.</returns>
        public bool CanRemove()
        {
            return Priority == BindingPriority.Unset &&
                !IsOverridenCurrentValue &&
                !IsCoercedDefaultValue;
        }

        /// <summary>
        /// Unsubscribes from any unused value entries.
        /// </summary>
        public void UnsubscribeIfNecessary()
        {
            if (Priority == BindingPriority.Unset)
            {
                ValueEntry?.Unsubscribe();
                ValueEntry = null;
            }

            if (BasePriority == BindingPriority.Unset)
            {
                BaseValueEntry?.Unsubscribe();
                BaseValueEntry = null;
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
        /// Sets the value and base value for a LocalValue priority, raising 
        /// <see cref="AvaloniaObject.PropertyChanged"/> where necessary.
        /// </summary>
        /// <param name="owner">The associated value store.</param>
        /// <param name="property">The property being changed.</param>
        /// <param name="value">The new value of the property.</param>
        public abstract void SetLocalValueAndRaise(
            ValueStore owner,
            AvaloniaProperty property,
            object? value);

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

        /// <summary>
        /// Coerces the default value, raising <see cref="AvaloniaObject.PropertyChanged"/>
        /// where necessary.
        /// </summary>
        /// <param name="owner">The associated value store.</param>
        /// <param name="property">The property being coerced.</param>
        protected abstract void CoerceDefaultValueAndRaise(ValueStore owner, AvaloniaProperty property);

        /// <summary>
        /// Gets the current effective value as a boxed value.
        /// </summary>
        protected abstract object? GetBoxedValue();

        protected void UpdateValueEntry(IValueEntry? entry, BindingPriority priority)
        {
            Debug.Assert(priority != BindingPriority.LocalValue);

            if (priority <= BindingPriority.Animation)
            {
                // If we've received an animation value and the current value is a non-animation
                // value, then the current entry becomes our base entry.
                if (Priority > BindingPriority.LocalValue && Priority < BindingPriority.Inherited)
                {
                    Debug.Assert(ValueEntry is not null);
                    BaseValueEntry = ValueEntry;
                    ValueEntry = null;
                }

                if (ValueEntry != entry)
                {
                    ValueEntry?.Unsubscribe();
                    ValueEntry = entry;
                }
            }
            else if (Priority <= BindingPriority.Animation)
            {
                // We've received a non-animation value and have an active animation value, so the
                // new entry becomes our base entry.
                if (BaseValueEntry != entry)
                {
                    BaseValueEntry?.Unsubscribe();
                    BaseValueEntry = entry;
                }
            }
            else if (ValueEntry != entry)
            {
                // Both the current value and the new value are non-animation values, so the new
                // entry replaces the existing entry.
                ValueEntry?.Unsubscribe();
                ValueEntry = entry;
            }
        }
    }
}
