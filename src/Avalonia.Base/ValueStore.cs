﻿using System;
using System.Collections.Generic;
using Avalonia.Data;
using Avalonia.Utilities;

namespace Avalonia
{
    internal class ValueStore : IPriorityValueOwner
    {
        private readonly AvaloniaObject _owner;
        private readonly Dictionary<AvaloniaProperty, object> _values =
            new Dictionary<AvaloniaProperty, object>();

        public ValueStore(AvaloniaObject owner)
        {
            _owner = owner;
        }

        public IDisposable AddBinding(
            AvaloniaProperty property,
            IObservable<object> source,
            BindingPriority priority)
        {
            PriorityValue priorityValue;

            if (_values.TryGetValue(property, out var v))
            {
                priorityValue = v as PriorityValue;

                if (priorityValue == null)
                {
                    priorityValue = CreatePriorityValue(property);
                    priorityValue.SetValue(v, (int)BindingPriority.LocalValue);
                    _values[property] = priorityValue;
                }
            }
            else
            {
                priorityValue = CreatePriorityValue(property);
                _values.Add(property, priorityValue);
            }

            return priorityValue.Add(source, (int)priority);
        }

        public void AddValue(AvaloniaProperty property, object value, int priority)
        {
            PriorityValue priorityValue;

            if (_values.TryGetValue(property, out var v))
            {
                priorityValue = v as PriorityValue;

                if (priorityValue == null)
                {
                    if (priority == (int)BindingPriority.LocalValue)
                    {
                        _values[property] = Validate(property, value);
                        Changed(property, priority, v, value);
                        return;
                    }
                    else
                    {
                        priorityValue = CreatePriorityValue(property);
                        priorityValue.SetValue(v, (int)BindingPriority.LocalValue);
                        _values[property] = priorityValue;
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
                    _values.Add(property, Validate(property, value));
                    Changed(property, priority, AvaloniaProperty.UnsetValue, value);
                    return;
                }
                else
                {
                    priorityValue = CreatePriorityValue(property);
                    _values.Add(property, priorityValue);
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

        public IDictionary<AvaloniaProperty, object> GetSetValues() => _values;

        public object GetValue(AvaloniaProperty property)
        {
            var result = AvaloniaProperty.UnsetValue;

            if (_values.TryGetValue(property, out var value))
            {
                result = (value is PriorityValue priorityValue) ? priorityValue.Value : value;
            }

            return result;
        }

        public bool IsAnimating(AvaloniaProperty property)
        {
            return _values.TryGetValue(property, out var value) && value is PriorityValue priority && priority.IsAnimating;
        }

        public bool IsSet(AvaloniaProperty property)
        {
            if (_values.TryGetValue(property, out var value))
            {
                return ((value as PriorityValue)?.Value ?? value) != AvaloniaProperty.UnsetValue;
            }

            return false;
        }

        public void Revalidate(AvaloniaProperty property)
        {
            if (_values.TryGetValue(property, out var value))
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

        private DeferredSetter<object> _defferedSetter;

        public DeferredSetter<object> Setter
        {
            get
            {
                return _defferedSetter ??
                    (_defferedSetter = new DeferredSetter<object>());
            }
        }
    }
}
