using System;
using System.Buffers;
using System.Collections.Generic;

#nullable enable

namespace Avalonia.Reactive
{
    internal abstract class AvaloniaPropertyObservable : IObservable<object?>, IDescription
    {
        protected readonly WeakReference<AvaloniaObject> _source;
        protected readonly List<object> _observers = new();

        public AvaloniaPropertyObservable(AvaloniaObject source)
        {
            _source = new(source);
        }

        public abstract string Description { get; }

        public IDisposable Subscribe(IObserver<object?> observer)
        {
            var result = SubscribeCore(observer);
            observer.OnNext(GetValueUntyped());
            return result;
        }

        protected abstract object? GetValueUntyped();

        protected IDisposable SubscribeCore(object observer)
        {
            _observers.Add(observer);
            return new RemoveObserver(this, observer);
        }

        private sealed class RemoveObserver : IDisposable
        {
            private readonly AvaloniaPropertyObservable _parent;
            private readonly object _observer;

            public RemoveObserver(AvaloniaPropertyObservable parent, object observer)
            {
                _parent = parent;
                _observer = observer;
            }

            public void Dispose() => _parent._observers.Remove(_observer);
        }
    }

    internal abstract class AvaloniaPropertyObservable<T> : AvaloniaPropertyObservable, IObservable<T?>
    {
        public AvaloniaPropertyObservable(AvaloniaObject source)
            : base(source)
        {
        }

        public void PublishNext()
        {
            if (_observers.Count == 0)
                return;

            if (_observers.Count == 1)
            {
                var observer = _observers[0];
                var value = GetValue();

                if (observer is IObserver<T?> typed)
                    typed.OnNext(value);
                else
                    ((IObserver<object?>)observer).OnNext(value);
            }
            else
            {
                var count = _observers.Count;
                var o = ArrayPool<object>.Shared.Rent(count);

                try
                {
                    _observers.CopyTo(o);

                    for (var i = 0; i < count; ++i)
                    {
                        var observer = o[i];
                        var value = GetValue();

                        if (observer is IObserver<T?> typed)
                            typed.OnNext(value);
                        else
                            ((IObserver<object?>)observer).OnNext(value);
                    }
                }
                finally
                {
                    ArrayPool<object>.Shared.Return(o);
                }
            }
        }

        public IDisposable Subscribe(IObserver<T?> observer)
        {
            var result = SubscribeCore(observer);
            observer.OnNext(GetValue());
            return result;
        }

        protected abstract T? GetValue();
        protected override object? GetValueUntyped() => GetValue();
    }

    internal class StyledPropertyObservable<T> : AvaloniaPropertyObservable<T>, IDescription
    {
        private readonly StyledPropertyBase<T> _property;

        public StyledPropertyObservable(
            AvaloniaObject source,
            StyledPropertyBase<T> property)
            : base(source)
        {
            _property = property;
        }

        public override string Description
        {
            get
            {
                if (_source.TryGetTarget(out var source))
                    return $"{source.GetType().Name}.{_property.Name}";
                else
                    return "(dead)";
            }
        }

        protected override T? GetValue()
        {
            if (_source.TryGetTarget(out var source))
                return source.GetValue(_property);
            return default;
        }
    }

    internal class DirectPropertyObservable<T> : AvaloniaPropertyObservable<T>, IDescription
    {
        private readonly DirectPropertyBase<T> _property;

        public DirectPropertyObservable(
            AvaloniaObject source,
            DirectPropertyBase<T> property)
            : base(source)
        {
            _property = property;
        }

        public override string Description
        {
            get
            {
                if (_source.TryGetTarget(out var source))
                    return $"{source.GetType().Name}.{_property.Name}";
                else
                    return "(dead)";
            }
        }

        protected override T? GetValue()
        {
            if (_source.TryGetTarget(out var source))
                return source.GetValue(_property);
            return default;
        }
    }
}
