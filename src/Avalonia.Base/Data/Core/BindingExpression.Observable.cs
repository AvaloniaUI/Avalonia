using System;

namespace Avalonia.Data.Core;

// Mostly for unit tests, BindingExpression implements IObservable<object?> and IObserver<object?>.
// We limit support to a single subscriber in this scenario.
internal partial class BindingExpression : IObservable<object?>, IObserver<object?>
{
    IDisposable IObservable<object?>.Subscribe(IObserver<object?> observer)
    {
        if (observer is null)
            throw new ArgumentNullException(nameof(observer));
        if (_sink is not null)
            throw new InvalidOperationException(
                $"An {nameof(BindingExpression)} may only have a single subscriber.");

        _sink = new ObservableSink(observer);
        Start(produceValue: true);
        return this;
    }

    void IObserver<object?>.OnCompleted() { }
    void IObserver<object?>.OnError(Exception error) { }
    void IObserver<object?>.OnNext(object? value) => SetValue(value);

    private class ObservableSink : IBindingExpressionSink
    {
        private IObserver<object?> _observer;
        public ObservableSink(IObserver<object?> observer) => _observer = observer;

        public void OnChanged(BindingExpression instance, bool hasValueChanged, bool hasErrorChanged)
        {
            instance.GetDataValidationState(out var state, out var error);

            if (instance.IsDataValidationEnabled || error is not null)
            {
                BindingNotification notification;

                if (state.HasFlag(BindingValueType.BindingError) && error is not null)
                    notification = new(error, BindingErrorType.Error, instance.GetValue());
                else if (state.HasFlag(BindingValueType.DataValidationError) && error is not null)
                    notification = new(error, BindingErrorType.DataValidationError, instance.GetValue());
                else
                    notification = new(instance.GetValue());

                _observer.OnNext(notification);
            }
            else if (hasValueChanged)
            {
                _observer.OnNext(instance.GetValue());
            }
        }

        public void OnCompleted(BindingExpression instance) => _observer.OnCompleted();
    }
}
