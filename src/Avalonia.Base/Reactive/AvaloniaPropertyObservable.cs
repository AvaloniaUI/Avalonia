using System;
using System.Collections.Generic;
using Avalonia.Collections.Pooled;
using Avalonia.Data;
using Avalonia.Threading;

#nullable enable

namespace Avalonia.Reactive
{
    internal abstract class AvaloniaPropertyObservable<T> :
        IObservable<AvaloniaPropertyChangedEventArgs<T>>,
        IDescription
    {
        private readonly WeakReference<AvaloniaObject> _owner;
        private object? _observer;
        private PooledQueue<AvaloniaPropertyChangedEventArgs<T>>? _queue;
        private AvaloniaPropertyChangedEventArgs<T>? _signalling;
        private ValueSelector? _valueAdapter;
        private BindingValueSelector? _bindingValueAdapter;
        private UntypedValueSelector? _untypedValueAdapter;

        private AvaloniaPropertyObservable(AvaloniaObject owner)
        {
            owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _owner = new WeakReference<AvaloniaObject>(owner);
        }

        public string Description
        {
            get
            {
                if (_owner.TryGetTarget(out var owner))
                {
                    return $"{owner.GetType().Name}.{Property.Name}";
                }
                else
                {
                    return $"(dead).{Property.Name}";
                }
            }
        }

        public abstract AvaloniaProperty<T> Property { get; }

        public IObservable<T> ValueAdapter
        {
            get => _valueAdapter ??= new ValueSelector(this);
        }

        public IObservable<BindingValue<T>> BindingValueAdapter
        {
            get => _bindingValueAdapter ??= new BindingValueSelector(this);
        }

        public IObservable<object?> UntypedValueAdapter
        {
            get => _untypedValueAdapter ??= new UntypedValueSelector(this);
        }

        public static AvaloniaPropertyObservable<T> Create(AvaloniaObject o, StyledPropertyBase<T> property)
        {
            return new Styled(o, property);
        }

        public static AvaloniaPropertyObservable<T> Create(AvaloniaObject o, DirectPropertyBase<T> property)
        {
            return new Direct(o, property);
        }

        public Optional<T> GetValue()
        {
            if (_owner.TryGetTarget(out var owner))
            {
                return GetValue(owner);
            }

            return default;
        }

        public void Signal(AvaloniaPropertyChangedEventArgs<T> change)
        {
            if (SignalCore(change) && _queue is object)
            {
                while (_queue.Count > 0)
                {
                    var queuedChange = _queue.Dequeue();

                    if (_queue.Count != 0)
                    {
                        queuedChange.MarkOutdated();
                    }

                    SignalCore(queuedChange);
                }

                _queue.Dispose();
                _queue = null;
            }
        }

        public IDisposable Subscribe(IObserver<AvaloniaPropertyChangedEventArgs<T>> observer)
        {
            observer = observer ?? throw new ArgumentNullException(nameof(observer));
            return SubscribeCore(new ObserverEntry(ObserverType.AvaloniaPropertyChange, observer));
        }

        protected abstract T GetValue(AvaloniaObject owner);
        
        private IDisposable SubscribeCore(ObserverEntry entry)
        {
            Dispatcher.UIThread.VerifyAccess();

            if (_observer is null)
            {
                _observer = entry;
            }
            else
            {
                if (!(_observer is List<ObserverEntry> list))
                {
                    var existing = (ObserverEntry)_observer;
                    _observer = list = new List<ObserverEntry>();
                    list.Add(existing);
                }

                list.Add(entry);
            }

            return new Disposable(this, entry);
        }

        private bool SignalCore(AvaloniaPropertyChangedEventArgs<T> change)
        {
            if (_signalling is null)
            {
                _signalling = change;

                if (_observer is List<ObserverEntry> list)
                {
                    foreach (var observer in list)
                    {
                        if (_queue?.Count > 0 && !change.IsOutdated)
                        {
                            change.MarkOutdated();
                        }

                        SignalCore(observer, change);
                    }
                }
                else if (_observer is ObserverEntry e)
                {
                    SignalCore(e, change);
                }

                _signalling = null;
                return true;
            }
            else
            {
                _queue ??= new PooledQueue<AvaloniaPropertyChangedEventArgs<T>>();
                _queue.Enqueue(change);
                _signalling.MarkOutdated();
                return false;
            }
        }

        private void SignalCore(ObserverEntry entry, AvaloniaPropertyChangedEventArgs<T> change)
        {
            if (entry.Type == ObserverType.AvaloniaPropertyChange)
            {
                ((IObserver<AvaloniaPropertyChangedEventArgs<T>>)entry.Observer).OnNext(change);
            }
            else if (!change.IsOutdated && change.IsActiveValueChange)
            {
                switch (entry.Type)
                {
                    case ObserverType.BindingValue:
                        ((IObserver<BindingValue<T>>)entry.Observer).OnNext(change.NewValue);
                        break;
                    case ObserverType.TypedValue:
                        ((IObserver<T>)entry.Observer).OnNext(change.NewValue.Value);
                        break;
                    case ObserverType.UntypedValue:
                        ((IObserver<object?>)entry.Observer).OnNext(change.NewValue.ToUntyped());
                        break;
                }
            }
        }

        private void Remove(ObserverEntry entry)
        {
            if (_observer is ObserverEntry e &&
                e.Type == entry.Type &&
                e.Observer == entry.Observer)
            {
                _observer = null;
            }
            else if (_observer is List<ObserverEntry> list)
            {
                for (var i = 0; i < list.Count; ++i)
                {
                    if (list[i].Type == entry.Type && list[i].Observer == entry.Observer)
                    {
                        list.RemoveAt(i);
                    }
                }
            }
        }

        private class Disposable : IDisposable
        {
            private readonly AvaloniaPropertyObservable<T> _owner;
            private readonly ObserverEntry _entry;

            public Disposable(AvaloniaPropertyObservable<T> owner, ObserverEntry entry)
            {
                _owner = owner;
                _entry = entry;
            }

            public void Dispose()
            {
                _owner.Remove(_entry);
            }
        }

        private class ValueSelector : IObservable<T>
        {
            private readonly AvaloniaPropertyObservable<T> _owner;
            public ValueSelector(AvaloniaPropertyObservable<T> owner) => _owner = owner;
            public IDisposable Subscribe(IObserver<T> observer)
            {
                observer = observer ?? throw new ArgumentNullException(nameof(observer));

                var value = _owner.GetValue();
                var result = _owner.SubscribeCore(new ObserverEntry(ObserverType.TypedValue, observer));

                if (value.HasValue)
                {
                    observer.OnNext(value.Value);
                }

                return result;
            }
        }

        private class BindingValueSelector : IObservable<BindingValue<T>>
        {
            private readonly AvaloniaPropertyObservable<T> _owner;
            public BindingValueSelector(AvaloniaPropertyObservable<T> owner) => _owner = owner;

            public IDisposable Subscribe(IObserver<BindingValue<T>> observer)
            {
                observer = observer ?? throw new ArgumentNullException(nameof(observer));

                var value = _owner.GetValue();
                var result = _owner.SubscribeCore(new ObserverEntry(ObserverType.BindingValue, observer));

                if (value.HasValue)
                {
                    observer.OnNext(value.Value);
                }

                return result;
            }
        }

        private class UntypedValueSelector : IObservable<object?>
        {
            private readonly AvaloniaPropertyObservable<T> _owner;
            public UntypedValueSelector(AvaloniaPropertyObservable<T> owner) => _owner = owner;

            public IDisposable Subscribe(IObserver<object?> observer)
            {
                var value = _owner.GetValue();
                var result = _owner.SubscribeCore(new ObserverEntry(ObserverType.UntypedValue, observer));

                if (value.HasValue)
                {
                    observer.OnNext(value.Value);
                }

                return result;
            }
        }

        private class Styled : AvaloniaPropertyObservable<T>
        {
            private readonly StyledPropertyBase<T> _property;

            public Styled(AvaloniaObject owner, StyledPropertyBase<T> property)
                : base(owner)
            {
                _property = property ?? throw new ArgumentNullException(nameof(property));
            }

            public override AvaloniaProperty<T> Property => _property;
            protected override T GetValue(AvaloniaObject owner) => owner.GetValue(_property);
        }

        private class Direct : AvaloniaPropertyObservable<T>
        {
            private readonly DirectPropertyBase<T> _property;

            public Direct(AvaloniaObject owner, DirectPropertyBase<T> property)
                : base(owner)
            {
                _property = property ?? throw new ArgumentNullException(nameof(property));
            }

            public override AvaloniaProperty<T> Property => _property;
            protected override T GetValue(AvaloniaObject owner) => owner.GetValue(_property);
        }

        private enum ObserverType
        {
            AvaloniaPropertyChange,
            BindingValue,
            TypedValue,
            UntypedValue,
        }

        private struct ObserverEntry
        {
            public ObserverEntry(ObserverType type, object observer)
            {
                Type = type;
                Observer = observer;
            }

            public ObserverType Type { get; }
            public object Observer { get; }
        }
    }
}
