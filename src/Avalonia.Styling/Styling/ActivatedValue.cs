// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace Avalonia.Styling
{
    /// <summary>
    /// An value which is switched on or off according to an activator observable.
    /// </summary>
    /// <remarks>
    /// An <see cref="ActivatedValue"/> has two inputs: an activator observable and an
    /// <see cref="Value"/>. When the activator produces true, the 
    /// <see cref="ActivatedValue"/> will produce the current value. When the activator 
    /// produces false it will produce <see cref="AvaloniaProperty.UnsetValue"/>.
    /// </remarks>
    internal class ActivatedValue : IObservable<object>, IDescription
    {
        private static readonly object NotSent = new object();
        private List<IObserver<object>> _observers = new List<IObserver<object>>();
        private IDisposable _activatorSubscription;
        private object _value;
        private object _last = NotSent;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivatedObservable"/> class.
        /// </summary>
        /// <param name="activator">The activator.</param>
        /// <param name="value">The activated value.</param>
        /// <param name="description">The binding description.</param>
        public ActivatedValue(
            IObservable<bool> activator,
            object value,
            string description)
        {
            Contract.Requires<ArgumentNullException>(activator != null);

            Activator = activator;
            Value = value;
            Description = description;
            Listener = CreateListener();
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
        /// Gets a value indicating whether the activator is active.
        /// </summary>
        public bool? IsActive { get; private set; }

        /// <summary>
        /// Gets the value that will be produced when <see cref="IsActive"/> is true.
        /// </summary>
        public object Value
        {
            get => _value;
            protected set
            {
                _value = value;
                PublishValue();
            }
        }

        protected ActivatorListener Listener { get; }

        public virtual IDisposable Subscribe(IObserver<object> observer)
        {
            _observers.Add(observer);

            if (_observers.Count == 1)
            {
                Initialize();
            }

            return Disposable.Create(() =>
            {
                _observers.Remove(observer);

                if (_observers.Count == 0)
                {
                    Deinitialize();
                }
            });
        }

        protected virtual ActivatorListener CreateListener() => new ActivatorListener(this);

        protected virtual void Deinitialize()
        {
            _activatorSubscription.Dispose();
            _activatorSubscription = null;
        }

        protected virtual void Initialize()
        {
            _activatorSubscription = Activator.Subscribe(Listener);
        }

        protected virtual void NotifyCompleted()
        {
            foreach (var observer in _observers)
            {
                observer.OnCompleted();
            }

            Deinitialize();
            _observers = null;
        }

        protected virtual void NotifyError(Exception error)
        {
            foreach (var observer in _observers)
            {
                observer.OnError(error);
            }

            Deinitialize();
            _observers = null;
        }

        protected virtual void NotifyActive(bool active)
        {
            IsActive = active;
            PublishValue();
        }

        private void PublishValue()
        {
            if (IsActive.HasValue)
            {
                var v = IsActive.Value ? Value : AvaloniaProperty.UnsetValue;

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

        protected class ActivatorListener : IObserver<bool>
        {
            public ActivatorListener(ActivatedValue parent)
            {
                Parent = parent;
            }

            protected ActivatedValue Parent { get; }

            void IObserver<bool>.OnCompleted() => Parent.NotifyCompleted();
            void IObserver<bool>.OnError(Exception error) => Parent.NotifyError(error);
            void IObserver<bool>.OnNext(bool value) => Parent.NotifyActive(value);
        }
    }
}
