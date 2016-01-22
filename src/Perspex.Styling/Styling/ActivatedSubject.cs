// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Perspex.Styling
{
    /// <summary>
    /// A subject which is switched on or off according to an activator observable.
    /// </summary>
    /// <remarks>
    /// An <see cref="ActivatedSubject"/> has two inputs: an activator observable and either an
    /// <see cref="ActivatedValue"/> or a <see cref="Source"/> observable which produces the
    /// activated value. When the activator produces true, the <see cref="ActivatedObservable"/> will
    /// produce the current activated value. When the activator produces false it will produce
    /// <see cref="PerspexProperty.UnsetValue"/>.
    /// </remarks>
    internal class ActivatedSubject : ISubject<object>, IDescription
    {
        private IObservable<bool> _activator;
        private bool _active;
        private object _pushValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivatedSubject"/> class.
        /// </summary>
        /// <param name="activator">The activator.</param>
        /// <param name="source">An observable that produces the activated value.</param>
        /// <param name="description">The binding description.</param>
        public ActivatedSubject(
            IObservable<bool> activator,
            ISubject<object> source,
            string description)
        {
            _activator = activator;
            Description = description;
            Source = source;

            _activator.Skip(1).Subscribe(ActivatorChanged);
        }

        /// <summary>
        /// Gets a description of the binding.
        /// </summary>
        public string Description
        {
            get;
        }

        /// <summary>
        /// Gets the underlying subject.
        /// </summary>
        public ISubject<object> Source
        {
            get;
        }

        /// <summary>
        /// Notifies all subscribed observers about the end of the sequence.
        /// </summary>
        public void OnCompleted()
        {
            if (_active)
            {
                Source.OnCompleted();
            }
        }

        /// <summary>
        /// Notifies all subscribed observers with the exception.
        /// </summary>
        /// <param name="error">The exception to send to all subscribed observers.</param>
        /// <exception cref="ArgumentNullException"><paramref name="error"/> is null.</exception>
        public void OnError(Exception error)
        {
            if (_active)
            {
                Source.OnError(error);
            }
        }

        /// <summary>
        /// Notifies all subscribed observers with the value.
        /// </summary>
        /// <param name="value">The value to send to all subscribed observers.</param>        
        public void OnNext(object value)
        {
            _pushValue = value;

            if (_active)
            {
                Source.OnNext(value);
            }
        }

        /// <summary>
        /// Notifies the provider that an observer is to receive notifications.
        /// </summary>
        /// <param name="observer">The observer.</param>
        /// <returns>IDisposable object used to unsubscribe from the observable sequence.</returns>
        public IDisposable Subscribe(IObserver<object> observer)
        {
            Contract.Requires<ArgumentNullException>(observer != null);

            var completed = _activator.TakeLast(1).Select(_ => Unit.Default);

            return _activator
                .CombineLatest(Source, (x, y) => new { Active = x, Value = y })
                .Select(x => x.Active ? x.Value : PerspexProperty.UnsetValue)
                .DistinctUntilChanged()
                .TakeUntil(completed)
                .Subscribe(observer);
        }

        private void ActivatorChanged(bool active)
        {
            _active = active;
            Source.OnNext(active ? _pushValue : PerspexProperty.UnsetValue);
        }
    }
}
