using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        private BatchUpdate? _batchUpdate;

        public ValueStore(AvaloniaObject owner)
        {
            _sink = _owner = owner;
            _values = new AvaloniaPropertyValueStore<IValue>();
        }

        public void BeginBatchUpdate()
        {
            _batchUpdate ??= new BatchUpdate(this);
            _batchUpdate.Begin();
        }

        public void EndBatchUpdate()
        {
            if (_batchUpdate is null)
            {
                throw new InvalidOperationException("No batch update in progress.");
            }

            if (_batchUpdate.End())
            {
                _batchUpdate = null;
            }
        }

        public bool IsAnimating(AvaloniaProperty property)
        {
            if (TryGetValue(property, out var slot))
            {
                return slot.Priority < BindingPriority.LocalValue;
            }

            return false;
        }

        public bool IsSet(AvaloniaProperty property)
        {
            if (TryGetValue(property, out var slot))
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
            if (TryGetValue(property, out var slot))
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

            if (TryGetValue(property, out var slot))
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
            if (TryGetValue(property, out var slot))
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
            if (TryGetValue(property, out var slot))
            {
                if (slot is PriorityValue<T> p)
                {
                    p.ClearLocalValue();
                }
                else if (slot.Priority == BindingPriority.LocalValue)
                {
                    var old = TryGetValue(property, BindingPriority.LocalValue, out var value) ? value : default;

                    // During batch update values can't be removed immediately because they're needed to raise
                    // a correctly-typed _sink.ValueChanged notification. They instead mark themselves for removal
                    // by setting their priority to Unset.
                    if (!IsBatchUpdating())
                    {
                        _values.Remove(property);
                    }
                    else if (slot is IDisposable d)
                    {
                        d.Dispose();
                    }
                    else
                    {
                        // Local value entries are optimized and contain only a single value field to save space,
                        // so there's no way to mark them for removal at the end of a batch update. Instead convert
                        // them to a constant value entry with Unset priority in the event of a local value being
                        // cleared during a batch update.
                        var sentinel = new ConstantValueEntry<T>(property, default, BindingPriority.Unset, _sink);
                        _values.SetValue(property, sentinel);
                    }

                    NotifyValueChanged<T>(property, old, default, BindingPriority.Unset);
                }
            }
        }

        public void CoerceValue<T>(StyledPropertyBase<T> property)
        {
            if (TryGetValue(property, out var slot))
            {
                if (slot is PriorityValue<T> p)
                {
                    p.UpdateEffectiveValue();
                }
            }
        }

        public Diagnostics.AvaloniaPropertyValue? GetDiagnostic(AvaloniaProperty property)
        {
            if (TryGetValue(property, out var slot))
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
                if (change.IsEffectiveValueChange)
                {
                    NotifyValueChanged<T>(change.Property, change.OldValue, change.NewValue, change.Priority);
                }
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
            // We need to include remove sentinels here so call `_values.TryGetValue` directly.
            if (_values.TryGetValue(property, out var slot) && slot == entry)
            {
                if (_batchUpdate is null)
                {
                    _values.Remove(property);
                    _sink.Completed(property, entry, oldValue);
                }
                else
                {
                    _batchUpdate.ValueChanged(property, oldValue.ToObject());
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
                    if (IsBatchUpdating())
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

                if (IsBatchUpdating())
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
            if (IsBatchUpdating() && value is IBatchUpdate batch)
                batch.BeginBatchUpdate();
            value.Start();
        }

        private void NotifyValueChanged<T>(
            AvaloniaProperty<T> property,
            Optional<T> oldValue,
            BindingValue<T> newValue,
            BindingPriority priority)
        {
            if (_batchUpdate is null)
            {
                _sink.ValueChanged(new AvaloniaPropertyChangedEventArgs<T>(
                    _owner,
                    property,
                    oldValue,
                    newValue,
                    priority));
            }
            else
            {
                _batchUpdate.ValueChanged(property, oldValue.ToObject());
            }
        }

        private bool IsBatchUpdating() => _batchUpdate?.IsBatchUpdating == true;

        private bool TryGetValue(AvaloniaProperty property, [MaybeNullWhen(false)] out IValue value)
        {
            return _values.TryGetValue(property, out value) && !IsRemoveSentinel(value);
        }

        private static bool IsRemoveSentinel(IValue value)
        {
            // Local value entries are optimized and contain only a single value field to save space,
            // so there's no way to mark them for removal at the end of a batch update. Instead a
            // ConstantValueEntry with a priority of Unset is used as a sentinel value.
            return value is IConstantValueEntry t && t.Priority == BindingPriority.Unset;
        }

        private class BatchUpdate
        {
            private ValueStore _owner;
            private List<Notification>? _notifications;
            private int _batchUpdateCount;
            private int _iterator = -1;

            public BatchUpdate(ValueStore owner) => _owner = owner;

            public bool IsBatchUpdating => _batchUpdateCount > 0;

            public void Begin()
            {
                if (_batchUpdateCount++ == 0)
                {
                    var values = _owner._values;

                    for (var i = 0; i < values.Count; ++i)
                    {
                        (values[i] as IBatchUpdate)?.BeginBatchUpdate();
                    }
                }
            }

            public bool End()
            {
                if (--_batchUpdateCount > 0)
                    return false;

                var values = _owner._values;

                // First call EndBatchUpdate on all bindings. This should cause the active binding to be subscribed
                // but notifications will still not be raised because the owner ValueStore will still have a reference
                // to this batch update object.
                for (var i = 0; i < values.Count; ++i)
                {
                    (values[i] as IBatchUpdate)?.EndBatchUpdate();

                    // Somehow subscribing to a binding caused a new batch update. This shouldn't happen but in case it
                    // does, abort and continue batch updating.
                    if (_batchUpdateCount > 0)
                        return false;
                }

                if (_notifications is object)
                {
                    // Raise all batched notifications. Doing this can cause other notifications to be added and even
                    // cause a new batch update to start, so we need to handle _notifications being modified by storing
                    // the index in field.
                    _iterator = 0;

                    for (; _iterator < _notifications.Count; ++_iterator)
                    {
                        var entry = _notifications[_iterator];

                        if (values.TryGetValue(entry.property, out var slot))
                        {
                            var oldValue = entry.oldValue;
                            var newValue = slot.GetValue();

                            // Raising this notification can cause a new batch update to be started, which in turn
                            // results in another change to the property. In this case we need to update the old value
                            // so that the *next* notification has an oldValue which follows on from the newValue
                            // raised here.
                            _notifications[_iterator] = new Notification
                            {
                                property = entry.property,
                                oldValue = newValue,
                            };

                            // Call _sink.ValueChanged with an appropriately typed AvaloniaPropertyChangedEventArgs<T>.
                            slot.RaiseValueChanged(_owner._sink, _owner._owner, entry.property, oldValue, newValue);

                            // During batch update values can't be removed immediately because they're needed to raise
                            // the _sink.ValueChanged notification. They instead mark themselves for removal by setting
                            // their priority to Unset. We need to re-read the slot here because raising ValueChanged
                            // could have caused it to be updated.
                            if (values.TryGetValue(entry.property, out var updatedSlot) &&
                                updatedSlot.Priority == BindingPriority.Unset)
                            {
                                values.Remove(entry.property);
                            }
                        }
                        else
                        {
                            throw new AvaloniaInternalException("Value could not be found at the end of batch update.");
                        }

                        // If a new batch update was started while ending this one, abort.
                        if (_batchUpdateCount > 0)
                            return false;
                    }
                }

                _iterator = int.MaxValue - 1;
                return true;
            }

            public void ValueChanged(AvaloniaProperty property, Optional<object> oldValue)
            {
                _notifications ??= new List<Notification>();

                for (var i = 0; i < _notifications.Count; ++i)
                {
                    if (_notifications[i].property == property)
                    {
                        oldValue = _notifications[i].oldValue;
                        _notifications.RemoveAt(i);

                        if (i <= _iterator)
                            --_iterator;
                        break;
                    }
                }

                _notifications.Add(new Notification
                {
                    property = property,
                    oldValue = oldValue,
                });
            }

            private struct Notification
            {
                public AvaloniaProperty property;
                public Optional<object> oldValue;
            }
        }
    }
}
