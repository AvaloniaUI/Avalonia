// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Data;
using Avalonia.Data.Core.Plugins;

namespace Avalonia.Data.Core
{
    internal class PropertyAccessorNode : ExpressionNode, ISettableNode
    {
        private static readonly object CacheInvalid = new object();
        private readonly bool _enableValidation;
        private IPropertyAccessor _accessor;
        private WeakReference _lastValue = null;

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
                try
                {
                    if (ShouldNotSet(value))
                    {
                        return true;
                    }
                    else
                    {
                        return _accessor.SetValue(value, priority);
                    }
                }
                catch { }
            }

            return false;
        }

        private bool ShouldNotSet(object value)
        {
            if (PropertyType.IsValueType)
            {
                return _lastValue?.Target.Equals(value) ?? false;
            }
            return Object.ReferenceEquals(_lastValue?.Target ?? CacheInvalid, value);
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
                _ => accessor).Select(value =>
                {
                    if (value is BindingNotification notification)
                    {
                        _lastValue = notification.HasValue ? new WeakReference(notification.Value) : null; 
                    }
                    else
                    {
                        _lastValue = new WeakReference(value);
                    }
                    return value;
                });
        }
    }
}
