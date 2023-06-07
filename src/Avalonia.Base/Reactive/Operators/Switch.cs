using System;

namespace Avalonia.Reactive.Operators;

// Code based on https://github.com/dotnet/reactive/blob/main/Rx.NET/Source/src/System.Reactive/Linq/Observable/Switch.cs

internal sealed class Switch<TSource> : IObservable<TSource>
{
    private readonly IObservable<IObservable<TSource>> _sources;

    public Switch(IObservable<IObservable<TSource>> sources)
    {
        _sources = sources;
    }

    public IDisposable Subscribe(IObserver<TSource> observer)
    {
        return _sources.Subscribe(new _(observer));
    }

    internal sealed class _ : Sink<IObservable<TSource>, TSource>
    {
        private readonly object _gate = new object();

        public _(IObserver<TSource> observer)
            : base(observer)
        {
        }

        private IDisposable? _innerSerialDisposable;
        private bool _isStopped;
        private ulong _latest;
        private bool _hasLatest;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _innerSerialDisposable?.Dispose();
            }

            base.Dispose(disposing);
        }

        public override void OnNext(IObservable<TSource> value)
        {
            ulong id;

            lock (_gate)
            {
                id = unchecked(++_latest);
                _hasLatest = true;
            }

            var innerObserver = new InnerObserver(this, id);

            _innerSerialDisposable = innerObserver;
            innerObserver.Disposable = value.Subscribe(innerObserver);
        }

        public override void OnError(Exception error)
        {
            lock (_gate)
            {
                ForwardOnError(error);
            }
        }

        public override void OnCompleted()
        {
            lock (_gate)
            {
                DisposeUpstream();

                _isStopped = true;
                if (!_hasLatest)
                {
                    ForwardOnCompleted();
                }
            }
        }

        private sealed class InnerObserver : IObserver<TSource>, IDisposable
        {
            private readonly _ _parent;
            private readonly ulong _id;

            public InnerObserver(_ parent, ulong id)
            {
                _parent = parent;
                _id = id;
            }

            public IDisposable? Disposable { get; set; }

            public void OnNext(TSource value)
            {
                lock (_parent._gate)
                {
                    if (_parent._latest == _id)
                    {
                        _parent.ForwardOnNext(value);
                    }
                }
            }

            public void OnError(Exception error)
            {
                lock (_parent._gate)
                {
                    Dispose();

                    if (_parent._latest == _id)
                    {
                        _parent.ForwardOnError(error);
                    }
                }
            }

            public void OnCompleted()
            {
                lock (_parent._gate)
                {
                    Dispose();

                    if (_parent._latest == _id)
                    {
                        _parent._hasLatest = false;

                        if (_parent._isStopped)
                        {
                            _parent.ForwardOnCompleted();
                        }
                    }
                }
            }

            public void Dispose()
            {
                Disposable?.Dispose();
            }
        }
    }
}
