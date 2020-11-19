using System;
using System.Collections.Generic;
using Avalonia.Data;
using Avalonia.PropertyStore;
using Avalonia.Utilities;

#nullable enable

namespace Avalonia
{
    /// <summary>
    /// Stores styled property values for an <see cref="AvaloniaObject"/>.
    /// </summary>
    /// <remarks>
    /// At its core this class consists of an <see cref="AvaloniaProperty"/> to 
    /// <see cref="IValue"/> mapping which holds the current values for each set property. This
    /// <see cref="IValue"/> can be in one of 4 states:
    /// 
    /// - For a single local value it will be an instance of <see cref="LocalValueEntry{T}"/>.
    /// - For a single value of a priority other than LocalValue it will be an instance of
    ///   <see cref="ConstantValueEntry{T}"/>`
    /// - For a single binding it will be an instance of <see cref="BindingEntry{T}"/>
    /// - For all other cases it will be an instance of <see cref="PriorityValue{T}"/>
    /// </remarks>
    internal class ValueStore : IValueSink
    {
        private readonly AvaloniaObject _owner;
        private readonly IValueSink _sink;
        private readonly AvaloniaPropertyValueStore<IValue> _values;
        private List<PropertyUpdate>? _batchUpdate;

        public ValueStore(AvaloniaObject owner)
        {
            _sink = _owner = owner;
            _values = new AvaloniaPropertyValueStore<IValue>();
        }

        public void BeginBatchUpdate()
        {
            if (_batchUpdate is object)
            {
                throw new InvalidOperationException("Batch update already in progress.");
            }

            _batchUpdate = new List<PropertyUpdate>();

            for (var i = 0; i < _values.Count; ++i)
            {
                (_values[i] as IBatchUpdate)?.BeginBatchUpdate();
            }
        }

        public void EndBatchUpdate()
        {
            if (_batchUpdate is null)
            {
                throw new InvalidOperationException("No batch update in progress.");
            }

            for (var i = 0; i < _values.Count; ++i)
            {
                (_values[i] as IBatchUpdate)?.EndBatchUpdate();
            }

            foreach (var entry in _batchUpdate)
            {
                if (_values.TryGetValue(entry.property, out var slot))
                {
                    slot.RaiseValueChanged(_sink, _owner, entry.property, entry.oldValue);

                    if (slot.Priority == BindingPriority.Unset)
                    {
                        _values.Remove(entry.property);
                    }
                }
                else
                {
                    throw new AvaloniaInternalException("Value could not be found at the end of batch update.");
                }
            }

            _batchUpdate = null;
        }

        public bool IsAnimating(AvaloniaProperty property)
        {
            if (_values.TryGetValue(property, out var slot))
            {
                return slot.Priority < BindingPriority.LocalValue;
            }

            return false;
        }

        public bool IsSet(AvaloniaProperty property)
        {
            if (_values.TryGetValue(property, out var slot))
            {
                return slot.GetValue().HasValue;
            }

            return false;
        }

        public bool TryGetValue<T>(
            StyledPropertyBase<T> property,
            BindingPriority maxPriority,
            out T value)
        {
            if (_values.TryGetValue(property, out var slot))
            {
                var v = ((IValue<T>)slot).GetValue(maxPriority);

                if (v.HasValue)
                {
                    value = v.Value;
                    return true;
                }
            }

            value = default!;
            return false;
        }

        public IDisposable? SetValue<T>(StyledPropertyBase<T> property, T value, BindingPriority priority)
        {
            if (property.ValidateValue?.Invoke(value) == false)
            {
                throw new ArgumentException($"{value} is not a valid value for '{property.Name}.");
            }

            IDisposable? result = null;

            if (_values.TryGetValue(property, out var slot))
            {
                result = SetExisting(slot, property, value, priority);
            }
            else if (property.HasCoercion)
            {
                // If the property has any coercion callbacks then always create a PriorityValue.
                var entry = new PriorityValue<T>(_owner, property, this);
                AddValue(property, entry);
                result = entry.SetValue(value, priority);
            }
            else
            {
                if (priority == BindingPriority.LocalValue)
                {
                    AddValue(property, new LocalValueEntry<T>(value));
                    NotifyValueChanged<T>(property, default, value, priority);
                }
                else
                {
                    var entry = new ConstantValueEntry<T>(property, value, priority, this);
                    AddValue(property, entry);
                    NotifyValueChanged<T>(property, default, value, priority);
                    result = entry;
                }
            }

            return result;
        }

        public IDisposable AddBinding<T>(
            StyledPropertyBase<T> property,
            IObservable<BindingValue<T>> source,
            BindingPriority priority)
        {
            if (_values.TryGetValue(property, out var slot))
            {
                return BindExisting(slot, property, source, priority);
            }
            else if (property.HasCoercion)
            {
                // If the property has any coercion callbacks then always create a PriorityValue.
                var entry = new PriorityValue<T>(_owner, property, this);
                var binding = entry.AddBinding(source, priority);
                AddValue(property, entry);
                return binding;
            }
            else
            {
                var entry = new BindingEntry<T>(_owner, property, source, priority, this);
                AddValue(property, entry);
                return entry;
            }
        }

