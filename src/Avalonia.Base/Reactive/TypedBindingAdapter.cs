using System;
using Avalonia.Data;
using Avalonia.Logging;

namespace Avalonia.Reactive
{
    internal class TypedBindingAdapter<T> : SingleSubscriberObservableBase<BindingValue<T>>,
        IObserver<BindingValue<object?>>
    {
        private readonly IAvaloniaObject _target;
        private readonly AvaloniaProperty<T> _property;
        private readonly IObservable<BindingValue<object?>> _source;
        private IDisposable? _subscription;

        public TypedBindingAdapter(
            IAvaloniaObject target,
            AvaloniaProperty<T> property,
            IObservable<BindingValue<object?>> source)
        {
            _target = target;
            _property = property;
            _source = source;
        }

        public void OnNext(BindingValue<object?> value)
        {
            try
            {
                PublishNext(value.Convert<T>());
            }
            catch (InvalidCastException e)
            {
                var unwrappedValue = value.HasValue ? value.Value : null;
                
                Logger.TryGet(LogEventLevel.Error, LogArea.Binding)?.Log(
                    _target,
                    "Binding produced invalid value for {$Property} ({$PropertyType}): {$Value} ({$ValueType})",
                    _property.Name,
                    _property.PropertyType,
                    unwrappedValue,
                    unwrappedValue?.GetType());
                PublishNext(BindingValue<T>.BindingError(e));
            }
        }

        public void OnCompleted() => PublishCompleted();
        public void OnError(Exception error) => PublishError(error);

        public static IObservable<BindingValue<T>> Create(
            IAvaloniaObject target,
            AvaloniaProperty<T> property,
            IObservable<BindingValue<object?>> source)
        {
            return source is IObservable<BindingValue<T>> result ?
                result :
                new TypedBindingAdapter<T>(target, property, source);
        }

        protected override void Subscribed() => _subscription = _source.Subscribe(this);
        protected override void Unsubscribed() => _subscription?.Dispose();
    }
}
