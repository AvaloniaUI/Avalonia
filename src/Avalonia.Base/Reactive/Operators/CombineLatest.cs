using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Avalonia.Reactive.Operators;

// Code based on https://github.com/dotnet/reactive/blob/main/Rx.NET/Source/src/System.Reactive/Linq/Observable/CombineLatest.cs

internal sealed class CombineLatest<TFirst, TSecond, TResult> : IObservable<TResult>
{
    private readonly IObservable<TFirst> _first;
    private readonly IObservable<TSecond> _second;
    private readonly Func<TFirst, TSecond, TResult> _resultSelector;

    public CombineLatest(IObservable<TFirst> first, IObservable<TSecond> second,
        Func<TFirst, TSecond, TResult> resultSelector)
    {
        _first = first;
        _second = second;
        _resultSelector = resultSelector;
    }

    public IDisposable Subscribe(IObserver<TResult> observer)
    {
        var sink = new _(_resultSelector, observer);
        sink.Run(_first, _second);
        return sink;
    }

    internal sealed class _ : IdentitySink<TResult>
    {
        private readonly Func<TFirst, TSecond, TResult> _resultSelector;
        private readonly object _gate = new object();

        public _(Func<TFirst, TSecond, TResult> resultSelector, IObserver<TResult> observer)
            : base(observer)
        {
            _resultSelector = resultSelector;
            _firstDisposable = null!;
            _secondDisposable = null!;
        }

        private IDisposable _firstDisposable;
        private IDisposable _secondDisposable;

        public void Run(IObservable<TFirst> first, IObservable<TSecond> second)
        {
            var fstO = new FirstObserver(this);
            var sndO = new SecondObserver(this);

            fstO.SetOther(sndO);
            sndO.SetOther(fstO);

            _firstDisposable = first.Subscribe(fstO);
            _secondDisposable = second.Subscribe(sndO);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _firstDisposable.Dispose();
                _secondDisposable.Dispose();
            }

            base.Dispose(disposing);
        }

        private sealed class FirstObserver : IObserver<TFirst>
        {
            private readonly _ _parent;
            private SecondObserver _other;

            public FirstObserver(_ parent)
            {
                _parent = parent;
                _other = default!; // NB: Will be set by SetOther.
            }

            public void SetOther(SecondObserver other) { _other = other; }

            public bool HasValue { get; private set; }
            public TFirst? Value { get; private set; }
            public bool Done { get; private set; }

            public void OnNext(TFirst value)
            {
                lock (_parent._gate)
                {
                    HasValue = true;
                    Value = value;

                    if (_other.HasValue)
                    {
                        TResult res;
                        try
                        {
                            res = _parent._resultSelector(value, _other.Value!);
                        }
                        catch (Exception ex)
                        {
                            _parent.ForwardOnError(ex);
                            return;
                        }

                        _parent.ForwardOnNext(res);
                    }
                    else if (_other.Done)
                    {
                        _parent.ForwardOnCompleted();
                    }
                }
            }

            public void OnError(Exception error)
            {
                lock (_parent._gate)
                {
                    _parent.ForwardOnError(error);
                }
            }

            public void OnCompleted()
            {
                lock (_parent._gate)
                {
                    Done = true;

                    if (_other.Done)
                    {
                        _parent.ForwardOnCompleted();
                    }
                    else
                    {
                        _parent._firstDisposable.Dispose();
                    }
                }
            }
        }

        private sealed class SecondObserver : IObserver<TSecond>
        {
            private readonly _ _parent;
            private FirstObserver _other;

            public SecondObserver(_ parent)
            {
                _parent = parent;
                _other = default!; // NB: Will be set by SetOther.
            }

            public void SetOther(FirstObserver other) { _other = other; }

            public bool HasValue { get; private set; }
            public TSecond? Value { get; private set; }
            public bool Done { get; private set; }

            public void OnNext(TSecond value)
            {
                lock (_parent._gate)
                {
                    HasValue = true;
                    Value = value;

                    if (_other.HasValue)
                    {
                        TResult res;
                        try
                        {
                            res = _parent._resultSelector(_other.Value!, value);
                        }
                        catch (Exception ex)
                        {
                            _parent.ForwardOnError(ex);
                            return;
                        }

                        _parent.ForwardOnNext(res);
                    }
                    else if (_other.Done)
                    {
                        _parent.ForwardOnCompleted();
                    }
                }
            }

