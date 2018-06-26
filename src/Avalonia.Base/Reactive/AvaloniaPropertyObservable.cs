using System;

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
                _value = (T)e.NewValue;
                PublishNext(_value);
            }
        }
    }
}
