using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Avalonia.Reactive;

/// <summary>
/// Class to create an <see cref="IObserver{T}"/> instance from delegate-based implementations of the On* methods.
/// </summary>
/// <typeparam name="T">The type of the elements in the sequence.</typeparam>
public class AnonymousObserver<T> : IObserver<T>
{
    private static readonly Action<Exception> ThrowsOnError = ex => throw ex;
    private static readonly Action NoOpCompleted = () => { };  
    private readonly Action<T> _onNext;
    private readonly Action<Exception> _onError;
    private readonly Action _onCompleted;

    public AnonymousObserver(TaskCompletionSource<T> tcs)
    {
        if (tcs is null)
        {
            throw new ArgumentNullException(nameof(tcs));
        }

        _onNext = tcs.SetResult;
        _onError = tcs.SetException;
        _onCompleted = NoOpCompleted;
    }
    
    public AnonymousObserver(Action<T> onNext, Action<Exception> onError, Action onCompleted)
    {
        _onNext = onNext ?? throw new ArgumentNullException(nameof(onNext));
        _onError = onError ?? throw new ArgumentNullException(nameof(onError));
        _onCompleted = onCompleted ?? throw new ArgumentNullException(nameof(onCompleted));
    }

    public AnonymousObserver(Action<T> onNext)
        : this(onNext, ThrowsOnError, NoOpCompleted)
    {
    }

    public AnonymousObserver(Action<T> onNext, Action<Exception> onError)
        : this(onNext, onError, NoOpCompleted)
    {
    }

    public AnonymousObserver(Action<T> onNext, Action onCompleted)
        : this(onNext, ThrowsOnError, onCompleted)
    {
    }

    public void OnCompleted()
    {
        _onCompleted.Invoke();
    }

    public void OnError(Exception error)
    {
        _onError.Invoke(error);
    }

    public void OnNext(T value)
    {
        _onNext.Invoke(value);
    }
}
