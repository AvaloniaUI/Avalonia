using System;
using System.Collections.Generic;
using Avalonia.Data;

#nullable enable

namespace Avalonia.PropertyStore
{
    internal class PriorityValue<T> : IValue<T>, IValueSink
    {
        private readonly IValueSink _sink;
        private readonly List<IPriorityValueEntry<T>> _entries = new List<IPriorityValueEntry<T>>();
        private Optional<T> _localValue;

        public PriorityValue(
            StyledPropertyBase<T> property,
            IValueSink sink)
        {
            Property = property;
            _sink = sink;
        }

        public PriorityValue(
            StyledPropertyBase<T> property,
            IValueSink sink,
            IPriorityValueEntry<T> existing)
            : this(property, sink)
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
            StyledPropertyBase<T> property,
            IValueSink sink,
            LocalValueEntry<T> existing)
            : this(property, sink)
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

        public void SetValue(T value, BindingPriority priority)
        {
            if (priority == BindingPriority.LocalValue)
            {
                _localValue = value;
            }
            else
            {
                var insert = FindInsertPoint(priority);
                _entries.Insert(insert, new ConstantValueEntry<T>(Property, value, priority));
            }

            UpdateEffectiveValue();
        }

        public BindingEntry<T> AddBinding(IObservable<BindingValue<T>> source, BindingPriority priority)
        {
            var binding = new BindingEntry<T>(Property, source, priority, this);
            var insert = FindInsertPoint(binding.Priority);
            _entries.Insert(insert, binding);
            return binding;
        }

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

        void IValueSink.Completed(AvaloniaProperty property, IPriorityValueEntry entry)
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

            if (value != Value)
            {
                var old = Value;
                Value = value;
                _sink.ValueChanged(Property, ValuePriority, old, value);
            }
        }
    }
}
