using System;
using Perspex.Data;

namespace Perspex.Markup.Data.Plugins
{

    /// <summary>
    /// A base class for validating <see cref="IPropertyAccessor"/>s that wraps an <see cref="IPropertyAccessor"/> and forwards method calls to it.
    /// </summary>
    public abstract class ValidatingPropertyAccessorBase : IPropertyAccessor
    {
        protected readonly WeakReference _reference;
        protected readonly string _name;
        private readonly IPropertyAccessor _accessor;
        private readonly Action<IValidationStatus> _callback;

        protected ValidatingPropertyAccessorBase(WeakReference reference, string name, IPropertyAccessor accessor, Action<IValidationStatus> callback)
        {
            _reference = reference;
            _name = name;
            _accessor = accessor;
            _callback = callback;
        }

        /// <inheritdoc/>
        public Type PropertyType => _accessor.PropertyType;

        /// <inheritdoc/>
        public object Value => _accessor.Value;

        /// <inheritdoc/>
        public virtual void Dispose() => _accessor.Dispose();

        /// <inheritdoc/>
        public virtual bool SetValue(object value, BindingPriority priority) => _accessor.SetValue(value, priority);

        /// <summary>
        /// Sends the validation status to the callback specified in construction.
        /// </summary>
        /// <param name="status">The validation status.</param>
        protected void SendValidationCallback(IValidationStatus status)
        {
            _callback?.Invoke(status);
        }
    }
}