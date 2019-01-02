using System;
using System.Collections.Generic;
using Avalonia.Data;
using Avalonia.Utilities;

namespace Avalonia
{
    internal class ValueStore : IPriorityValueOwner
    {
        struct Entry
        {
            internal int PropertyId;
            internal object Value;
        }

        private readonly AvaloniaObject _owner;
        private Entry[] _entries;

        public ValueStore(AvaloniaObject owner)
        {
            _owner = owner;

            // The last item in the list is always int.MaxValue
            _entries = new[] { new Entry { PropertyId = int.MaxValue, Value = null } };
        }

        public IDisposable AddBinding(
            AvaloniaProperty property,
            IObservable<object> source,
            BindingPriority priority)
        {
            PriorityValue priorityValue;

            if (TryGetValue(property, out var v))
            {
                priorityValue = v as PriorityValue;

                if (priorityValue == null)
                {
                    priorityValue = CreatePriorityValue(property);
                    priorityValue.SetValue(v, (int)BindingPriority.LocalValue);
                    SetValueInternal(property, priorityValue);
                }
            }
            else
            {
                priorityValue = CreatePriorityValue(property);
                AddValueInternal(property, priorityValue);
            }

            return priorityValue.Add(source, (int)priority);
        }

        public void AddValue(AvaloniaProperty property, object value, int priority)
        {
            PriorityValue priorityValue;

            if (TryGetValue(property, out var v))
            {
                priorityValue = v as PriorityValue;

                if (priorityValue == null)
                {
                    if (priority == (int)BindingPriority.LocalValue)
                    {
                        SetValueInternal(property, Validate(property, value));
                        Changed(property, priority, v, value);
                        return;
                    }
                    else
                    {
                        priorityValue = CreatePriorityValue(property);
                        priorityValue.SetValue(v, (int)BindingPriority.LocalValue);
                        SetValueInternal(property, priorityValue);
                    }
                }
            }
            else
            {
                if (value == AvaloniaProperty.UnsetValue)
                {
                    return;
                }

                if (priority == (int)BindingPriority.LocalValue)
                {
                    AddValueInternal(property, Validate(property, value));
                    Changed(property, priority, AvaloniaProperty.UnsetValue, value);
                    return;
                }
                else
                {
                    priorityValue = CreatePriorityValue(property);
                    AddValueInternal(property, priorityValue);
                }
            }

            priorityValue.SetValue(value, priority);
        }

        public void BindingNotificationReceived(AvaloniaProperty property, BindingNotification notification)
        {
            _owner.BindingNotificationReceived(property, notification);
        }

        public void Changed(AvaloniaProperty property, int priority, object oldValue, object newValue)
        {
            _owner.PriorityValueChanged(property, priority, oldValue, newValue);
        }

        public IDictionary<AvaloniaProperty, object> GetSetValues()
        {
            var dict = new Dictionary<AvaloniaProperty, object>(_entries.Length - 1);
            for (int i = 0; i < _entries.Length - 1; ++i)
            {
                dict.Add(AvaloniaPropertyRegistry.Instance.FindRegistered(_entries[i].PropertyId), _entries[i].Value);
            }

            return dict;
        }

        public object GetValue(AvaloniaProperty property)
        {
            var result = AvaloniaProperty.UnsetValue;

            if (TryGetValue(property, out var value))
            {
                result = (value is PriorityValue priorityValue) ? priorityValue.Value : value;
            }

            return result;
        }

        public bool IsAnimating(AvaloniaProperty property)
        {
            return TryGetValue(property, out var value) && value is PriorityValue priority && priority.IsAnimating;
        }

        public bool IsSet(AvaloniaProperty property)
        {
            if (TryGetValue(property, out var value))
            {
                return ((value as PriorityValue)?.Value ?? value) != AvaloniaProperty.UnsetValue;
            }

            return false;
        }

        public void Revalidate(AvaloniaProperty property)
        {
            if (TryGetValue(property, out var value))
            {
                (value as PriorityValue)?.Revalidate();
            }
        }

        public void VerifyAccess() => _owner.VerifyAccess();

        private PriorityValue CreatePriorityValue(AvaloniaProperty property)
        {
            var validate = ((IStyledPropertyAccessor)property).GetValidationFunc(_owner.GetType());
            Func<object, object> validate2 = null;

            if (validate != null)
            {
                validate2 = v => validate(_owner, v);
            }

            return new PriorityValue(
                this,
                property,
                property.PropertyType,
                validate2);
        }

