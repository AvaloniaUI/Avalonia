// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using Glass;

namespace Perspex.Markup.Xaml.DataBinding.ChangeTracking
{
    public class ObservablePropertyBranch
    {
        private readonly object _instance;
        private readonly PropertyPath _propertyPath;
        private readonly PropertyMountPoint _mountPoint;

        public ObservablePropertyBranch(object instance, PropertyPath propertyPath)
        {
            Guard.ThrowIfNull(instance, nameof(instance));
            Guard.ThrowIfNull(propertyPath, nameof(propertyPath));

            _instance = instance;
            _propertyPath = propertyPath;
            _mountPoint = new PropertyMountPoint(instance, propertyPath);
            var properties = GetPropertiesThatRaiseNotifications();
            Values = CreateUnifiedObservableFromNodes(properties);
        }

        public IObservable<object> Values { get; private set; }

        private IObservable<object> CreateUnifiedObservableFromNodes(IEnumerable<PropertyDefinition> subscriptions)
        {
            return subscriptions.Select(GetObservableFromProperty).Merge();
        }

        private IObservable<object> GetObservableFromProperty(PropertyDefinition subscription)
        {
            return Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                parentOnPropertyChanged => subscription.Parent.PropertyChanged += parentOnPropertyChanged,
                parentOnPropertyChanged => subscription.Parent.PropertyChanged -= parentOnPropertyChanged)
                .Where(pattern => pattern.EventArgs.PropertyName == subscription.PropertyName)
                .Select(pattern => _mountPoint.Value);
        }

        private IEnumerable<PropertyDefinition> GetPropertiesThatRaiseNotifications()
        {
            return GetSubscriptionsRecursive(_instance, _propertyPath, 0);
        }

        private IEnumerable<PropertyDefinition> GetSubscriptionsRecursive(object current, PropertyPath propertyPath, int i)
        {
            var subscriptions = new List<PropertyDefinition>();
            var inpc = current as INotifyPropertyChanged;

            if (inpc == null)
            {
                return subscriptions;
            }

            var nextPropertyName = propertyPath.Chunks[i];
            subscriptions.Add(new PropertyDefinition(inpc, nextPropertyName));

            if (i < _propertyPath.Chunks.Length)
            {
                var currentObjectTypeInfo = current.GetType().GetTypeInfo();
                var nextProperty = currentObjectTypeInfo.GetDeclaredProperty(nextPropertyName);
                var nextInstance = nextProperty.GetValue(current);

                if (i < _propertyPath.Chunks.Length - 1)
                {
                    subscriptions.AddRange(GetSubscriptionsRecursive(nextInstance, propertyPath, i + 1));
                }
            }

            return subscriptions;
        }

        public object Value
        {
            get
            {
                return _mountPoint.Value;
            }

            set
            {
                _mountPoint.Value = value;
            }
        }

        public Type Type => _mountPoint.ProperyType;

        private class PropertyDefinition
        {
            public PropertyDefinition(INotifyPropertyChanged parent, string propertyName)
            {
                Parent = parent;
                PropertyName = propertyName;
            }

            public INotifyPropertyChanged Parent { get; }

            public string PropertyName { get; }
        }
    }
}