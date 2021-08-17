using System;
using System.Collections.Generic;

namespace Avalonia.Reactive
{
    internal class AvaloniaPropertyObservable<T> : LightweightObservableBase<T>, IDescription
    {
        private readonly WeakReference<IAvaloniaObject> _target;
        private readonly AvaloniaProperty _property;
        private T _value;

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
                _value = (T)target.GetValue(_property);
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

        protected override void Subscribed(IObserver<T> observer, bool first)
        {
            observer.OnNext(_value);
        }

        private void PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == _property)
            {
                T newValue;

                if (e is AvaloniaPropertyChangedEventArgs<T> typed)
                {
                    newValue = typed.Sender.GetValue(typed.Property);
                }
                else
                {
                    newValue = (T)e.Sender.GetValue(e.Property);
                }

                if (!EqualityComparer<T>.Default.Equals(newValue, _value))
                {
                    _value = newValue;
                    PublishNext(_value);
                }
            }
        }
    }
}
