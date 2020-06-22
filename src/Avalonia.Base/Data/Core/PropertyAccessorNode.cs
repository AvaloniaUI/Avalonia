using System;
using Avalonia.Data.Core.Plugins;

namespace Avalonia.Data.Core
{
    public class PropertyAccessorNode : SettableNode
    {
        private readonly bool _enableValidation;
        private IPropertyAccessor _accessor;

        public PropertyAccessorNode(string propertyName, bool enableValidation)
        {
            PropertyName = propertyName;
            _enableValidation = enableValidation;
        }

        public override string Description => PropertyName;
        public string PropertyName { get; }
        public override Type PropertyType => _accessor?.PropertyType;

        protected override bool SetTargetValueCore(object value, BindingPriority priority)
        {
            if (_accessor != null)
            {
                try
                {
                    return _accessor.SetValue(value, priority);
                }
                catch { }
            }

            return false;
        }

        protected override void StartListeningCore(WeakReference<object> reference)
        {
            reference.TryGetTarget(out object target);

            IPropertyAccessorPlugin plugin = null;

            foreach (IPropertyAccessorPlugin x in ExpressionObserver.PropertyAccessors)
            {
                if (x.Match(target, PropertyName))
                {
                    plugin = x;
                    break;
                }
            }

            var accessor = plugin?.Start(reference, PropertyName);

            if (_enableValidation && Next == null)
            {
                foreach (var validator in ExpressionObserver.DataValidators)
                {
                    if (validator.Match(reference, PropertyName))
                    {
                        accessor = validator.Start(reference, PropertyName, accessor);
                    }
                }
            }

            if (accessor == null)
            {
                reference.TryGetTarget(out object instance);

                var message = $"Could not find a matching property accessor for '{PropertyName}' on '{instance}'";

                var exception = new MissingMemberException(message);

                accessor = new PropertyError(new BindingNotification(exception, BindingErrorType.Error));
            }

            _accessor = accessor;
            accessor.Subscribe(ValueChanged);
        }

        protected override void StopListeningCore()
        {
            _accessor.Dispose();
            _accessor = null;
        }
    }
}
