using System;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Reactive;

/// <summary>
/// Subscribes an <see cref="IObserver{T}"/> to an <see cref="IObservable{T}"/> such that the
/// observable holds only a weak reference to the observer, disposing the subscription once the
/// observer has been collected.
/// </summary>
/// <typeparam name="T">The type of the elements in the sequence.</typeparam>
internal sealed class WeakObserverSubscription<T> : IObserver<T>, IDisposable
{
    private readonly WeakReference<IObserver<T>> _observer;
    private IDisposable? _subscription;

    private WeakObserverSubscription(IObserver<T> observer)
    {
        _observer = new WeakReference<IObserver<T>>(observer);
    }

    /// <summary>
    /// Subscribes <paramref name="observer"/> to <paramref name="observable"/> via a weak reference.
    /// </summary>
    /// <returns>
    /// A disposable which unsubscribes from the observable when disposed. The caller must keep it
    /// alive for as long as the subscription is required.
    /// </returns>
    public static IDisposable Subscribe(IObservable<T> observable, IObserver<T> observer)
    {
        var subscription = new WeakObserverSubscription<T>(observer);
        subscription._subscription = observable.Subscribe(subscription);
        return subscription;
    }

    public void OnCompleted()
    {
        if (TryGetObserver(out var observer))
            observer.OnCompleted();
    }

    public void OnError(Exception error)
    {
        if (TryGetObserver(out var observer))
            observer.OnError(error);
    }

    public void OnNext(T value)
    {
        if (TryGetObserver(out var observer))
            observer.OnNext(value);
    }

    public void Dispose()
    {
        _subscription?.Dispose();
        _subscription = null;
    }

    private bool TryGetObserver([NotNullWhen(true)] out IObserver<T>? observer)
    {
        if (_observer.TryGetTarget(out observer))
            return true;

        // The observer has been collected; unsubscribe from the observable.
        Dispose();
        return false;
    }
}
