// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Data;
using Avalonia.Markup.Data.Plugins;

namespace Avalonia.Markup.Data
{
    internal class PropertyAccessorNode : ExpressionNode, ISettableNode
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
        public Type PropertyType => _accessor?.PropertyType;

        public bool SetTargetValue(object value, BindingPriority priority)
        {
            if (_accessor != null)
            {
                try { return _accessor.SetValue(value, priority); } catch { }
            }

            return false;
        }

        protected override IObservable<object> StartListeningCore(WeakReference reference)
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

            // Ensure that _accessor is set for the duration of the subscription.
            return Observable.Using(
                () =>
                {
                    _accessor = accessor;
                    return Disposable.Create(() => _accessor = null);
                },
                _ => accessor);
        }
    }
}
