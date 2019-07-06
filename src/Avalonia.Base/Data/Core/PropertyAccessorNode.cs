// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Data.Core.Plugins;

namespace Avalonia.Data.Core
{
    public class PropertyAccessorNode : SettableNode
    {
        private readonly bool _enableValidation;
        private IPropertyAccessorPlugin _customPlugin;
        private IPropertyAccessor _accessor;

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

        protected override void StartListeningCore(WeakReference reference)
        {
            var plugin = _customPlugin ?? GetPropertyAccessorPluginForObject(reference.Target);
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

            _accessor = accessor ?? throw new NotSupportedException(
                    $"Could not find a matching property accessor for {PropertyName}.");
            accessor.Subscribe(ValueChanged);
        }

        private IPropertyAccessorPlugin GetPropertyAccessorPluginForObject(object target)
        {
            return ExpressionObserver.PropertyAccessors.FirstOrDefault(x => x.Match(target, PropertyName));
        }

        protected override void StopListeningCore()
        {
            _accessor.Dispose();
            _accessor = null;
        }
    }
}
