using System;
using System.Reactive.Subjects;
using Avalonia.Reactive;

namespace Avalonia.Data.Core
{
    /// <summary>
    /// A binding expression which uses delegates to read and write a bound value.
    /// </summary>
    /// <typeparam name="TIn">The input type of the binding expression.</typeparam>
    /// <typeparam name="TOut">The output type of the binding expression.</typeparam>
    /// <remarks>
    /// A <see cref="TypedBindingExpression{TIn, TOut}"/> represents a typed binding which has been
    /// instantiated on an object.
    /// </remarks>
    public class TypedBindingExpression<TIn, TOut> : LightweightObservableBase<BindingValue<TOut>>,
        ISubject<BindingValue<TOut>>,
        IDescription
            where TIn : class?
    {
        private readonly IObservable<TIn?> _rootSource;
        private readonly Func<TIn, TOut>? _read;
        private readonly Action<TIn, TOut>? _write;
        private readonly TypedBindingTrigger<TIn>[]? _triggers;
        private readonly Optional<TOut> _fallbackValue;
        private readonly Action<int> _triggerFired;
        private IDisposable? _rootSourceSubsciption;
        private WeakReference<TIn>? _root;
        private bool _initialized;
        private bool _rootHasFired;
        private bool _listening;
        private int _publishCount;

        internal TypedBindingExpression(
            IObservable<TIn?> root,
            Func<TIn, TOut>? read,
            Action<TIn, TOut>? write,
            TypedBindingTrigger<TIn>[]? triggers,
            Optional<TOut> fallbackValue)
        {
            _rootSource = root ?? throw new ArgumentNullException(nameof(root));
            _read = read;
            _write = write;
            _triggers = triggers;
            _fallbackValue = fallbackValue;
            _triggerFired = TriggerFired;
        }

        public string Description => "TODO";

        /// <summary>
        /// Writes the specified value to the binding source if a write function was supplied.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <returns>
        /// True if the value could be written to the binding source; otherwise false.
        /// </returns>
        public bool Write(TOut value)
        {
            if (_write is not null &&
                _root is not null &&
                _root.TryGetTarget(out var root))
            {
                try
                {
                    var c = _publishCount;
                    if (_initialized)
                        _write.Invoke(root, value);
                    if (_publishCount == c)
                        PublishValue();
                    return true;
                }
                catch
                {
                    PublishValue();
                }
            }

            return false;
        }

        /// <summary>
        /// Reads from the binding source to produce a new value.
        /// </summary>
        public void Read() => PublishValue();

        void IObserver<BindingValue<TOut>>.OnNext(BindingValue<TOut> value)
        {
            if (value.HasValue)
                Write(value.Value);
        }

        void IObserver<BindingValue<TOut>>.OnCompleted()
        {
        }

        void IObserver<BindingValue<TOut>>.OnError(Exception error)
        {
        }

        protected override void Initialize()
        {
            _rootHasFired = false;
            _rootSourceSubsciption = _rootSource.Subscribe(RootChanged);
            _initialized = true;
        }

        protected override void Deinitialize()
        {
            StopListeningToChain(0);
            _rootSourceSubsciption?.Dispose();
            _rootSourceSubsciption = null;
            _initialized = false;
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

        private void RootChanged(TIn? value)
        {
            _root = value is null ? null : new WeakReference<TIn>(value);
            _rootHasFired = true;
            StopListeningToChain(0);
            ListenToChain(0);
            PublishValue();
        }

        private void ListenToChain(int from)
        {
            if (_triggers != null && _root != null && _root.TryGetTarget(out var root))
            {
                for (var i = from; i < _triggers.Length; ++i)
                    if (!_triggers[i].Subscribe(root, _triggerFired))
                        break;
                _listening = true;
            }
        }

        private void StopListeningToChain(int from)
        {
            if (!_listening)
                return;

            if (_triggers != null && _root != null && _root.TryGetTarget(out _))
            {
                for (var i = from; i < _triggers.Length; ++i)
                    _triggers[i].Unsubscribe();
            }

            _listening = false;
        }

        private BindingValue<TOut>? GetResult()
        {
            if (_read is null)
                return BindingValue<TOut>.DoNothing;

            if (_root != null && _root.TryGetTarget(out var root))
            {
                try
                {
                    var value = _read(root);
                    return new BindingValue<TOut>(value);
                }
                catch (Exception e)
                {
                    return BindingValue<TOut>.BindingError(e, _fallbackValue);
                }
            }
            else if (_rootHasFired)
            {
                return BindingValue<TOut>.BindingError(new NullReferenceException(), _fallbackValue);
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

        private void TriggerFired(int index)
        {
            StopListeningToChain(index + 1);
            ListenToChain(index + 1);
            PublishValue();
        }

        private struct Link
        {
            public Link(Func<TIn, object> eval)
            {
                Eval = eval;
                Value = null;
            }

            public Func<TIn, object> Eval;
            public WeakReference<object>? Value;
        }

        [Flags]
        private enum Flags
        {
            Initialized = 0x01,
            RootHasFired = 0x02,
            ListeningToChain = 0x04,
        }
    }
}
