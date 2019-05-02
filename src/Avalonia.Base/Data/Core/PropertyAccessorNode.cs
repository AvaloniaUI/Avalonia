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

        protected override void StartListeningCore(WeakReference reference)
        {
            var plugin = ExpressionObserver.PropertyAccessors.FirstOrDefault(x => x.Match(reference.Target, PropertyName));
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
                throw new NotSupportedException(
                    $"Could not find a matching property accessor for {PropertyName}.");
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
