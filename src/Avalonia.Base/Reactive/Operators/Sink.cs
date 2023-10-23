using System;
using System.Threading;

namespace Avalonia.Reactive.Operators;

// Code based on https://github.com/dotnet/reactive/blob/main/Rx.NET/Source/src/System.Reactive/Internal/Sink.cs

internal abstract class Sink<TTarget> : IDisposable
{
    private IDisposable? _upstream;
    private readonly IObserver<TTarget> _observer;

    protected Sink(IObserver<TTarget> observer)
    {
        _observer = observer;
    }

    public void Dispose()
    {
        Dispose(true);
    }

    /// <summary>
    /// Override this method to dispose additional resources.
    /// The method is guaranteed to be called at most once.
    /// </summary>
    /// <param name="disposing">If true, the method was called from <see cref="Dispose()"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
        //Calling base.Dispose(true) is not a proper disposal, so we can omit the assignment here.
        //Sink is internal so this can pretty much be enforced.
        //_observer = NopObserver<TTarget>.Instance;

        _upstream?.Dispose();
    }

    public void ForwardOnNext(TTarget value)
    {
        _observer.OnNext(value);
    }

    public void ForwardOnCompleted()
    {
        _observer.OnCompleted();
        Dispose();
    }

    public void ForwardOnError(Exception error)
    {
        _observer.OnError(error);
        Dispose();
    }

    protected void SetUpstream(IDisposable upstream)
    {
        _upstream = upstream;
    }

    protected void DisposeUpstream()
    {
        _upstream?.Dispose();
    }
}

internal abstract class Sink<TSource, TTarget> : Sink<TTarget>, IObserver<TSource>
{
    protected Sink(IObserver<TTarget> observer) : base(observer)
    {
    }

    public virtual void Run(IObservable<TSource> source)
    {
        SetUpstream(source.Subscribe(this));
    }

    public abstract void OnNext(TSource value);

    public virtual void OnError(Exception error) => ForwardOnError(error);

    public virtual void OnCompleted() => ForwardOnCompleted();

    public IObserver<TTarget> GetForwarder() => new _(this);

    private sealed class _ : IObserver<TTarget>
    {
        private readonly Sink<TSource, TTarget> _forward;

        public _(Sink<TSource, TTarget> forward)
        {
            _forward = forward;
        }

        public void OnNext(TTarget value) => _forward.ForwardOnNext(value);

        public void OnError(Exception error) => _forward.ForwardOnError(error);

        public void OnCompleted() => _forward.ForwardOnCompleted();
    }
}

internal abstract class IdentitySink<T> : Sink<T, T>
{
    protected IdentitySink(IObserver<T> observer) : base(observer)
    {
    }

    public override void OnNext(T value)
    {
        ForwardOnNext(value);
    }
}
