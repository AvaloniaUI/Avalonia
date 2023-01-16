using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Data;

namespace Avalonia.PropertyStore
{
    /// <summary>
    /// Represents the active value for a property in a <see cref="ValueStore"/>.
    /// </summary>
    /// <remarks>
    /// Stores the active value in an <see cref="AvaloniaObject"/>'s <see cref="ValueStore"/>
    /// for a single property, when the value is not inherited or unset/default.
    /// </remarks>
    internal sealed class EffectiveValue<T> : EffectiveValue
    {
        private readonly StyledPropertyMetadata<T> _metadata;
        private T? _baseValue;
        private UncommonFields? _uncommon;

        public EffectiveValue(AvaloniaObject owner, StyledProperty<T> property)
        {
            Priority = BindingPriority.Unset;
            BasePriority = BindingPriority.Unset;
            _metadata = property.GetMetadata(owner.GetType());

            var value = _metadata.DefaultValue;

            if (property.HasCoercion && _metadata.CoerceValue is { } coerce)
            {
                _uncommon = new()
                {
                    _coerce = coerce,
                    _uncoercedValue = value,
                    _uncoercedBaseValue = value,
                };

                LastPrioritizedValue = coerce(owner, value);
            }
            else
            {
                LastPrioritizedValue = value;
            }
        }

        /// <inheritdoc/>
        public override bool IsCurrent => _uncommon is { _hasCurrentValue: true };

        /// <summary>
        /// Gets the last value which was set with a <see cref="BindingPriority"/>.
        /// </summary>
        /// <seealso cref="ActiveValue"/>
        public T LastPrioritizedValue { get; private set; }

        /// <summary>
        /// Gets either the value set with <see cref="SetCurrentValue"/>, or <see cref="LastPrioritizedValue"/>.
        /// </summary>
        public T ActiveValue => _uncommon is { _hasCurrentValue: true } uncommon ? uncommon._currentValue! : LastPrioritizedValue;

        protected override object? GetBoxedValue() => ActiveValue;

        public override void SetAndRaise(
            ValueStore owner,
            IValueEntry value, 
            BindingPriority priority, 
            bool clearCurrentValue = false)
        {
            Debug.Assert(priority != BindingPriority.LocalValue);
            UpdateValueEntry(value, priority);

            SetAndRaiseCore(owner,  (StyledProperty<T>)value.Property, GetValue(value), priority, clearCurrentValue);
        }

        /// <summary>
        /// Sets a new value with local priority. Always clears the value from <see cref="SetCurrentValue"/>.
        /// </summary>
        /// <param name="owner">The associated value store.</param>
        /// <param name="property">The property to set the value on.</param>
        /// <param name="value">The new value of the property.</param>
        public void SetLocalValueAndRaise(
            ValueStore owner,
            StyledProperty<T> property,
            T value)
        {
            SetAndRaiseCore(owner, property, value, BindingPriority.LocalValue, true);
        }

        public bool TryGetBaseValue([MaybeNullWhen(false)] out T value)
        {
            value = _baseValue!;
            return BasePriority != BindingPriority.Unset;
        }

        public override void RaiseInheritedValueChanged(
            AvaloniaObject owner,
            AvaloniaProperty property,
            EffectiveValue? oldValue,
            EffectiveValue? newValue)
        {
            Debug.Assert(oldValue is not null || newValue is not null);

            var p = (StyledProperty<T>)property;
            var o = oldValue is not null ? ((EffectiveValue<T>)oldValue).ActiveValue : _metadata.DefaultValue;
            var n = newValue is not null ? ((EffectiveValue<T>)newValue).ActiveValue : _metadata.DefaultValue;
            var priority = newValue is not null ? BindingPriority.Inherited : BindingPriority.Unset;

            if (!EqualityComparer<T>.Default.Equals(o, n))
            {
                owner.RaisePropertyChanged(p, o, n, priority, true);
            }
        }

        public override void RemoveAnimationAndRaise(ValueStore owner, AvaloniaProperty property)
        {
            Debug.Assert(Priority != BindingPriority.Animation);
            Debug.Assert(BasePriority != BindingPriority.Unset);
            UpdateValueEntry(null, BindingPriority.Animation);
            SetAndRaiseCore(owner, (StyledProperty<T>)property, _baseValue!, BasePriority, true);
        }

        public override void CoerceValue(ValueStore owner, AvaloniaProperty property)
        {
            if (_uncommon?._coerce is null)
                return;

            CoerceImpl(
                owner, 
                (StyledProperty<T>)property, 
                _uncommon._uncoercedValue!, 
                Priority, 
                _uncommon._uncoercedBaseValue!,
                BasePriority);
        }

        /// <inheritdoc/>
        public override void SetCurrentValue(ValueStore owner, AvaloniaProperty property, object? value)
        {
            var oldValue = ActiveValue;
            var newValue = (T?)value;

            if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
            {
                return;
            }

            _uncommon ??= new();

            _uncommon._uncoercedValue = newValue;

            if (_uncommon._coerce != null)
            {
                newValue = _uncommon._coerce(owner.Owner, newValue!);
            }

            _uncommon._currentValue = newValue;
            _uncommon._hasCurrentValue = true;

            if (Priority >= BindingPriority.Inherited)
            {
                Priority = BindingPriority.Internal;
            }

            RaiseValueChanged(owner, (StyledProperty<T>)property, oldValue!);
        }

