// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Styling
{
    /// <summary>
    /// An observable which is switched on or off according to an activator observable.
    /// </summary>
    /// <remarks>
    /// An <see cref="ActivatedObservable"/> has two inputs: an activator observable and a 
    /// <see cref="Source"/> observable which produces the activated value. When the activator 
    /// produces true, the <see cref="ActivatedObservable"/> will produce the current activated 
    /// value. When the activator produces false it will produce
    /// <see cref="AvaloniaProperty.UnsetValue"/>.
    /// </remarks>
    internal class ActivatedObservable : ActivatedValue, IDescription
    {
        private IDisposable _sourceSubscription;

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
            : base(activator, AvaloniaProperty.UnsetValue, description)
        {
            Contract.Requires<ArgumentNullException>(source != null);

            Source = source;
        }

        /// <summary>
        /// Gets an observable which produces the <see cref="ActivatedValue"/>.
        /// </summary>
        public IObservable<object> Source { get; }

        protected override ActivatorListener CreateListener() => new ValueListener(this);

        protected override void Deinitialize()
        {
            base.Deinitialize();
            _sourceSubscription.Dispose();
            _sourceSubscription = null;
        }

        protected override void Initialize()
        {
            base.Initialize();
            _sourceSubscription = Source.Subscribe((ValueListener)Listener);
        }

        protected virtual void NotifyValue(object value)
        {
            Value = value;
        }

        private class ValueListener : ActivatorListener, IObserver<object>
        {
            public ValueListener(ActivatedObservable parent)
                : base(parent)
            {
            }
            protected new ActivatedObservable Parent => (ActivatedObservable)base.Parent;

            void IObserver<object>.OnCompleted() => Parent.CompletedReceived();
            void IObserver<object>.OnError(Exception error) => Parent.ErrorReceived(error);
            void IObserver<object>.OnNext(object value) => Parent.NotifyValue(value);
        }
    }
}
