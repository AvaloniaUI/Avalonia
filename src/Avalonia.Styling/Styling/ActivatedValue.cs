// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive;
using System.Reactive.Linq;

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
    internal class ActivatedValue : ObservableBase<object>, IDescription
    {
        /// <summary>
        /// The activator.
        /// </summary>
        private readonly IObservable<bool> _activator;

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
            _activator = activator;
            Value = value;
            Description = description;
        }

        /// <summary>
        /// Gets the activated value.
        /// </summary>
        public object Value
        {
            get;
        }

        /// <summary>
        /// Gets a description of the binding.
        /// </summary>
        public string Description
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

            return _activator
                .Select(active => active ? Value : AvaloniaProperty.UnsetValue)
                .Subscribe(observer);
        }
    }
}
