using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private Optional<T> _value;

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

            var v = existing.GetValue();
            
            if (v.HasValue)
            {
                _value = v;
                Priority = existing.Priority;
            }
        }

        public PriorityValue(
            IAvaloniaObject owner,
            StyledPropertyBase<T> property,
            IValueSink sink,
            LocalValueEntry<T> existing)
            : this(owner, property, sink)
        {
            _value = _localValue = existing.GetValue(BindingPriority.LocalValue);
            Priority = BindingPriority.LocalValue;
        }

        public StyledPropertyBase<T> Property { get; }
        public BindingPriority Priority { get; private set; } = BindingPriority.Unset;
        public IReadOnlyList<IPriorityValueEntry<T>> Entries => _entries;
        Optional<object> IValue.GetValue() => _value.ToObject();

        public void ClearLocalValue()
        {
            UpdateEffectiveValue(new AvaloniaPropertyChangedEventArgs<T>(
                _owner,
                Property,
                default,
                default,
                BindingPriority.LocalValue));
        }

        public Optional<T> GetValue(BindingPriority maxPriority = BindingPriority.Animation)
        {
            if (Priority == BindingPriority.Unset)
            {
                return default;
            }

            if (Priority >= maxPriority)
            {
                return _value;
            }

            return CalculateValue(maxPriority).Item1;
        }

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

            UpdateEffectiveValue(new AvaloniaPropertyChangedEventArgs<T>(
                _owner,
                Property,
                default,
                value,
                priority));

            return result;
        }

        public BindingEntry<T> AddBinding(IObservable<BindingValue<T>> source, BindingPriority priority)
        {
            var binding = new BindingEntry<T>(_owner, Property, source, priority, this);
            var insert = FindInsertPoint(binding.Priority);
            _entries.Insert(insert, binding);
            return binding;
        }

        public void CoerceValue() => UpdateEffectiveValue(null);

        void IValueSink.ValueChanged<TValue>(AvaloniaPropertyChangedEventArgs<TValue> change)
        {
            if (change.Priority == BindingPriority.LocalValue)
            {
                _localValue = default;
            }

            if (change is AvaloniaPropertyChangedEventArgs<T> c)
            {
                UpdateEffectiveValue(c);
            }
        }

        void IValueSink.Completed<TValue>(
            StyledPropertyBase<TValue> property,
            IPriorityValueEntry entry,
            Optional<TValue> oldValue)
        {
            _entries.Remove((IPriorityValueEntry<T>)entry);

            if (oldValue is Optional<T> o)
            {
                UpdateEffectiveValue(new AvaloniaPropertyChangedEventArgs<T>(
                    _owner,
                    Property,
                    o,
                    default,
                    entry.Priority));
            }
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

        public (Optional<T>, BindingPriority) CalculateValue(BindingPriority maxPriority)
        {
            var reachedLocalValues = false;

            for (var i = _entries.Count - 1; i >= 0; --i)
            {
                var entry = _entries[i];

                if (entry.Priority < maxPriority)
                {
                    continue;
                }

                if (!reachedLocalValues &&
                    entry.Priority >= BindingPriority.LocalValue &&
                    maxPriority <= BindingPriority.LocalValue &&
                    _localValue.HasValue)
                {
                    return (_localValue, BindingPriority.LocalValue);
                }

                var entryValue = entry.GetValue();

                if (entryValue.HasValue)
                {
                    return (entryValue, entry.Priority);
                }
            }

            if (!reachedLocalValues &&
                maxPriority <= BindingPriority.LocalValue &&
                _localValue.HasValue)
            {
                return (_localValue, BindingPriority.LocalValue);
            }

            return (default, BindingPriority.Unset);
        }

        private void UpdateEffectiveValue(AvaloniaPropertyChangedEventArgs<T>? change)
        {
            var (value, priority) = CalculateValue(BindingPriority.Animation);

            if (value.HasValue && _coerceValue != null)
            {
                value = _coerceValue(_owner, value.Value);
            }

            Priority = priority;

            if (value != _value)
            {
                var old = _value;
                _value = value;

                _sink.ValueChanged(new AvaloniaPropertyChangedEventArgs<T>(
                    _owner,
                    Property,
                    old,
                    value,
                    Priority));
            }
            else if (change is object)
            {
                change.MarkInactiveValue();
                change.SetOldValue(default);
                _sink.ValueChanged(change);
            }
        }
    }
}
