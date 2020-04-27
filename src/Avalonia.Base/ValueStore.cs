using System;
using System.Net.Http.Headers;
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

        public ValueStore(AvaloniaObject owner)
        {
            _sink = _owner = owner;
            _values = new AvaloniaPropertyValueStore<IValue>();
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

            var change = new AvaloniaPropertyChangedEventArgs<T>(_owner, property, default, value, priority);
            IDisposable? result = null;

            if (_values.TryGetValue(property, out var slot))
            {
                result = SetExisting(slot, property, value, priority);
            }
            else if (property.HasCoercion)
            {
                // If the property has any coercion callbacks then always create a PriorityValue.
                var entry = new PriorityValue<T>(_owner, property, this);
                _values.AddValue(property, entry);
                result = entry.SetValue(value, priority);
            }
            else if (priority == BindingPriority.LocalValue)
            {
                _values.AddValue(property, new LocalValueEntry<T>(value));
                _sink.ValueChanged(change);
            }
            else
            {
                var entry = new ConstantValueEntry<T>(property, value, priority, this);
                _values.AddValue(property, entry);
                _sink.ValueChanged(change);
                result = entry;
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
                _values.AddValue(property, entry);
                binding.Start();
                return binding;
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
                        var old = TryGetValue(property, BindingPriority.LocalValue, out var value) ? value : default;
                        _values.Remove(property);
                        _sink.ValueChanged(new AvaloniaPropertyChangedEventArgs<T>(
                            _owner,
                            property,
                            old,
                            default,
                            BindingPriority.Unset));
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
                    p.CoerceValue();
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
            _sink.ValueChanged(change);
        }

        void IValueSink.Completed<T>(
            StyledPropertyBase<T> property,
            IPriorityValueEntry entry,
            Optional<T> oldValue)
        {
            if (_values.TryGetValue(property, out var slot))
            {
                if (slot == entry)
                {
                    _values.Remove(property);
                    _sink.Completed(property, entry, oldValue);
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
                    _sink.ValueChanged(new AvaloniaPropertyChangedEventArgs<T>(
                        _owner,
                        property,
                        old,
                        value,
                        priority));
                }
                else
                {
                    var priorityValue = new PriorityValue<T>(_owner, property, this, l);
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
