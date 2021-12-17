using System;

namespace Avalonia.Data.Core.Plugins
{
    /// <summary>
    /// Defines a default base implementation for a <see cref="IPropertyAccessor"/>.
    /// </summary>
    public abstract class PropertyAccessorBase : IPropertyAccessor
    {
        private Action<object?>? _listener;

        /// <inheritdoc/>
        public abstract Type? PropertyType { get; }

        /// <inheritdoc/>
        public abstract object? Value { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_listener != null)
            {
                Unsubscribe();
            }
        }

        /// <inheritdoc/>
        public abstract bool SetValue(object? value, BindingPriority priority);

        /// <inheritdoc/>
        public void Subscribe(Action<object?> listener)
        {
            _ = listener ?? throw new ArgumentNullException(nameof(listener));

            if (_listener != null)
            {
                throw new InvalidOperationException(
                    "A member accessor can be subscribed to only once.");
            }

            _listener = listener;
            SubscribeCore();
        }

        public void Unsubscribe()
        {
            if (_listener == null)
            {
                throw new InvalidOperationException(
                    "The member accessor was not subscribed.");
            }

            UnsubscribeCore();
            _listener = null;
        }

        /// <summary>
        /// Publishes a value to the listener.
        /// </summary>
        /// <param name="value">The value.</param>
        protected void PublishValue(object? value) => _listener?.Invoke(value);

        /// <summary>
        /// When overridden in a derived class, begins listening to the member.
        /// </summary>
        protected abstract void SubscribeCore();

        /// <summary>
        /// When overridden in a derived class, stops listening to the member.
        /// </summary>
        protected abstract void UnsubscribeCore();
    }
}
