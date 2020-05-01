using System;
using System.Collections.Generic;
using Avalonia.Data;

#nullable enable

namespace Avalonia.PropertyStore
{
    /// <summary>
    /// Stores a set of prioritized values and bindings in a <see cref="ValueStore"/>.
    /// </summary>
    /// <typeparam name="T">The property type.</typeparam>
    /// <remarks>
    /// When more than a single value or binding is applied to a property in an
    /// <see cref="AvaloniaObject"/>, the entry in the <see cref="ValueStore"/> is converted into
    /// a <see cref="PriorityValue{T}"/>. This class holds any number of
    /// <see cref="IPriorityValueEntry{T}"/> entries (sorted first by priority and then in the order
    /// they were added) plus a local value.
    /// </remarks>
    internal class PriorityValue<T> : IValue<T>, IValueSink
    {
        private readonly IAvaloniaObject _owner;
        private readonly IValueSink _sink;
        private readonly List<IPriorityValueEntry<T>> _entries = new List<IPriorityValueEntry<T>>();
        private readonly Func<IAvaloniaObject, T, T>? _coerceValue;
        private Optional<T> _localValue;

        public PriorityValue(
            IAvaloniaObject owner,
            StyledPropertyBase<T> property,
            IValueSink sink)
        {
            _owner = owner;
            Property = property;
            _sink = sink;

            if (property.HasCoercion)
            {
                var metadata = property.GetMetadata(owner.GetType());
                _coerceValue = metadata.CoerceValue;
            }
        }

        public PriorityValue(
            IAvaloniaObject owner,
            StyledPropertyBase<T> property,
            IValueSink sink,
            IPriorityValueEntry<T> existing)
            : this(owner, property, sink)
        {
            existing.Reparent(this);
            _entries.Add(existing);
            
            if (existing.Value.HasValue)
            {
                Value = existing.Value;
                ValuePriority = existing.Priority;
            }
        }

        public PriorityValue(
            IAvaloniaObject owner,
            StyledPropertyBase<T> property,
            IValueSink sink,
            LocalValueEntry<T> existing)
            : this(owner, property, sink)
        {
            _localValue = existing.Value;
            Value = _localValue;
            ValuePriority = BindingPriority.LocalValue;
        }

        public StyledPropertyBase<T> Property { get; }
        public Optional<T> Value { get; private set; }
        public BindingPriority ValuePriority { get; private set; }
        public IReadOnlyList<IPriorityValueEntry<T>> Entries => _entries;
        Optional<object> IValue.Value => Value.ToObject();

        public void ClearLocalValue() => UpdateEffectiveValue();

        public IDisposable? SetValue(T value, BindingPriority priority)
        {
            IDisposable? result = null;

            if (priority == BindingPriority.LocalValue)
            {
                _localValue = value;
            }
            else
            {
                var insert = FindInsertPoint(priority);
                var entry = new ConstantValueEntry<T>(Property, value, priority, this);
                _entries.Insert(insert, entry);
                result = entry;
            }

            UpdateEffectiveValue();
            return result;
        }

        public BindingEntry<T> AddBinding(IObservable<BindingValue<T>> source, BindingPriority priority)
        {
            var binding = new BindingEntry<T>(_owner, Property, source, priority, this);
            var insert = FindInsertPoint(binding.Priority);
            _entries.Insert(insert, binding);
            return binding;
        }

        public void CoerceValue() => UpdateEffectiveValue();

        void IValueSink.ValueChanged<TValue>(
            StyledPropertyBase<TValue> property,
            BindingPriority priority,
            Optional<TValue> oldValue,
            BindingValue<TValue> newValue)
        {
            if (priority == BindingPriority.LocalValue)
            {
                _localValue = default;
            }

            UpdateEffectiveValue();
        }

        void IValueSink.Completed<TValue>(
            StyledPropertyBase<TValue> property,
            IPriorityValueEntry entry,
            Optional<TValue> oldValue)
        {
            _entries.Remove((IPriorityValueEntry<T>)entry);
            UpdateEffectiveValue();
        }

        private int FindInsertPoint(BindingPriority priority)
        {
            var result = _entries.Count;

            for (var i = 0; i < _entries.Count; ++i)
            {
                if (_entries[i].Priority < priority)
                {
                    result = i;
                    break;
                }
            }

            return result;
        }

        private void UpdateEffectiveValue()
        {
            var reachedLocalValues = false;
            var value = default(Optional<T>);

            if (_entries.Count > 0)
            {
                for (var i = _entries.Count - 1; i >= 0; --i)
                {
                    var entry = _entries[i];

                    if (!reachedLocalValues && entry.Priority >= BindingPriority.LocalValue)
                    {
                        reachedLocalValues = true;

                        if (_localValue.HasValue)
                        {
                            value = _localValue;
                            ValuePriority = BindingPriority.LocalValue;
                            break;
                        }
                    }

                    if (entry.Value.HasValue)
                    {
                        value = entry.Value;
                        ValuePriority = entry.Priority;
                        break;
                    }
                }
            }
            else if (_localValue.HasValue)
            {
                value = _localValue;
                ValuePriority = BindingPriority.LocalValue;
            }

            if (value.HasValue && _coerceValue != null)
            {
                value = _coerceValue(_owner, value.Value);
            }

            if (value != Value)
            {
                var old = Value;
                Value = value;
                _sink.ValueChanged(Property, ValuePriority, old, value);
            }
        }
    }
}
