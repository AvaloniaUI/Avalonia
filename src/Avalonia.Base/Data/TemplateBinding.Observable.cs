using System;
using Avalonia.Reactive;

namespace Avalonia.Data
{
    // TODO12: Remove IAvaloniaSubject<object?> support from TemplateBinding.
    public partial class TemplateBinding : IAvaloniaSubject<object?>
    {
        private IAvaloniaSubject<object?>? _observableAdapter;

        public IDisposable Subscribe(IObserver<object?> observer)
        {
            _observableAdapter ??= ToObservable();
            return _observableAdapter.Subscribe(observer);
        }

        void IObserver<object?>.OnCompleted() => _observableAdapter?.OnCompleted();
        void IObserver<object?>.OnError(Exception error) => _observableAdapter?.OnError(error);
        void IObserver<object?>.OnNext(object? value) => _observableAdapter?.OnNext(value);
    }
}
