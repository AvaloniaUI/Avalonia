using System;
using System.Collections.Generic;
using Avalonia.Data;

namespace Avalonia.Reactive
{
    internal class AvaloniaPropertyBindingObservable<TSource,TResult> : LightweightObservableBase<BindingValue<TResult>>, IDescription
    {
        private readonly WeakReference<AvaloniaObject> _target;
        private readonly AvaloniaProperty _property;
        private readonly Func<TSource, TResult>? _converter;
        private BindingValue<TResult> _value = BindingValue<TResult>.Unset;

        public AvaloniaPropertyBindingObservable(
            AvaloniaObject target,
            AvaloniaProperty property,
            Func<TSource, TResult>? converter = null)
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
        }

        protected override void Subscribed(IObserver<BindingValue<TResult>> observer, bool first)
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
                if (e is AvaloniaPropertyChangedEventArgs<TResult> typedArgs)
                {
                    PublishValue(e.Sender.GetValue<TResult>(typedArgs.Property));
                }
                else
                {
                    PublishUntypedValue(e.Sender.GetValue(e.Property));
                }
            }
        }

        private void PropertyChanged_WithConversion(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == _property)
            {
                if (e is AvaloniaPropertyChangedEventArgs<TSource> typedArgs)
                {
                    var newValueRaw = e.Sender.GetValue<TSource>(typedArgs.Property);

                    var newValue = _converter!(newValueRaw);

                    PublishValue(newValue);
                }
                else
                {
                    var newValue = e.Sender.GetValue(e.Property);

                    if (newValue is TSource source)
                    {
                        newValue = _converter!(source);
                    }

                    PublishUntypedValue(newValue);
                }
            }
        }

        private void PublishValue(TResult newValue)
        {
            if (!_value.HasValue || !EqualityComparer<TResult>.Default.Equals(newValue, _value.Value))
            {
                _value = newValue;
                PublishNext(_value);
            }
        }

        private void PublishUntypedValue(object? newValue)
        {
            if (!Equals(newValue, _value))
            {
                _value = (TResult)newValue!;
                PublishNext(_value);
            }
        }
    }
}
