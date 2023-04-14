using System;
using System.Collections.Generic;
using Avalonia.Data;

namespace Avalonia.Reactive
{
    internal class AvaloniaPropertyObservable<TSource,TResult> : LightweightObservableBase<TResult>, IDescription
    {
        private readonly WeakReference<AvaloniaObject> _target;
        private readonly AvaloniaProperty _property;
        private readonly Func<TSource, TResult>? _converter;
        private Optional<TResult> _value;

        public AvaloniaPropertyObservable(
            AvaloniaObject target,
            AvaloniaProperty property,
            Func<TSource,TResult>? converter = null)
        {
            _target = new WeakReference<AvaloniaObject>(target);
            _property = property;
            _converter = converter;
        }

        public string Description => $"{_target.GetType().Name}.{_property.Name}";

        protected override void Initialize()
        {
            if (_target.TryGetTarget(out var target))
            {
                if (_converter is { } converter)
                {
                    var unconvertedValue = (TSource)target.GetValue(_property)!;
                    _value = converter(unconvertedValue);
                    target.PropertyChanged += PropertyChanged_WithConversion;
                }
                else
                {
                    _value = (TResult)target.GetValue(_property)!;
                    target.PropertyChanged += PropertyChanged;
                }
            }
        }

        protected override void Deinitialize()
        {
            if (_target.TryGetTarget(out var target))
            {
                if (_converter is not null)
                {
                    target.PropertyChanged -= PropertyChanged_WithConversion;
                }
                else
                {
                    target.PropertyChanged -= PropertyChanged;
                }
            }

            _value = default;
        }

        protected override void Subscribed(IObserver<TResult> observer, bool first)
        {
            if (_value.HasValue)
                observer.OnNext(_value.Value);
        }

        private void PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == _property)
            {
                TResult newValue;

                if (e is AvaloniaPropertyChangedEventArgs<TResult> typed)
                {
                    newValue = AvaloniaObjectExtensions.GetValue(e.Sender, typed.Property);
                }
                else
                {
                    newValue = (TResult)e.Sender.GetValue(e.Property)!;
                }

                PublishNewValue(newValue);
            }
        }

        private void PropertyChanged_WithConversion(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == _property)
            {
                TSource newValueRaw;

                if (e is AvaloniaPropertyChangedEventArgs<TSource> typed)
                {
                    newValueRaw = AvaloniaObjectExtensions.GetValue(e.Sender, typed.Property);
                }
                else
                {
                    newValueRaw = (TSource)e.Sender.GetValue(e.Property)!;
                }

                var newValue = _converter!(newValueRaw);

                PublishNewValue(newValue);
            }
        }

        private void PublishNewValue(TResult newValue)
        {
            if (!_value.HasValue ||
                !EqualityComparer<TResult>.Default.Equals(newValue, _value.Value))
            {
                _value = newValue;
                PublishNext(_value.Value!);
            }
        }
    }
}
