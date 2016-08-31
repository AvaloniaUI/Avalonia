// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Data;

namespace Avalonia.Markup.Data.Plugins
{
    /// <summary>
    /// Defines a default base implementation for a <see cref="IPropertyAccessor"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="IPropertyAccessor"/> is an observable that will only be subscribed to one time.
    /// In addition, the subscription can be disposed by calling <see cref="Dispose()"/> on the
    /// property accessor itself - this prevents needing to hold two references for a subscription.
    /// </remarks>
    public abstract class PropertyAccessorBase : IPropertyAccessor
    {
        /// <inheritdoc/>
        public abstract Type PropertyType { get; }

        /// <inheritdoc/>
        public abstract object Value { get; }

        /// <summary>
        /// Stops the subscription.
        /// </summary>
        public void Dispose() => Dispose(true);

        /// <inheritdoc/>
        public abstract bool SetValue(object value, BindingPriority priority);

        /// <summary>
        /// The currently subscribed observer.
        /// </summary>
        protected IObserver<object> Observer { get; private set; }

        /// <inheritdoc/>
        public IDisposable Subscribe(IObserver<object> observer)
        {
            Contract.Requires<ArgumentNullException>(observer != null);

            if (Observer != null)
            {
                throw new InvalidOperationException(
                    "A property accessor can be subscribed to only once.");
            }

            Observer = observer;
            SubscribeCore(observer);
            return this;
        }

        /// <summary>
        /// Stops listening to the property.
        /// </summary>
        /// <param name="disposing">
        /// True if the <see cref="Dispose()"/> method was called, false if the object is being
        /// finalized.
        /// </param>
        protected virtual void Dispose(bool disposing) => Observer = null;

        /// <summary>
        /// When overridden in a derived class, begins listening to the property.
        /// </summary>
        protected abstract void SubscribeCore(IObserver<object> observer);
    }
}
