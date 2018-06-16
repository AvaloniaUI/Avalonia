﻿// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Data;

namespace Avalonia.Data.Core.Plugins
{
    /// <summary>
    /// Base class for data validators.
    /// </summary>
    /// <remarks>
    /// Data validators are <see cref="IPropertyAccessor"/>s that are returned from an 
    /// <see cref="IDataValidationPlugin"/>. They wrap an inner <see cref="IPropertyAccessor"/>
    /// and convert any values received from the inner property accessor into
    /// <see cref="BindingNotification"/>s.
    /// </remarks>
    public abstract class DataValidatiorBase : PropertyAccessorBase, IObserver<object>
    {
        private readonly IPropertyAccessor _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataValidatiorBase"/> class.
        /// </summary>
        /// <param name="inner">The inner property accessor.</param>
        protected DataValidatiorBase(IPropertyAccessor inner)
        {
            _inner = inner;
        }

        /// <inheritdoc/>
        public override Type PropertyType => _inner.PropertyType;

        /// <inheritdoc/>
        public override object Value => _inner.Value;

        /// <inheritdoc/>
        public override bool SetValue(object value, BindingPriority priority) => _inner.SetValue(value, priority);

        /// <summary>
        /// Should never be called: the inner <see cref="IPropertyAccessor"/> should never notify
        /// completion.
        /// </summary>
        void IObserver<object>.OnCompleted() { }

        /// <summary>
        /// Should never be called: the inner <see cref="IPropertyAccessor"/> should never notify
        /// an error.
        /// </summary>
        void IObserver<object>.OnError(Exception error) { }

        /// <summary>
        /// Called when the inner <see cref="IPropertyAccessor"/> notifies with a new value.
        /// </summary>
        /// <param name="value">The value.</param>
        void IObserver<object>.OnNext(object value) => InnerValueChanged(value);

        /// <inheritdoc/>
        protected override void Dispose(bool disposing) => _inner.Dispose();

        /// <summary>
        /// Begins listening to the inner <see cref="IPropertyAccessor"/>.
        /// </summary>
        protected override void SubscribeCore(IObserver<object> observer) => _inner.Subscribe(this);

        /// <summary>
        /// Called when the inner <see cref="IPropertyAccessor"/> notifies with a new value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <remarks>
        /// Notifies the observer that the value has changed. The value will be wrapped in a
        /// <see cref="BindingNotification"/> if it is not already a binding notification.
        /// </remarks>
        protected virtual void InnerValueChanged(object value)
        {
            var notification = value as BindingNotification ?? new BindingNotification(value);
            Observer.OnNext(notification);
        }
    }
}