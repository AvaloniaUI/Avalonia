using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Data.Core.Plugins;

namespace Avalonia.Data.Core
{
    [RequiresUnreferencedCode(TrimmingMessages.ExpressionNodeRequiresUnreferencedCodeMessage)]
    public class PropertyAccessorNode : SettableNode
    {
        private readonly bool _enableValidation;
        private IPropertyAccessorPlugin? _customPlugin;
        private IPropertyAccessor? _accessor;

        public PropertyAccessorNode(string propertyName, bool enableValidation)
        {
            PropertyName = propertyName;
            _enableValidation = enableValidation;
        }

        public PropertyAccessorNode(string propertyName, bool enableValidation, IPropertyAccessorPlugin customPlugin)
        {
            PropertyName = propertyName;
            _enableValidation = enableValidation;
            _customPlugin = customPlugin;
        }

        public override string Description => PropertyName;
        public string PropertyName { get; }
        public override Type? PropertyType => _accessor?.PropertyType;

        protected override bool SetTargetValueCore(object? value, BindingPriority priority)
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

        protected override void StartListeningCore(WeakReference<object?> reference)
        {
            if (!reference.TryGetTarget(out var target) || target is null)
                return;

            var plugin = _customPlugin ?? GetPropertyAccessorPluginForObject(target);
            var accessor = plugin?.Start(reference, PropertyName);

            // We need to handle accessor fallback before handling validation. Validators do not support null accessors.
            if (accessor == null)
            {
                reference.TryGetTarget(out var instance);

                var message = $"Could not find a matching property accessor for '{PropertyName}' on '{instance}'";

                var exception = new MissingMemberException(message);

                accessor = new PropertyError(new BindingNotification(exception, BindingErrorType.Error));
            }

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

            if (accessor is null)
            {
                throw new AvaloniaInternalException("Data validators must return non-null accessor.");
            }

            _accessor = accessor;
            accessor.Subscribe(ValueChanged);
        }

        private IPropertyAccessorPlugin? GetPropertyAccessorPluginForObject(object target)
        {
            foreach (IPropertyAccessorPlugin x in ExpressionObserver.PropertyAccessors)
            {
                if (x.Match(target, PropertyName))
                {
                    return x;
                }
            }
            return null;
        }

        protected override void StopListeningCore()
        {
            _accessor?.Dispose();
            _accessor = null;
        }
    }
}
