using System;
using System.Collections.Generic;
using Avalonia.Data;

namespace Avalonia.Reactive
{
    internal class AvaloniaPropertyBindingObservable<T> : LightweightObservableBase<BindingValue<T>>, IDescription
    {
        private readonly WeakReference<AvaloniaObject> _target;
        private readonly AvaloniaProperty _property;
        private BindingValue<T> _value = BindingValue<T>.Unset;

        public AvaloniaPropertyBindingObservable(
            AvaloniaObject target,
            AvaloniaProperty property)
        {
            _target = new WeakReference<AvaloniaObject>(target);
            _property = property;
        }

        public string Description => $"{_target.GetType().Name}.{_property.Name}";

        protected override void Initialize()
        {
            if (_target.TryGetTarget(out var target))
            {
                _value = (T)target.GetValue(_property)!;
                target.PropertyChanged += PropertyChanged;
            }
        }

        protected override void Deinitialize()
        {
            if (_target.TryGetTarget(out var target))
            {
                target.PropertyChanged -= PropertyChanged;
            }
        }

        protected override void Subscribed(IObserver<BindingValue<T>> observer, bool first)
        {
            if (_value.Type != BindingValueType.UnsetValue)
            {
                observer.OnNext(_value);
            }
        }

        private void PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == _property)
            {
                if (e is AvaloniaPropertyChangedEventArgs<T> typedArgs)
                {
                    var newValue = e.Sender.GetValue<T>(typedArgs.Property);

                    if (!_value.HasValue || !EqualityComparer<T>.Default.Equals(newValue, _value.Value))
                    {
                        _value = newValue;
                        PublishNext(_value);
                    }
                }
                else
                {
                    var newValue = e.Sender.GetValue(e.Property);

                    if (!Equals(newValue, _value))
                    {
                        _value = (T)newValue!;
                        PublishNext(_value);
                    }
                }
            }
        }
    }
}
