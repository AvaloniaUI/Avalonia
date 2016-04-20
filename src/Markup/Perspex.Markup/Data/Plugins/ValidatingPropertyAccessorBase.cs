using System;
using Perspex.Data;

namespace Perspex.Markup.Data.Plugins
{
    public abstract class ValidatingPropertyAccessorBase : IPropertyAccessor
    {
        protected readonly WeakReference _reference;
        protected readonly string _name;
        private readonly IPropertyAccessor _accessor;
        private readonly Action<ValidationStatus> _callback;

        protected ValidatingPropertyAccessorBase(WeakReference reference, string name, IPropertyAccessor accessor, Action<ValidationStatus> callback)
        {
            _reference = reference;
            _name = name;
            _accessor = accessor;
            _callback = callback;
        }

        public Type PropertyType => _accessor.PropertyType;

        public object Value => _accessor.Value;

        public virtual void Dispose() => _accessor.Dispose();

        public virtual bool SetValue(object value, BindingPriority priority) => _accessor.SetValue(value, priority);

        protected void SendValidationCallback(ValidationStatus status)
        {
            _callback?.Invoke(status);
        }
    }
}