        private object Validate(AvaloniaProperty property, object value)
        {
            var validate = ((IStyledPropertyAccessor)property).GetValidationFunc(_owner.GetType());

            if (validate != null && value != AvaloniaProperty.UnsetValue)
            {
                return validate(_owner, value);
            }

            return value;
        }

        private DeferredSetter<object> _deferredSetter;

        public DeferredSetter<object> Setter
        {
            get
            {
                return _deferredSetter ??
                    (_deferredSetter = new DeferredSetter<object>());
            }
        }

        private bool TryGetValue(AvaloniaProperty property, out object value)
        {
            (int index, bool found) = TryFindEntry(property.Id);
            if (!found)
            {
                value = null;
                return false;
            }

            value = _entries[index].Value;
            return true;
        }

        private void AddValueInternal(AvaloniaProperty property, object value)
        {
            Entry[] entries = new Entry[_entries.Length + 1];

            for (int i = 0; i < _entries.Length; ++i)
            {
                if (_entries[i].PropertyId > property.Id)
                {
                    if (i > 0)
                    {
                        Array.Copy(_entries, 0, entries, 0, i);
                    }

                    entries[i] = new Entry { PropertyId = property.Id, Value = value };
                    Array.Copy(_entries, i, entries, i + 1, _entries.Length - i);
                    break;
                }
            }

            _entries = entries;
        }

        private void SetValueInternal(AvaloniaProperty property, object value)
        {
            _entries[TryFindEntry(property.Id).Item1].Value = value;
        }

        private (int, bool) TryFindEntry(int propertyId)
        {
            if (_entries.Length <= 20)
            {
                // For small lists, we use an optimized linear search. Since the last item in the list
                // is always int.MaxValue, we can skip a conditional branch in each iteration.
                // By unrolling the loop, we can skip another unconditional branch in each iteration.

                if (_entries[0].PropertyId >= propertyId) return (0, _entries[0].PropertyId == propertyId);
                if (_entries[1].PropertyId >= propertyId) return (1, _entries[1].PropertyId == propertyId);
                if (_entries[2].PropertyId >= propertyId) return (2, _entries[2].PropertyId == propertyId);
                if (_entries[3].PropertyId >= propertyId) return (3, _entries[3].PropertyId == propertyId);
                if (_entries[4].PropertyId >= propertyId) return (4, _entries[4].PropertyId == propertyId);
                if (_entries[5].PropertyId >= propertyId) return (5, _entries[5].PropertyId == propertyId);
                if (_entries[6].PropertyId >= propertyId) return (6, _entries[6].PropertyId == propertyId);
                if (_entries[7].PropertyId >= propertyId) return (7, _entries[7].PropertyId == propertyId);
                if (_entries[8].PropertyId >= propertyId) return (8, _entries[8].PropertyId == propertyId);
                if (_entries[9].PropertyId >= propertyId) return (9, _entries[9].PropertyId == propertyId);
                if (_entries[10].PropertyId >= propertyId) return (10, _entries[10].PropertyId == propertyId);
                if (_entries[11].PropertyId >= propertyId) return (11, _entries[11].PropertyId == propertyId);
                if (_entries[12].PropertyId >= propertyId) return (12, _entries[12].PropertyId == propertyId);
                if (_entries[13].PropertyId >= propertyId) return (13, _entries[13].PropertyId == propertyId);
                if (_entries[14].PropertyId >= propertyId) return (14, _entries[14].PropertyId == propertyId);
                if (_entries[15].PropertyId >= propertyId) return (15, _entries[15].PropertyId == propertyId);
                if (_entries[16].PropertyId >= propertyId) return (16, _entries[16].PropertyId == propertyId);
                if (_entries[17].PropertyId >= propertyId) return (17, _entries[17].PropertyId == propertyId);
                if (_entries[18].PropertyId >= propertyId) return (18, _entries[18].PropertyId == propertyId);
            }
            else
            {
                int low = 0;
                int high = _entries.Length;
                int id;

                if (high > 0)
                {
                    while (high - low > 3)
                    {
                        int pivot = (high + low) / 2;
                        id = _entries[pivot].PropertyId;

                        if (propertyId == id)
                            return (pivot, true);

                        if (propertyId <= id)
                            high = pivot;
                        else
                            low = pivot + 1;
                    }

                    do
                    {
                        id = _entries[low].PropertyId;

                        if (id == propertyId)
                            return (low, true);

                        if (id > propertyId)
                            break;

                        ++low;
                    }
                    while (low < high);
                }
            }

            return (0, false);
        }
    }
}