            public void OnError(Exception error)
            {
                lock (_parent._gate)
                {
                    _parent.ForwardOnError(error);
                }
            }

            public void OnCompleted()
            {
                lock (_parent._gate)
                {
                    Done = true;

                    if (_other.Done)
                    {
                        _parent.ForwardOnCompleted();
                    }
                    else
                    {
                        _parent._secondDisposable.Dispose();
                    }
                }
            }
        }
    }
}

internal sealed class CombineLatest<TSource, TResult> : IObservable<TResult>
{
    private readonly IEnumerable<IObservable<TSource>> _sources;
    private readonly Func<TSource[], TResult> _resultSelector;

    public CombineLatest(IEnumerable<IObservable<TSource>> sources, Func<TSource[], TResult> resultSelector)
    {
        _sources = sources;
        _resultSelector = resultSelector;
    }

    public IDisposable Subscribe(IObserver<TResult> observer)
    {
        var sink = new _(_resultSelector, observer);
        sink.Run(_sources);
        return sink;
    }

    internal sealed class _ : IdentitySink<TResult>
    {
        private readonly object _gate = new object();
        private readonly Func<TSource[], TResult> _resultSelector;

        public _(Func<TSource[], TResult> resultSelector, IObserver<TResult> observer)
            : base(observer)
        {
            _resultSelector = resultSelector;

            // NB: These will be set in Run before getting used.
            _hasValue = null!;
            _values = null!;
            _isDone = null!;
            _subscriptions = null!;
        }

        private bool[] _hasValue;
        private bool _hasValueAll;
        private TSource[] _values;
        private bool[] _isDone;
        private IDisposable[] _subscriptions;

        public void Run(IEnumerable<IObservable<TSource>> sources)
        {
            var srcs = sources.ToArray();

            var N = srcs.Length;

            _hasValue = new bool[N];
            _hasValueAll = false;

            _values = new TSource[N];

            _isDone = new bool[N];

            _subscriptions = new IDisposable[N];

            for (var i = 0; i < N; i++)
            {
                var j = i;

                var o = new SourceObserver(this, j);
                _subscriptions[j] = o;

                o.Disposable = srcs[j].Subscribe(o);
            }

            SetUpstream(new CompositeDisposable(_subscriptions));
        }

        private void OnNext(int index, TSource value)
        {
            lock (_gate)
            {
                _values[index] = value;

                _hasValue[index] = true;

                if (_hasValueAll || (_hasValueAll = _hasValue.All(v => v)))
                {
                    TResult res;
                    try
                    {
                        res = _resultSelector(_values);
                    }
                    catch (Exception ex)
                    {
                        ForwardOnError(ex);
                        return;
                    }

                    ForwardOnNext(res);
                }
                else if (_isDone.Where((_, i) => i != index).All(d => d))
                {
                    ForwardOnCompleted();
                }
            }
        }

        private new void OnError(Exception error)
        {
            lock (_gate)
            {
                ForwardOnError(error);
            }
        }

        private void OnCompleted(int index)
        {
            lock (_gate)
            {
                _isDone[index] = true;

                if (_isDone.All(d => d))
                {
                    ForwardOnCompleted();
                }
                else
                {
                    _subscriptions[index].Dispose();
                }
            }
        }

        private sealed class SourceObserver : IObserver<TSource>, IDisposable
        {
            private readonly _ _parent;
            private readonly int _index;

            public SourceObserver(_ parent, int index)
            {
                _parent = parent;
                _index = index;
            }

            public IDisposable? Disposable { get; set; }

            public void OnNext(TSource value)
            {
                _parent.OnNext(_index, value);
            }

            public void OnError(Exception error)
            {
                _parent.OnError(error);
            }

            public void OnCompleted()
            {
                _parent.OnCompleted(_index);
            }

            public void Dispose()
            {
                Disposable?.Dispose();
            }
        }
    }
}