        public override void RemoveCurrentValue()
        {
            if (_uncommon is not { _hasCurrentValue: true } uncommon)
            {
                return;
            }

            Priority = BindingPriority.Unset; // prepare for ValueStore.ReevaluateEffectiveValue

            if (uncommon._coerce == null)
            {
                _uncommon = null; // we have no more use for this object
            }
            else
            {
                uncommon._currentValue = default;
                uncommon._hasCurrentValue = false;
            }
        }

        public override void DisposeAndRaiseUnset(ValueStore owner, AvaloniaProperty property)
        {
            UnsubscribeValueEntries();
            DisposeAndRaiseUnset(owner, (StyledProperty<T>)property);
        }

        public void DisposeAndRaiseUnset(ValueStore owner, StyledProperty<T> property)
        {
            BindingPriority priority;
            T oldValue;

            if (property.Inherits && owner.TryGetInheritedValue(property, out var i))
            {
                oldValue = ((EffectiveValue<T>)i).ActiveValue;
                priority = BindingPriority.Inherited;
            }
            else
            {
                oldValue = _metadata.DefaultValue;
                priority = BindingPriority.Unset;
            }

            if (!EqualityComparer<T>.Default.Equals(oldValue, ActiveValue))
            {
                owner.Owner.RaisePropertyChanged(property, ActiveValue, oldValue, priority, true);
                if (property.Inherits)
                    owner.OnInheritedEffectiveValueDisposed(property, ActiveValue);
            }
        }
        
        private static T GetValue(IValueEntry entry)
        {
            if (entry is IValueEntry<T> typed)
                return typed.GetValue();
            else
                return (T)entry.GetValue()!;
        }

        private void SetAndRaiseCore(
            ValueStore owner,
            StyledProperty<T> property,
            T value,
            BindingPriority priority,
            bool clearCurrentValue)
        {
            Debug.Assert(priority < BindingPriority.Inherited);

            var oldValue = ActiveValue;
            var valueChanged = false;
            var baseValueChanged = false;
            var v = value;

            if (_uncommon?._coerce is { } coerce)
                v = coerce(owner.Owner, value);

            if (priority <= Priority)
            {
                valueChanged = !EqualityComparer<T>.Default.Equals(oldValue, v);
                LastPrioritizedValue = v;
                Priority = priority;
                if (_uncommon is not null)
                    _uncommon._uncoercedValue = value;
            }

            if (priority <= BasePriority && priority >= BindingPriority.LocalValue)
            {
                baseValueChanged = !EqualityComparer<T>.Default.Equals(_baseValue, v);
                _baseValue = v;
                BasePriority = priority;
                if (_uncommon is not null)
                    _uncommon._uncoercedBaseValue = value;
            }

            if (clearCurrentValue)
            {
                RemoveCurrentValue();
            }

            if (valueChanged)
            {
                RaiseValueChanged(owner, property, oldValue);
            }
            else if (baseValueChanged)
            {
                owner.Owner.RaisePropertyChanged(property, default, _baseValue!, BasePriority, false);
            }
        }

        private void CoerceImpl(
            ValueStore owner,
            StyledProperty<T> property,
            T value,
            BindingPriority priority,
            T baseValue,
            BindingPriority basePriority)
        {
            Debug.Assert(priority < BindingPriority.Inherited);
            Debug.Assert(basePriority > BindingPriority.Animation);
            Debug.Assert(priority <= basePriority);
            Debug.Assert(_uncommon?._coerce is not null);

            var oldValue = ActiveValue;
            var valueChanged = false;
            var baseValueChanged = false;

            var coercedValue = _uncommon!._coerce!(owner.Owner, value);
            var coercedBaseValue = _uncommon._coerce(owner.Owner, baseValue);

            if (priority != BindingPriority.Unset && !EqualityComparer<T>.Default.Equals(oldValue, coercedValue))
            {
                if (_uncommon._hasCurrentValue)
                {
                    _uncommon._currentValue = coercedValue;
                }
                else
                {
                    LastPrioritizedValue = coercedValue;
                }

                valueChanged = true;
                _uncommon._uncoercedValue = value;
            }

            if (priority != BindingPriority.Unset &&
                (BasePriority == BindingPriority.Unset ||
                 !EqualityComparer<T>.Default.Equals(_baseValue, coercedBaseValue)))
            {
                _baseValue = coercedValue;
                baseValueChanged = true;
                _uncommon._uncoercedValue = baseValue;
            }

            Priority = priority;
            BasePriority = basePriority;

            if (valueChanged)
            {
                RaiseValueChanged(owner, property, oldValue);
            }
            
            if (baseValueChanged)
            {
                owner.Owner.RaisePropertyChanged(property, default, _baseValue!, BasePriority, false);
            }
        }

        private void RaiseValueChanged(ValueStore owner, StyledProperty<T> property, T oldValue)
        {
            using var notifying = PropertyNotifying.Start(owner.Owner, property);
            owner.Owner.RaisePropertyChanged(property, oldValue, ActiveValue, Priority, true);
            if (property.Inherits)
                owner.OnInheritedEffectiveValueChanged(property, oldValue, this);
        }

        private class UncommonFields
        {
            public Func<AvaloniaObject, T, T>? _coerce;
            public T? _uncoercedValue;
            public T? _uncoercedBaseValue;

            public T? _currentValue;
            public bool _hasCurrentValue;
        }
    }
}
