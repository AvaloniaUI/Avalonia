// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive;
using System.Reactive.Linq;

namespace Perspex.Styling
{
    /// <summary>
    /// An observable which is switched on or off according to an activator observable.
    /// </summary>
    /// <remarks>
    /// An <see cref="ActivatedObservable"/> has two inputs: an activator observable a 
    /// <see cref="Source"/> observable which produces the activated value. When the activator 
    /// produces true, the <see cref="ActivatedObservable"/> will produce the current activated 
    /// value. When the activator produces false it will produce
    /// <see cref="PerspexProperty.UnsetValue"/>.
    /// </remarks>
    internal class ActivatedObservable : ObservableBase<object>, IDescription
    {
        /// <summary>
        /// The activator.
        /// </summary>
        private readonly IObservable<bool> _activator;

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
            _activator = activator;
            Description = description;
            Source = source;
        }

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

            var sourceCompleted = Source.TakeLast(1).Select(_ => Unit.Default);
            var activatorCompleted = _activator.TakeLast(1).Select(_ => Unit.Default);
            var completed = sourceCompleted.Merge(activatorCompleted);

            return _activator
                .CombineLatest(Source, (x, y) => new { Active = x, Value = y })
                .Select(x => x.Active ? x.Value : PerspexProperty.UnsetValue)
                .DistinctUntilChanged()
                .TakeUntil(completed)
                .Subscribe(observer);
        }
    }
}
