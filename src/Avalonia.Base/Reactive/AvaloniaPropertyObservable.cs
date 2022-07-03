using System;
using System.Collections.Generic;
using Avalonia.Data;

namespace Avalonia.Reactive
{
    internal class AvaloniaPropertyObservable<T> : LightweightObservableBase<T>, IDescription
    {
        private readonly WeakReference<IAvaloniaObject> _target;
        private readonly AvaloniaProperty _property;
        private Optional<T> _value;

        public AvaloniaPropertyObservable(
            IAvaloniaObject target,
            AvaloniaProperty property)
        {
            _target = new WeakReference<IAvaloniaObject>(target);
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

            _value = default;
        }

        protected override void Subscribed(IObserver<T> observer, bool first)
        {
            if (_value.HasValue)
                observer.OnNext(_value.Value);
        }

        private void PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == _property)
            {
                if (e.Sender is AvaloniaObject ao)
                {
                    T newValue;

                    if (e is AvaloniaPropertyChangedEventArgs<T> typed)
                    {
                        newValue = AvaloniaObjectExtensions.GetValue(ao, typed.Property);
                    }
                    else
                    {
                        newValue = (T)e.Sender.GetValue(e.Property)!;
                    }

                    if (!_value.HasValue ||
                        !EqualityComparer<T>.Default.Equals(newValue, _value.Value))
                    {
                        _value = newValue;
                        PublishNext(_value.Value!);
                    }
                }
                else
                {
                    throw new NotSupportedException("Custom implementations of IAvaloniaObject not supported.");
                }

            }
        }
    }
}
