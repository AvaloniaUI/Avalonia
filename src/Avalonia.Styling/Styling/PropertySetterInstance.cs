using System;
using Avalonia.Data;

#nullable enable

namespace Avalonia.Styling
{
    internal class PropertySetterInstance<T> : ISetterInstance
    {
        private readonly IStyleable _target;
        private readonly StyledPropertyBase<T>? _styledProperty;
        private readonly DirectPropertyBase<T>? _directProperty;
        private readonly BindingPriority _priority;
        private readonly T _value;
        private IDisposable? _subscription;
        private bool _isActive;

        public PropertySetterInstance(
            IStyleable target,
            StyledPropertyBase<T> property,
            BindingPriority priority,
            T value)
        {
            _target = target;
            _styledProperty = property;
            _priority = priority;
            _value = value;
        }

        public PropertySetterInstance(
            IStyleable target,
            DirectPropertyBase<T> property,
            BindingPriority priority,
            T value)
        {
            _target = target;
            _directProperty = property;
            _priority = priority;
            _value = value;
        }

        public void Activate()
        {
            if (!_isActive)
            {
                if (_styledProperty is object)
                {
                    _subscription = _target.SetValue(_styledProperty, _value, _priority);
                }
                else
                {
                    _target.SetValue(_directProperty!, _value);
                }

                _isActive = true;
            }
        }

        public void Deactivate()
        {
            if (_isActive)
            {
                if (_subscription is null)
                {
                    if (_styledProperty is object)
                    {
                        _target.ClearValue(_styledProperty);
                    }
                    else
                    {
                        _target.ClearValue(_directProperty!);
                    }
                }
                else
                {
                    _subscription.Dispose();
                    _subscription = null;
                }
            }
        }
    }
}
