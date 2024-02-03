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
        private readonly UncommonFields? _uncommon;

        public EffectiveValue(
            AvaloniaObject owner,
            StyledProperty<T> property,
            EffectiveValue<T>? inherited)
        {
            Priority = BindingPriority.Unset;
            BasePriority = BindingPriority.Unset;
            _metadata = property.GetMetadata(owner.GetType());

            var value = inherited is null ? _metadata.DefaultValue : inherited.Value;

            if (_metadata.CoerceValue is { } coerce)
            {
                HasCoercion = true;
                _uncommon = new()
                {
                    _coerce = coerce,
                    _uncoercedValue = value,
                    _uncoercedBaseValue = value,
                };
            }

            Value = value;
        }

        /// <summary>
        /// Gets the current effective value.
        /// </summary>
        public new T Value { get; private set; }

        public override void SetAndRaise(
            ValueStore owner,
            IValueEntry value, 
            BindingPriority priority)
        {
            Debug.Assert(priority != BindingPriority.LocalValue);
            UpdateValueEntry(value, priority);

            SetAndRaiseCore(owner,  (StyledProperty<T>)value.Property, GetValue(value), priority);

            if (priority > BindingPriority.LocalValue &&
                value.GetDataValidationState(out var state, out var error))
            {
                owner.Owner.OnUpdateDataValidation(value.Property, state, error);
            }
        }

        public override void SetLocalValueAndRaise(
            ValueStore owner,
            AvaloniaProperty property,
            object? value)
        {
            SetLocalValueAndRaise(owner, (StyledProperty<T>)property, (T)value!);
        }

        public void SetLocalValueAndRaise(
            ValueStore owner,
            StyledProperty<T> property,
            T value)
        {
            SetAndRaiseCore(owner, property, value, BindingPriority.LocalValue);
        }

        public void SetCurrentValueAndRaise(
            ValueStore owner,
            StyledProperty<T> property,
            T value)
        {
            SetAndRaiseCore(owner, property, value, Priority, isOverriddenCurrentValue: true);
        }

        public void SetCoercedDefaultValueAndRaise(
            ValueStore owner,
            StyledProperty<T> property,
            T value)
        {
            SetAndRaiseCore(owner, property, value, Priority, isCoercedDefaultValue: true);
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
            var o = oldValue is not null ? ((EffectiveValue<T>)oldValue).Value : _metadata.DefaultValue;
            var n = newValue is not null ? ((EffectiveValue<T>)newValue).Value : _metadata.DefaultValue;
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
            SetAndRaiseCore(owner, (StyledProperty<T>)property, _baseValue!, BasePriority);
        }

        public override void CoerceValue(ValueStore owner, AvaloniaProperty property)
        {
            if (_uncommon is null)
                return;
            SetAndRaiseCore(
                owner, 
                (StyledProperty<T>)property, 
                _uncommon._uncoercedValue!, 
                Priority, 
                _uncommon._uncoercedBaseValue!,
                BasePriority);
        }

        public override void DisposeAndRaiseUnset(ValueStore owner, AvaloniaProperty property)
        {
            var clearDataValidation = ValueEntry?.GetDataValidationState(out _, out _) ??
                BaseValueEntry?.GetDataValidationState(out _, out _) ??
                false;

            ValueEntry?.Unsubscribe();
            BaseValueEntry?.Unsubscribe();

            var p = (StyledProperty<T>)property;
            BindingPriority priority;
            T newValue;

            if (property.Inherits && owner.TryGetInheritedValue(property, out var i))
            {
                newValue = ((EffectiveValue<T>)i).Value;
                priority = BindingPriority.Inherited;
            }
            else
            {
                newValue = _metadata.DefaultValue;
                priority = BindingPriority.Unset;
            }

            if (!EqualityComparer<T>.Default.Equals(newValue, Value))
            {
                owner.Owner.RaisePropertyChanged(p, Value, newValue, priority, true);
                if (property.Inherits)
                    owner.OnInheritedEffectiveValueDisposed(p, Value, newValue);
            }

            if (clearDataValidation)
                owner.Owner.OnUpdateDataValidation(p, BindingValueType.UnsetValue, null);
        }

        protected override void CoerceDefaultValueAndRaise(ValueStore owner, AvaloniaProperty property)
        {
            Debug.Assert(_uncommon?._coerce is not null);
            Debug.Assert(Priority == BindingPriority.Unset);

            var coercedDefaultValue = _uncommon!._coerce!(owner.Owner, _metadata.DefaultValue);

            if (!EqualityComparer<T>.Default.Equals(_metadata.DefaultValue, coercedDefaultValue))
                SetCoercedDefaultValueAndRaise(owner, (StyledProperty<T>)property, coercedDefaultValue);
        }

        protected override object? GetBoxedValue() => Value;
        
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
            bool isOverriddenCurrentValue = false,
            bool isCoercedDefaultValue = false)
        {
            var oldValue = Value;
            var valueChanged = false;
            var baseValueChanged = false;
            var v = value;

            IsOverridenCurrentValue = isOverriddenCurrentValue;
            IsCoercedDefaultValue = isCoercedDefaultValue;

            if (!isCoercedDefaultValue && _uncommon?._coerce is { } coerce)
                v = coerce(owner.Owner, value);

            if (priority <= Priority)
            {
                valueChanged = !EqualityComparer<T>.Default.Equals(Value, v);
                Value = v;
                Priority = priority;
                if (!isCoercedDefaultValue && _uncommon is not null)
                    _uncommon._uncoercedValue = value;
            }

            if (priority <= BasePriority && priority >= BindingPriority.LocalValue)
            {
                baseValueChanged = !EqualityComparer<T>.Default.Equals(_baseValue, v);
                _baseValue = v;
                BasePriority = priority;
                if (!isCoercedDefaultValue && _uncommon is not null)
                    _uncommon._uncoercedBaseValue = value;
            }

            if (valueChanged)
            {
                using var notifying = PropertyNotifying.Start(owner.Owner, property);
                owner.Owner.RaisePropertyChanged(property, oldValue, Value, Priority, true);
                if (property.Inherits)
                    owner.OnInheritedEffectiveValueChanged(property, oldValue, this);
            }
            else if (baseValueChanged)
            {
                owner.Owner.RaisePropertyChanged(property, default, _baseValue!, BasePriority, false);
            }
        }

        private void SetAndRaiseCore(
            ValueStore owner,
            StyledProperty<T> property,
            T value,
            BindingPriority priority,
            T baseValue,
            BindingPriority basePriority)
        {
            Debug.Assert(basePriority > BindingPriority.Animation);
            Debug.Assert(priority <= basePriority);

            var oldValue = Value;
            var valueChanged = false;
            var baseValueChanged = false;
            var v = value;
            var bv = baseValue;

            if (_uncommon?._coerce is { } coerce)
            {
                v = coerce(owner.Owner, value);
                if (priority != basePriority)
                    bv = coerce(owner.Owner, baseValue);
            }

            if (!EqualityComparer<T>.Default.Equals(Value, v))
            {
                Value = v;
                valueChanged = true;
                if (_uncommon is not null)
                    _uncommon._uncoercedValue = value;
            }

            if (!EqualityComparer<T>.Default.Equals(_baseValue, bv))
            {
                _baseValue = v;
                baseValueChanged = true;
                if (_uncommon is not null)
                    _uncommon._uncoercedValue = baseValue;
            }

            Priority = priority;
            BasePriority = basePriority;

            if (valueChanged)
            {
                using var notifying = PropertyNotifying.Start(owner.Owner, property);
                owner.Owner.RaisePropertyChanged(property, oldValue, Value, Priority, true);
                if (property.Inherits)
                    owner.OnInheritedEffectiveValueChanged(property, oldValue, this);
            }
            
            if (baseValueChanged)
            {
                owner.Owner.RaisePropertyChanged(property, default, _baseValue!, BasePriority, false);
            }
        }

        private class UncommonFields
        {
            public Func<AvaloniaObject, T, T>? _coerce;
            public T? _uncoercedValue;
            public T? _uncoercedBaseValue;
        }
    }
}
