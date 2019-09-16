using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reactive.Subjects;
using Avalonia.Reactive;
using Avalonia.Utilities;

namespace Avalonia.Data.Core
{
    public class CompiledBindingExpression<TIn, TOut> : LightweightObservableBase<BindingValue<TOut>>,
        ISubject<TOut, BindingValue<TOut>>,
        IDescription
            where TIn : class
    {
        private readonly IObservable<TIn> _rootSource;
        private readonly Func<TIn, TOut> _read;
        private readonly Action<TIn, TOut> _write;
        private readonly Link[] _chain;
        private IDisposable _rootSourceSubsciption;
        private WeakReference<TIn> _root;
        private bool _rootHasFired;
        private int _publishCount;

        public CompiledBindingExpression(
            IObservable<TIn> root,
            Func<TIn, TOut> read,
            Action<TIn, TOut> write,
            List<Func<TIn, object>> links)
        {
            Contract.Requires<ArgumentNullException>(root != null);

            _rootSource = root;
            _read = read;
            _write = write;

            if (links != null)
            {
                _chain = new Link[links.Count];

                for (var i = 0; i < links.Count; ++i)
                {
                    _chain[i] = new Link(links[i]);
                }
            }
        }

        public string Description => "TODO";

        public void OnNext(TOut value)
        {
            if (_write != null && _root != null && _root.TryGetTarget(out var root))
            {
                try
                {
                    var c = _publishCount;
                    _write.Invoke(root, value);
                    if (_publishCount == c)
                        PublishValue();
                }
                catch
                {
                    PublishValue();
                }
            }
        }

        void IObserver<TOut>.OnCompleted()
        {
        }

        void IObserver<TOut>.OnError(Exception error)
        {
        }

        protected override void Initialize()
        {
            _rootHasFired = false;
            _rootSourceSubsciption = _rootSource.Subscribe(RootChanged);
        }

        protected override void Deinitialize()
        {
            StopListeningToChain(0);
            _rootSourceSubsciption?.Dispose();
            _rootSourceSubsciption = null;
        }

        protected override void Subscribed(IObserver<BindingValue<TOut>> observer, bool first)
        {
            // If this is the first subscription, `Initialize()` will have run which will
            // already have produced a value.
            if (!first)
            {
                var result = GetResult();

                if (result.HasValue)
                {
                    observer.OnNext(result.Value);
                }
            }
        }

        private void RootChanged(TIn value)
        {
            _root = new WeakReference<TIn>(value);
            _rootHasFired = true;
            StopListeningToChain(0);
            ListenToChain(0);
            PublishValue();
        }

        private void ListenToChain(int from)
        {
            if (_chain != null && _root != null && _root.TryGetTarget(out var root))
            {
                object last = null;

                try
                {
                    for (var i = from; i < _chain.Length; ++i)
                    {
                        var o = _chain[i].Eval(root);

                        if (o != last)
                        {
                            _chain[i].Value = new WeakReference<object>(o);

                            if (SubscribeToChanges(o))
                            {
                                last = o;
                            }
                        }
                    }
                }
                catch
                {
                    // Broken expression chain.
                }
            }
        }

        private void StopListeningToChain(int from)
        {
            if (_chain != null && _root != null && _root.TryGetTarget(out var root))
            {
                for (var i = from; i < _chain.Length; ++i)
                {
                    if (_chain[i].Value != null && 
                        _chain[i].Value.TryGetTarget(out var o) == true)
                    {
                        UnsubscribeToChanges(o);
                    }
                }
            }
        }

        private bool SubscribeToChanges(object o)
        {
            if (o is null)
            {
                return false;
            }

            var result = false;

            if (o is IAvaloniaObject ao)
            {
                WeakEventHandlerManager.Subscribe<IAvaloniaObject, AvaloniaPropertyChangedEventArgs, CompiledBindingExpression<TIn, TOut>>(
                    ao,
                    nameof(ao.PropertyChanged),
                    ChainPropertyChanged);
                result |= true;
            }
            else if (o is INotifyPropertyChanged inpc)
            {
                WeakEventHandlerManager.Subscribe<INotifyPropertyChanged, PropertyChangedEventArgs, CompiledBindingExpression<TIn, TOut>>(
                    inpc,
                    nameof(inpc.PropertyChanged),
                    ChainPropertyChanged);
                result |= true;
            }

            if (o is INotifyCollectionChanged incc)
            {
                WeakEventHandlerManager.Subscribe<INotifyCollectionChanged, NotifyCollectionChangedEventArgs, CompiledBindingExpression<TIn, TOut>>(
                    incc,
                    nameof(incc.CollectionChanged),
                    ChainCollectionChanged);
                result |= true;
            }

            return result;
        }

        private void UnsubscribeToChanges(object o)
        {
            if (o is null)
            {
                return;
            }

            if (o is IAvaloniaObject ao)
            {
                WeakEventHandlerManager.Unsubscribe<AvaloniaPropertyChangedEventArgs, CompiledBindingExpression<TIn, TOut>>(
                    ao,
                    nameof(ao.PropertyChanged),
                    ChainPropertyChanged);
            }
            else if (o is INotifyPropertyChanged inpc)
            {
                WeakEventHandlerManager.Unsubscribe<PropertyChangedEventArgs, CompiledBindingExpression<TIn, TOut>>(
                    inpc,
                    nameof(inpc.PropertyChanged),
                    ChainPropertyChanged);
            }

            if (o is INotifyCollectionChanged incc)
            {
                WeakEventHandlerManager.Unsubscribe<NotifyCollectionChangedEventArgs, CompiledBindingExpression<TIn, TOut>>(
                    incc,
                    nameof(incc.CollectionChanged),
                    ChainCollectionChanged);
            }
        }

        private BindingValue<TOut>? GetResult()
        {
            if (_root != null && _root.TryGetTarget(out var root))
            {
                try
                {
                    var value = _read(root);
                    return new BindingValue<TOut>(value);
                }
                catch (Exception e)
                {
                    return new BindingValue<TOut>(e);
                }
            }
            else if (_rootHasFired)
            {
                return new BindingValue<TOut>(new NullReferenceException());
            }
            else
            {
                return null;
            }
        }

        private void PublishValue()
        {
            var result = GetResult();

            if (result.HasValue)
            {
                unchecked { ++_publishCount; }
                PublishNext(result.Value);
            }
        }

        private int ChainIndexOf(object o)
        {
            if (_chain != null)
            {
                for (var i = 0; i < _chain.Length; ++i)
                {
                    var link = _chain[i];

                    if (link.Value != null &&
                        link.Value.TryGetTarget(out var q) && 
                        ReferenceEquals(o, q))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private void ChainPropertyChanged(object sender)
        {
            var index = ChainIndexOf(sender);

            if (index != -1)
            {
                StopListeningToChain(index);
                ListenToChain(index);
            }

            PublishValue();
        }

        private void ChainPropertyChanged(object sender, PropertyChangedEventArgs e) => ChainPropertyChanged(sender);
        private void ChainPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e) => ChainPropertyChanged(sender);
        private void ChainCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => ChainPropertyChanged(sender);

        private struct Link
        {
            public Link(Func<TIn, object> eval)
            {
                Eval = eval;
                Value = null;
            }

            public Func<TIn, object> Eval;
            public WeakReference<object> Value;
        }
    }
}
