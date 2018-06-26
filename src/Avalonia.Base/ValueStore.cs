using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Data;
using Avalonia.Utilities;

namespace Avalonia
{
    internal class ValueStore : IPriorityValueOwner
    {
        private readonly AvaloniaObject _owner;
        private readonly Dictionary<AvaloniaProperty, PriorityValue> _values =
            new Dictionary<AvaloniaProperty, PriorityValue>();

        public ValueStore(AvaloniaObject owner)
        {
            _owner = owner;
        }

        public IDisposable AddBinding(
            AvaloniaProperty property,
            IObservable<object> source,
            BindingPriority priority)
        {
            if (!_values.TryGetValue(property, out PriorityValue v))
            {
                v = CreatePriorityValue(property);
                _values.Add(property, v);
            }

            return v.Add(source, (int)priority);
        }

        public void AddValue(AvaloniaProperty property, object value, int priority)
        {
            var originalValue = value;

            if (!TypeUtilities.TryConvertImplicit(property.PropertyType, value, out value))
            {
                throw new ArgumentException(string.Format(
                    "Invalid value for Property '{0}': '{1}' ({2})",
                    property.Name,
                    originalValue,
                    originalValue?.GetType().FullName ?? "(null)"));
            }

            if (!_values.TryGetValue(property, out PriorityValue v))
            {
                if (value == AvaloniaProperty.UnsetValue)
                {
                    return;
                }

                v = CreatePriorityValue(property);
                _values.Add(property, v);
            }

            v.SetValue(value, priority);
        }

        public IDictionary<AvaloniaProperty, PriorityValue> GetSetValues() => _values;

        public object GetValue(AvaloniaProperty property)
        {
            var result = AvaloniaProperty.UnsetValue;

            if (_values.TryGetValue(property, out PriorityValue value))
            {
                result = value.Value;
            }

            if (result == AvaloniaProperty.UnsetValue)
            {
                result = _owner.GetDefaultValue(property);
            }

            return result;
        }

        public bool IsAnimating(AvaloniaProperty property)
        {
            return _values.TryGetValue(property, out PriorityValue value) ? value.IsAnimating : false;
        }

        public bool IsSet(AvaloniaProperty property)
        {
            if (_values.TryGetValue(property, out PriorityValue value))
            {
                return value.Value != AvaloniaProperty.UnsetValue;
            }

            return false;
        }

        public void Revalidate(AvaloniaProperty property)
        {
            if (_values.TryGetValue(property, out PriorityValue value))
            {
                value.Revalidate();
            }
        }

        void IPriorityValueOwner.BindingNotificationReceived(AvaloniaProperty property, BindingNotification notification)
        {
            ((IPriorityValueOwner)_owner).BindingNotificationReceived(property, notification);
        }

        void IPriorityValueOwner.Changed(AvaloniaProperty property, int priority, object oldValue, object newValue)
        {
            ((IPriorityValueOwner)_owner).Changed(property, priority, oldValue, newValue);
        }

        void IPriorityValueOwner.VerifyAccess() => _owner.VerifyAccess();

        private PriorityValue CreatePriorityValue(AvaloniaProperty property)
        {
            var validate = ((IStyledPropertyAccessor)property).GetValidationFunc(_owner.GetType());
            Func<object, object> validate2 = null;

            if (validate != null)
            {
                validate2 = v => validate(_owner, v);
            }

            PriorityValue result = new PriorityValue(
                this,
                property,
                property.PropertyType,
                validate2);

            return result;
        }
    }
}