        public void ClearLocalValue<T>(StyledPropertyBase<T> property)
        {
            if (_values.TryGetValue(property, out var slot))
            {
                if (slot is PriorityValue<T> p)
                {
                    p.ClearLocalValue();
                }
                else
                {
                    var remove = slot is ConstantValueEntry<T> c ?
                        c.Priority == BindingPriority.LocalValue : 
                        !(slot is IPriorityValueEntry<T>);

                    if (remove)
                    {
                        var old = TryGetValue(property, BindingPriority.LocalValue, out var value) ? value : default;
                        _values.Remove(property);
                        NotifyValueChanged<T>(property, old, default, BindingPriority.Unset);
                    }
                }
            }
        }

        public void CoerceValue<T>(StyledPropertyBase<T> property)
        {
            if (_values.TryGetValue(property, out var slot))
            {
                if (slot is PriorityValue<T> p)
                {
                    p.UpdateEffectiveValue();
                }
            }
        }

        public Diagnostics.AvaloniaPropertyValue? GetDiagnostic(AvaloniaProperty property)
        {
            if (_values.TryGetValue(property, out var slot))
            {
                var slotValue = slot.GetValue();
                return new Diagnostics.AvaloniaPropertyValue(
                    property,
                    slotValue.HasValue ? slotValue.Value : AvaloniaProperty.UnsetValue,
                    slot.Priority,
                    null);
            }

            return null;
        }

        void IValueSink.ValueChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            if (_batchUpdate is object)
            {
                NotifyValueChanged<T>(change.Property, change.OldValue, change.NewValue, change.Priority);
            }
            else
            {
                _sink.ValueChanged(change);
            }
        }

        void IValueSink.Completed<T>(
            StyledPropertyBase<T> property,
            IPriorityValueEntry entry,
            Optional<T> oldValue)
        {
            if (_values.TryGetValue(property, out var slot) && slot == entry)
            {
                if (_batchUpdate is null)
                {
                    _values.Remove(property);
                    _sink.Completed(property, entry, oldValue);
                }
                else
                {
                    NotifyValueChanged(property, oldValue, default, BindingPriority.Unset);
                }
            }
        }

        private IDisposable? SetExisting<T>(
            object slot,
            StyledPropertyBase<T> property,
            T value,
            BindingPriority priority)
        {
            IDisposable? result = null;

            if (slot is IPriorityValueEntry<T> e)
            {
                var priorityValue = new PriorityValue<T>(_owner, property, this, e);
                _values.SetValue(property, priorityValue);
                result = priorityValue.SetValue(value, priority);
            }
            else if (slot is PriorityValue<T> p)
            {
                result = p.SetValue(value, priority);
            }
            else if (slot is LocalValueEntry<T> l)
            {
                if (priority == BindingPriority.LocalValue)
                {
                    var old = l.GetValue(BindingPriority.LocalValue);
                    l.SetValue(value);
                    NotifyValueChanged<T>(property, old, value, priority);
                }
                else
                {
                    var priorityValue = new PriorityValue<T>(_owner, property, this, l);
                    if (_batchUpdate is object)
                        priorityValue.BeginBatchUpdate();
                    result = priorityValue.SetValue(value, priority);
                    _values.SetValue(property, priorityValue);
                }
            }
            else
            {
                throw new NotSupportedException("Unrecognised value store slot type.");
            }

            return result;
        }

        private IDisposable BindExisting<T>(
            object slot,
            StyledPropertyBase<T> property,
            IObservable<BindingValue<T>> source,
            BindingPriority priority)
        {
            PriorityValue<T> priorityValue;

            if (slot is IPriorityValueEntry<T> e)
            {
                priorityValue = new PriorityValue<T>(_owner, property, this, e);

                if (_batchUpdate is object)
                {
                    priorityValue.BeginBatchUpdate();
                }
            }
            else if (slot is PriorityValue<T> p)
            {
                priorityValue = p;
            }
            else if (slot is LocalValueEntry<T> l)
            {
                priorityValue = new PriorityValue<T>(_owner, property, this, l);
            }
            else
            {
                throw new NotSupportedException("Unrecognised value store slot type.");
            }

            var binding = priorityValue.AddBinding(source, priority);
            _values.SetValue(property, priorityValue);
            priorityValue.UpdateEffectiveValue();
            return binding;
        }

        private void AddValue(AvaloniaProperty property, IValue value)
        {
            _values.AddValue(property, value);
            if (_batchUpdate is object && value is IBatchUpdate batch)
                batch.BeginBatchUpdate();
            value.Start();
        }

        private void NotifyValueChanged<T>(
            AvaloniaProperty<T> property,
            Optional<T> oldValue,
            BindingValue<T> newValue,
            BindingPriority priority)
        {
            if (_batchUpdate is object)
            {
                var oldValueBoxed = oldValue.ToObject();

                for (var i = 0; i < _batchUpdate.Count; ++i)
                {
                    if (_batchUpdate[i].property == property)
                    {
                        oldValueBoxed = _batchUpdate[i].oldValue;
                        _batchUpdate.RemoveAt(i);
                        break;
                    }
                }

                _batchUpdate.Add(new PropertyUpdate
                {
                    property = property,
                    oldValue = oldValueBoxed,
                });
            }
            else
            {
                _sink.ValueChanged(new AvaloniaPropertyChangedEventArgs<T>(
                    _owner,
                    property,
                    oldValue,
                    newValue,
                    priority));
            }
        }

        private struct PropertyUpdate
        {
            public AvaloniaProperty property;
            public Optional<object> oldValue;
        }
    }
}
