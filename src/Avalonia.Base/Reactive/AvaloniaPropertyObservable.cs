using System;

namespace Avalonia.Reactive
{
    public class AvaloniaPropertyObservable<T> : LightweightObservableBase<T>, IDescription
    {
        private readonly IAvaloniaObject _target;
        private readonly AvaloniaProperty _property;
        private T _value;

        public AvaloniaPropertyObservable(
            IAvaloniaObject target,
            AvaloniaProperty property)
        {
            _target = target;
            _property = property;
        }

        public string Description => $"{_target.GetType().Name}.{_property.Name}";

        protected override void Initialize()
        {
            _value = (T)_target.GetValue(_property);
            _target.PropertyChanged += PropertyChanged;
        }

        protected override void Deinitialize()
        {
            _target.PropertyChanged -= PropertyChanged;
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
