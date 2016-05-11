// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive;
using System.Reactive.Linq;

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
    internal class ActivatedObservable : ObservableBase<object>, IDescription
    {
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

        /// <summary>
        /// Notifies the provider that an observer is to receive notifications.
        /// </summary>
        /// <param name="observer">The observer.</param>
        /// <returns>IDisposable object used to unsubscribe from the observable sequence.</returns>
        protected override IDisposable SubscribeCore(IObserver<object> observer)
        {
            Contract.Requires<ArgumentNullException>(observer != null);

            var sourceCompleted = Source.LastOrDefaultAsync().Select(_ => Unit.Default);
            var activatorCompleted = Activator.LastOrDefaultAsync().Select(_ => Unit.Default);
            var completed = sourceCompleted.Merge(activatorCompleted);

            return Activator
                .CombineLatest(Source, (x, y) => new { Active = x, Value = y })
                .Select(x => x.Active ? x.Value : AvaloniaProperty.UnsetValue)
                .DistinctUntilChanged()
                .TakeUntil(completed)
                .Subscribe(observer);
        }
    }
}
