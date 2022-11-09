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

        public EffectiveValue(AvaloniaObject owner, StyledPropertyBase<T> property)
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

                Value = coerce(owner, value);
            }
            else
            {
                Value = value;
            }
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

            SetAndRaiseCore(owner,  (StyledPropertyBase<T>)value.Property, GetValue(value), priority);
        }

        public void SetLocalValueAndRaise(
            ValueStore owner,
            StyledPropertyBase<T> property,
            T value)
        {
            SetAndRaiseCore(owner, property, value, BindingPriority.LocalValue);
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

            var p = (StyledPropertyBase<T>)property;
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
            SetAndRaiseCore(owner, (StyledPropertyBase<T>)property, _baseValue!, BasePriority);
        }

        public override void CoerceValue(ValueStore owner, AvaloniaProperty property)
        {
            if (_uncommon is null)
                return;
            SetAndRaiseCore(
                owner, 
                (StyledPropertyBase<T>)property, 
                _uncommon._uncoercedValue!, 
                Priority, 
                _uncommon._uncoercedBaseValue!,
                BasePriority);
        }

        public override void DisposeAndRaiseUnset(ValueStore owner, AvaloniaProperty property)
        {
            UnsubscribeValueEntries();
            DisposeAndRaiseUnset(owner, (StyledPropertyBase<T>)property);
        }

        public void DisposeAndRaiseUnset(ValueStore owner, StyledPropertyBase<T> property)
        {
            BindingPriority priority;
            T oldValue;

            if (property.Inherits && owner.TryGetInheritedValue(property, out var i))
            {
                oldValue = ((EffectiveValue<T>)i).Value;
                priority = BindingPriority.Inherited;
            }
            else
            {
                oldValue = _metadata.DefaultValue;
                priority = BindingPriority.Unset;
            }

            if (!EqualityComparer<T>.Default.Equals(oldValue, Value))
            {
                owner.Owner.RaisePropertyChanged(property, Value, oldValue, priority, true);
                if (property.Inherits)
                    owner.OnInheritedEffectiveValueDisposed(property, Value);
            }
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
            StyledPropertyBase<T> property,
            T value,
            BindingPriority priority)
        {
            Debug.Assert(priority < BindingPriority.Inherited);

            var oldValue = Value;
            var valueChanged = false;
            var baseValueChanged = false;
            var v = value;

            if (_uncommon?._coerce is { } coerce)
                v = coerce(owner.Owner, value);

            if (priority <= Priority)
            {
                valueChanged = !EqualityComparer<T>.Default.Equals(Value, v);
                Value = v;
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
            StyledPropertyBase<T> property,
            T value,
            BindingPriority priority,
            T baseValue,
            BindingPriority basePriority)
        {
            Debug.Assert(priority < BindingPriority.Inherited);
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
                bv = coerce(owner.Owner, baseValue);
            }

            if (priority != BindingPriority.Unset && !EqualityComparer<T>.Default.Equals(Value, v))
            {
                Value = v;
                valueChanged = true;
                if (_uncommon is not null)
                    _uncommon._uncoercedValue = value;
            }

            if (priority != BindingPriority.Unset &&
                (BasePriority == BindingPriority.Unset ||
                 !EqualityComparer<T>.Default.Equals(_baseValue, bv)))
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
            public Func<IAvaloniaObject, T, T>? _coerce;
            public T? _uncoercedValue;
            public T? _uncoercedBaseValue;
        }
    }
}
