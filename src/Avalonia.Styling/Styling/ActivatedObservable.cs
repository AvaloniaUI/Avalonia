// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace Avalonia.Styling
{
    /// <summary>
    /// An observable which is switched on or off according to an activator observable.
    /// </summary>
    /// <remarks>
    /// An <see cref="ActivatedObservable"/> has two inputs: an activator observable a 
    /// <see cref="Source"/> observable which produces the activated value. When the activator 
    /// produces true, the <see cref="ActivatedObservable"/> will produce the current activated 
    /// value. When the activator produces false it will produce
    /// <see cref="AvaloniaProperty.UnsetValue"/>.
    /// </remarks>
    internal class ActivatedObservable : IObservable<object>, IDescription
    {
        private static readonly object NotSent = new object();
        private readonly Listener _listener;
        private List<IObserver<object>> _observers;
        private IDisposable _activatorSubscription;
        private IDisposable _sourceSubscription;
        private bool? _active;
        private object _value;
        private object _last = NotSent;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivatedObservable"/> class.
        /// </summary>
        /// <param name="activator">The activator.</param>
        /// <param name="source">An observable that produces the activated value.</param>
        /// <param name="description">The binding description.</param>
        public ActivatedObservable(
            IObservable<bool> activator,
            IObservable<object> source,
            string description)
        {
            Contract.Requires<ArgumentNullException>(activator != null);
            Contract.Requires<ArgumentNullException>(source != null);

            Activator = activator;
            Description = description;
            Source = source;
            _listener = new Listener(this);
        }

        /// <summary>
        /// Gets the activator observable.
        /// </summary>
        public IObservable<bool> Activator { get; }

        /// <summary>
        /// Gets a description of the binding.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets an observable which produces the <see cref="ActivatedValue"/>.
        /// </summary>
        public IObservable<object> Source { get; }

        public IDisposable Subscribe(IObserver<object> observer)
        {
            var subscribe = _observers == null;

            _observers = _observers ?? new List<IObserver<object>>();
            _observers.Add(observer);

            if (subscribe)
            {
                _sourceSubscription = Source.Subscribe(_listener);
                _activatorSubscription = Activator.Subscribe(_listener);
            }

            return Disposable.Create(() =>
            {
                _observers.Remove(observer);

                if (_observers.Count == 0)
                {
                    _activatorSubscription.Dispose();
                    _sourceSubscription.Dispose();
                    _activatorSubscription = null;
                    _sourceSubscription = null;
                }
            });
        }

        private void NotifyCompleted()
        {
            foreach (var observer in _observers)
            {
                observer.OnCompleted();
            }

            _observers = null;
        }

        private void NotifyError(Exception error)
        {
            foreach (var observer in _observers)
            {
                observer.OnError(error);
            }

            _observers = null;
        }

        private void Update()
        {
            if (_active.HasValue)
            {
                var v = _active.Value ? _value : AvaloniaProperty.UnsetValue;

                if (!Equals(v, _last))
                {
                    foreach (var observer in _observers)
                    {
                        observer.OnNext(v);
                    }

                    _last = v;
                }
            }
        }

        private class Listener : IObserver<bool>, IObserver<object>
        {
            private readonly ActivatedObservable _parent;

            public Listener(ActivatedObservable parent)
            {
                _parent = parent;
            }

            void IObserver<bool>.OnCompleted() => _parent.NotifyCompleted();
            void IObserver<object>.OnCompleted() => _parent.NotifyCompleted();
            void IObserver<bool>.OnError(Exception error) => _parent.NotifyError(error);
            void IObserver<object>.OnError(Exception error) => _parent.NotifyError(error);

            void IObserver<bool>.OnNext(bool value)
            {
                _parent._active = value;
                _parent.Update();
            }

            void IObserver<object>.OnNext(object value)
            {
                _parent._value = value;
                _parent.Update();
            }
        }
    }
}
