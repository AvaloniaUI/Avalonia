using System;

namespace Avalonia.Reactive;

internal class CombinedSubject<T> : IAvaloniaSubject<T>
{
    private readonly IObserver<T> _observer;
    private readonly IObservable<T> _observable;

    public CombinedSubject(IObserver<T> observer, IObservable<T> observable)
    {
        _observer = observer;
        _observable = observable;
    }

    public void OnCompleted() => _observer.OnCompleted();

    public void OnError(Exception error) => _observer.OnError(error);

    public void OnNext(T value) => _observer.OnNext(value);

    public IDisposable Subscribe(IObserver<T> observer) => _observable.Subscribe(observer);
}
