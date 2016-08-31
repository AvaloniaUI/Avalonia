using System;
using System.Reactive.Disposables;
using Avalonia.Data;

namespace Avalonia.Markup.Data.Plugins
{
    /// <summary>
    /// An <see cref="IPropertyAccessor"/> that represents an error.
    /// </summary>
    public class PropertyError : IPropertyAccessor
    {
        private BindingNotification _error;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyError"/> class.
        /// </summary>
        /// <param name="error">The error to report.</param>
        public PropertyError(BindingNotification error)
        {
            _error = error;
        }

        /// <inheritdoc/>
        public Type PropertyType => null;

        /// <inheritdoc/>
        public object Value => _error;

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public bool SetValue(object value, BindingPriority priority)
        {
            return false;
        }

        public IDisposable Subscribe(IObserver<object> observer)
        {
            observer.OnNext(_error);
            return Disposable.Empty;
        }
    }
}
