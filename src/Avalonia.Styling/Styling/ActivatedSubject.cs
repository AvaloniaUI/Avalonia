// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Avalonia.Styling
{
    /// <summary>
    /// A subject which is switched on or off according to an activator observable.
    /// </summary>
    /// <remarks>
    /// An <see cref="ActivatedSubject"/> has two inputs: an activator observable and either an
    /// <see cref="ActivatedValue"/> or a <see cref="Source"/> observable which produces the
    /// activated value. When the activator produces true, the <see cref="ActivatedObservable"/> will
    /// produce the current activated value. When the activator produces false it will produce
    /// <see cref="AvaloniaProperty.UnsetValue"/>.
    /// </remarks>
    internal class ActivatedSubject : ActivatedObservable, ISubject<object>, IDescription
    {
        private bool? _active;
        private bool _completed;
        private object _value;

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
            : base(activator, source, description)
        {
            Activator.Subscribe(ActivatorChanged, ActivatorError, ActivatorCompleted);
        }

        /// <summary>
        /// Gets the underlying subject.
        /// </summary>
        public new ISubject<object> Source
        {
            get { return (ISubject<object>)base.Source; }
        }

        /// <summary>
        /// Notifies all subscribed observers about the end of the sequence.
        /// </summary>
        public void OnCompleted()
        {
            if (_active.Value && !_completed)
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
            if (_active.Value && !_completed)
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
            _value = value;

            if (_active.Value && !_completed)
            {
                Source.OnNext(value);
            }
        }

        private void ActivatorChanged(bool active)
        {
            bool first = !_active.HasValue;

            _active = active;

            if (!first)
            {
                Source.OnNext(active ? _value : AvaloniaProperty.UnsetValue);
            }
        }

        private void ActivatorCompleted()
        {
            _completed = true;
            Source.OnCompleted();
        }

        private void ActivatorError(Exception e)
        {
            _completed = true;
            Source.OnError(e);
        }
    }
}
