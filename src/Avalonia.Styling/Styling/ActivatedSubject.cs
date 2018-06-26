// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Subjects;

namespace Avalonia.Styling
{
    /// <summary>
    /// A subject which is switched on or off according to an activator observable.
    /// </summary>
    /// <remarks>
    /// An <see cref="ActivatedSubject"/> extends <see cref="ActivatedObservable"/> to
    /// be an <see cref="ISubject{Object}"/>. When the object is active then values
    /// received via <see cref="OnNext(object)"/> will be passed to the source subject.
    /// </remarks>
    internal class ActivatedSubject : ActivatedObservable, ISubject<object>, IDescription
    {
        private bool _completed;
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
            : base(activator, source, description)
        {
        }

        /// <summary>
        /// Gets the underlying subject.
        /// </summary>
        public new ISubject<object> Source
        {
            get { return (ISubject<object>)base.Source; }
        }

        public void OnCompleted()
        {
            Source.OnCompleted();
        }

        public void OnError(Exception error)
        {
            Source.OnError(error);
        }

        public void OnNext(object value)
        {
            _pushValue = value;

            if (IsActive == true && !_completed)
            {
                Source.OnNext(_pushValue);
            }
        }

        protected override void ActiveChanged(bool active)
        {
            bool first = !IsActive.HasValue;

            base.ActiveChanged(active);

            if (!first)
            {
                Source.OnNext(active ? _pushValue : AvaloniaProperty.UnsetValue);
            }
        }

        protected override void CompletedReceived()
        {
            base.CompletedReceived();

            if (!_completed)
            {
                Source.OnCompleted();
                _completed = true;
            }
        }

        protected override void ErrorReceived(Exception error)
        {
            base.ErrorReceived(error);

            if (!_completed)
            {
                Source.OnError(error);
                _completed = true;
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
