using System;

namespace Avalonia.Data.Core.Plugins
{
    /// <summary>
    /// An <see cref="IPropertyAccessor"/> that represents an error.
    /// </summary>
    public class PropertyError : IPropertyAccessor
    {
        private readonly BindingNotification _error;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyError"/> class.
        /// </summary>
        /// <param name="error">The error to report.</param>
        public PropertyError(BindingNotification error)
        {
            _error = error;
        }

        /// <inheritdoc/>
        public Type? PropertyType => null;

        /// <inheritdoc/>
        public object? Value => _error;

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public bool SetValue(object? value, BindingPriority priority)
        {
            return false;
        }

        public void Subscribe(Action<object> listener)
        {
            listener(_error);
        }

        public void Unsubscribe()
        {
        }
    }
}
