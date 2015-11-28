// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive;
using System.Reactive.Linq;

namespace Perspex.Styling
{
    /// <summary>
    /// Provides an observable for a style.
    /// </summary>
    /// <remarks>
    /// A <see cref="StyleBinding"/> has two inputs: an activator observable and either an
    /// <see cref="ActivatedValue"/> or a <see cref="Source"/> observable which produces the
    /// activated value. When the activator produces true, the <see cref="StyleBinding"/> will
    /// produce the current activated value. When the activator produces false it will produce
    /// <see cref="PerspexProperty.UnsetValue"/>.
    /// </remarks>
    internal class StyleBinding : ObservableBase<object>, IDescription
    {
        /// <summary>
        /// The activator.
        /// </summary>
        private readonly IObservable<bool> _activator;

        /// <summary>
        /// Initializes a new instance of the <see cref="StyleBinding"/> class.
        /// </summary>
        /// <param name="activator">The activator.</param>
        /// <param name="activatedValue">The activated value.</param>
        /// <param name="description">The binding description.</param>
        public StyleBinding(
            IObservable<bool> activator,
            object activatedValue,
            string description)
        {
            _activator = activator;
            ActivatedValue = activatedValue;
            Description = description;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StyleBinding"/> class.
        /// </summary>
        /// <param name="activator">The activator.</param>
        /// <param name="source">An observable that produces the activated value.</param>
        /// <param name="description">The binding description.</param>
        public StyleBinding(
            IObservable<bool> activator,
            IObservable<object> source,
            string description)
        {
            _activator = activator;
            Description = description;
            Source = source;
        }

        /// <summary>
        /// Gets the activated value.
        /// </summary>
        public object ActivatedValue
        {
            get; }

        /// <summary>
        /// Gets a description of the binding.
        /// </summary>
        public string Description
        {
            get;
        }

        /// <summary>
        /// Gets an observable which produces the <see cref="ActivatedValue"/>.
        /// </summary>
        public IObservable<object> Source
        {
            get;
        }

        /// <summary>
        /// Notifies the provider that an observer is to receive notifications.
        /// </summary>
        /// <param name="observer">The observer.</param>
        /// <returns>IDisposable object used to unsubscribe from the observable sequence.</returns>
        protected override IDisposable SubscribeCore(IObserver<object> observer)
        {
            Contract.Requires<ArgumentNullException>(observer != null);

            if (Source == null)
            {
                return _activator.Subscribe(
                    active => observer.OnNext(active ? ActivatedValue : PerspexProperty.UnsetValue),
                    observer.OnError,
                    observer.OnCompleted);
            }
            else
            {
                return _activator
                    .CombineLatest(Source, (x, y) => new { Active = x, Value = y })
                    .Subscribe(x => observer.OnNext(x.Active ? x.Value : PerspexProperty.UnsetValue));
            }
        }
    }
}
