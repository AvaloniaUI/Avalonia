using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Reactive.Operators;
using Avalonia.Threading;

namespace Avalonia.Reactive;

/// <summary>
/// Provides common observable methods as a replacement for the Rx framework.
/// </summary>
internal static class Observable
{
    public static IObservable<TSource> Create<TSource>(Func<IObserver<TSource>, IDisposable> subscribe)
    {
        return new CreateWithDisposableObservable<TSource>(subscribe);
    }

    public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> action)
    {
        return source.Subscribe(new AnonymousObserver<T>(action));
    }

    public static IObservable<TResult> Select<TSource, TResult>(this IObservable<TSource> source, Func<TSource, TResult> selector)
    {
        return Create<TResult>(obs =>
        {
            return source.Subscribe(new AnonymousObserver<TSource>(
                input =>
                {
                    TResult value;
                    try
                    {
                        value = selector(input);
                    }
                    catch (Exception ex)
                    {
                        obs.OnError(ex);
                        return;
                    }

                    obs.OnNext(value);
                }, obs.OnError, obs.OnCompleted));
        });
    }

    public static IObservable<TSource> StartWith<TSource>(this IObservable<TSource> source, TSource value)
    {
        return Create<TSource>(obs =>
        {
            obs.OnNext(value);
            return source.Subscribe(obs);
        });
    }
    
    public static IObservable<TSource> Where<TSource>(this IObservable<TSource> source, Func<TSource, bool> predicate)
    {
        return Create<TSource>(obs =>
        {
            return source.Subscribe(new AnonymousObserver<TSource>(
                input =>
                {
                    bool shouldRun;
                    try
                    {
                        shouldRun = predicate(input);
                    }
                    catch (Exception ex)
                    {
                        obs.OnError(ex);
                        return;
                    }
                    if (shouldRun)
                    {
                        obs.OnNext(input);
                    }
                }, obs.OnError, obs.OnCompleted));
        });
    }

    public static IObservable<TSource> Switch<TSource>(
        this IObservable<IObservable<TSource>> sources)
    {
        return new Switch<TSource>(sources);
    }

    public static IObservable<TResult> CombineLatest<TFirst, TSecond, TResult>(
        this IObservable<TFirst> first, IObservable<TSecond> second,
        Func<TFirst, TSecond, TResult> resultSelector)
    {
        return new CombineLatest<TFirst, TSecond, TResult>(first, second, resultSelector);
    }
    
    public static IObservable<TInput[]> CombineLatest<TInput>(
        this IEnumerable<IObservable<TInput>> inputs)
    {
        return new CombineLatest<TInput, TInput[]>(inputs, items => items);
    }

    public static IObservable<T> Skip<T>(this IObservable<T> source, int skipCount)
    {
        if (skipCount <= 0)
        {
            throw new ArgumentException("Skip count must be bigger than zero", nameof(skipCount));
        }

        return Create<T>(obs =>
        {
            var remaining = skipCount;
            return source.Subscribe(new AnonymousObserver<T>(
                input =>
                {
                    if (remaining <= 0)
                    {
                        obs.OnNext(input);
                    }
                    else
                    {
                        remaining--;
                    }
                }, obs.OnError, obs.OnCompleted));
        });
    }
    
    public static IObservable<T> Take<T>(this IObservable<T> source, int takeCount)
    {
        if (takeCount <= 0)
        {
            return Empty<T>();
        }

        return Create<T>(obs =>
        {
            var remaining = takeCount;
            IDisposable? sub = null;
            sub = source.Subscribe(new AnonymousObserver<T>(
                input =>
                {
                    if (remaining > 0)
                    {
                        --remaining;
                        obs.OnNext(input);

                        if (remaining == 0)
                        {
                            sub?.Dispose();
                            obs.OnCompleted();
                        }
                    }
                }, obs.OnError, obs.OnCompleted));
            return sub;
        });
    }

    public static IObservable<EventArgs> FromEventPattern(Action<EventHandler> addHandler, Action<EventHandler> removeHandler)
    {
        return Create<EventArgs>(observer =>
        {
            var handler = new Action<EventArgs>(observer.OnNext);
            var converted = new EventHandler((_, args) => handler(args));
            addHandler(converted);

            return Disposable.Create(() => removeHandler(converted));
        });
    }
    
    public static IObservable<T> FromEventPattern<T>(Action<EventHandler<T>> addHandler, Action<EventHandler<T>> removeHandler) where T : EventArgs
    {
        return Create<T>(observer =>
        {
            var handler = new Action<T>(observer.OnNext);
            var converted = new EventHandler<T>((_, args) => handler(args));
            addHandler(converted);

            return Disposable.Create(() => removeHandler(converted));
        });
    }
    
    public static IObservable<T> Return<T>(T value)
    {
        return new ReturnImpl<T>(value);
    }
    
    public static IObservable<T> Empty<T>()
    {
        return EmptyImpl<T>.Instance;
    }
        
    /// <summary>
    /// Returns an observable that fires once with the specified value and never completes.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>The observable.</returns>
    public static IObservable<T> SingleValue<T>(T value)
    {
        return new SingleValueImpl<T>(value);
    }
 
    private sealed class SingleValueImpl<T> : IObservable<T>
    {
        private readonly T _value;

        public SingleValueImpl(T value)
        {
            _value = value;
        }
        public IDisposable Subscribe(IObserver<T> observer)
        {
            observer.OnNext(_value);
            return Disposable.Empty;
        }
    }
    
    private sealed class ReturnImpl<T> : IObservable<T>
    {
        private readonly T _value;

        public ReturnImpl(T value)
        {
            _value = value;
        }
        public IDisposable Subscribe(IObserver<T> observer)
        {
            observer.OnNext(_value);
            observer.OnCompleted();
            return Disposable.Empty;
        }
    }
    
    internal sealed class EmptyImpl<TResult> : IObservable<TResult>
    {
        internal static readonly IObservable<TResult> Instance = new EmptyImpl<TResult>();

        private EmptyImpl() { }

        public IDisposable Subscribe(IObserver<TResult> observer)
        {
            observer.OnCompleted();
            return Disposable.Empty;
        }
    }
    
    private sealed class CreateWithDisposableObservable<TSource> : IObservable<TSource>
    {
        private readonly Func<IObserver<TSource>, IDisposable> _subscribe;

        public CreateWithDisposableObservable(Func<IObserver<TSource>, IDisposable> subscribe)
        {
            _subscribe = subscribe;
        }

        public IDisposable Subscribe(IObserver<TSource> observer)
        {
            return _subscribe(observer);
        }
    }
}
