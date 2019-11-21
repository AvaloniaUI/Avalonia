using System;
using System.Collections.Generic;
using Avalonia.Data;
using Avalonia.PropertyStore;
using Avalonia.Utilities;

#nullable enable

namespace Avalonia
{
    internal class ValueStore : IValueSink
    {
        private readonly AvaloniaObject _owner;
        private readonly IValueSink _sink;
        private readonly AvaloniaPropertyValueStore<object> _values;

        public ValueStore(AvaloniaObject owner)
        {
            _sink = _owner = owner;
            _values = new AvaloniaPropertyValueStore<object>();
        }

        public bool IsAnimating(AvaloniaProperty property)
        {
            if (_values.TryGetValue(property, out var slot))
            {
                if (slot is IValue v)
                {
                    return v.ValuePriority < BindingPriority.LocalValue;
                }
            }

            return false;
        }

        public bool IsSet(AvaloniaProperty property)
        {
            if (_values.TryGetValue(property, out var slot))
            {
                if (slot is IValue v)
                {
                    return v.Value.HasValue;
                }
            }

            return false;
        }

        public bool TryGetValue<T>(StyledPropertyBase<T> property, out T value)
        {
            if (_values.TryGetValue(property, out var slot))
            {
                if (slot is IValue<T> v)
                {
                    if (v.Value.HasValue)
                    {
                        value = v.Value.Value;
                        return true;
                    }
                }
            }

            value = default!;
            return false;
        }

        public void SetValue<T>(StyledPropertyBase<T> property, T value, BindingPriority priority)
        {
            if (_values.TryGetValue(property, out var slot))
            {
                SetExisting(slot, property, value, priority);
            }
            else if (priority == BindingPriority.LocalValue)
            {
                _values.AddValue(property, new LocalValueEntry<T>(value));
                _sink.ValueChanged(property, priority, default, value);
            }
            else
            {
                var entry = new ConstantValueEntry<T>(property, value, priority);
                _values.AddValue(property, entry);
                _sink.ValueChanged(property, priority, default, value);
            }
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
            else
            {
                var entry = new BindingEntry<T>(_owner, property, source, priority, this);
                _values.AddValue(property, entry);
                entry.Start();
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
                        var old = TryGetValue(property, out var value) ? value : default;
                        _values.Remove(property);
                        _sink.ValueChanged(
                            property,
                            BindingPriority.LocalValue,
                            old,
                            BindingValue<T>.Unset);
                    }
                }
            }
        }

        void IValueSink.ValueChanged<T>(
            StyledPropertyBase<T> property,
            BindingPriority priority,
            Optional<T> oldValue,
            BindingValue<T> newValue)
        {
            _sink.ValueChanged(property, priority, oldValue, newValue);
        }

        void IValueSink.Completed(AvaloniaProperty property, IPriorityValueEntry entry)
        {
            if (_values.TryGetValue(property, out var slot))
            {
                if (slot == entry)
                {
                    _values.Remove(property);
                }
            }
        }

        private void SetExisting<T>(
            object slot,
            StyledPropertyBase<T> property,
            T value,
            BindingPriority priority)
        {
            if (slot is IPriorityValueEntry<T> e)
            {
                var priorityValue = new PriorityValue<T>(_owner, property, this, e);
                _values.SetValue(property, priorityValue);
                priorityValue.SetValue(value, priority);
            }
            else if (slot is PriorityValue<T> p)
            {
                p.SetValue(value, priority);
            }
            else if (slot is LocalValueEntry<T> l)
            {
                if (priority == BindingPriority.LocalValue)
                {
                    var old = l.Value;
                    l.SetValue(value);
                    _sink.ValueChanged(property, priority, old, value);
                }
                else
                {
                    var priorityValue = new PriorityValue<T>(_owner, property, this, l);
                    _values.SetValue(property, priorityValue);
                }
            }
            else
            {
                throw new NotSupportedException("Unrecognised value store slot type.");
            }
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
            binding.Start();
            return binding;
        }
    }
}
