using System;
using System.Collections.Generic;
using Avalonia.Data;

#nullable enable

namespace Avalonia.Reactive
{
    internal class AvaloniaPropertyBindingObservable<T> : LightweightObservableBase<BindingValue<T>>, IDescription
    {
        private readonly WeakReference<IAvaloniaObject> _target;
        private readonly AvaloniaProperty _property;
        private T _value;

#nullable disable
        public AvaloniaPropertyBindingObservable(
            IAvaloniaObject target,
            AvaloniaProperty property)
        {
            _target = new WeakReference<IAvaloniaObject>(target);
            _property = property;
        }
#nullable enable

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

        protected override void Subscribed(IObserver<BindingValue<T>> observer, bool first)
        {
            observer.OnNext(new BindingValue<T>(_value));
        }

        private void PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == _property)
            {
                if (e is AvaloniaPropertyChangedEventArgs<T> typedArgs)
                {
                    var newValue = e.Sender.GetValue(typedArgs.Property);

                    if (!typedArgs.OldValue.HasValue || !EqualityComparer<T>.Default.Equals(newValue, _value))
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
                        _value = (T)newValue;
                        PublishNext(_value);
                    }
                }
            }
        }
    }
}
