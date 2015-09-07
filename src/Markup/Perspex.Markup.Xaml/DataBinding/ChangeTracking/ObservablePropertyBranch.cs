// -----------------------------------------------------------------------
// <copyright file="ObservablePropertyBranch.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Markup.Xaml.DataBinding.ChangeTracking
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Reflection;
    using Glass;

    public class ObservablePropertyBranch
    {
        private readonly object instance;
        private readonly PropertyPath propertyPath;
        private readonly PropertyMountPoint mountPoint;

        public ObservablePropertyBranch(object instance, PropertyPath propertyPath)
        {
            Guard.ThrowIfNull(instance, nameof(instance));
            Guard.ThrowIfNull(propertyPath, nameof(propertyPath));

            this.instance = instance;
            this.propertyPath = propertyPath;
            this.mountPoint = new PropertyMountPoint(instance, propertyPath);
            var properties = this.GetPropertiesThatRaiseNotifications();
            this.Values = this.CreateUnifiedObservableFromNodes(properties);
        }

        public IObservable<object> Values { get; private set; }

        private IObservable<object> CreateUnifiedObservableFromNodes(IEnumerable<PropertyDefinition> subscriptions)
        {
            return subscriptions.Select(this.GetObservableFromProperty).Merge();
        }

        private IObservable<object> GetObservableFromProperty(PropertyDefinition subscription)
        {
            return Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                parentOnPropertyChanged => subscription.Parent.PropertyChanged += parentOnPropertyChanged,
                parentOnPropertyChanged => subscription.Parent.PropertyChanged -= parentOnPropertyChanged)
                .Where(pattern => pattern.EventArgs.PropertyName == subscription.PropertyName)
                .Select(pattern => this.mountPoint.Value);
        }

        private IEnumerable<PropertyDefinition> GetPropertiesThatRaiseNotifications()
        {
            return this.GetSubscriptionsRecursive(this.instance, this.propertyPath, 0);
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

            if (i < this.propertyPath.Chunks.Length)
            {
                var currentObjectTypeInfo = current.GetType().GetTypeInfo();
                var nextProperty = currentObjectTypeInfo.GetDeclaredProperty(nextPropertyName);
                var nextInstance = nextProperty.GetValue(current);

                if (i < this.propertyPath.Chunks.Length - 1)
                {
                    subscriptions.AddRange(this.GetSubscriptionsRecursive(nextInstance, propertyPath, i + 1));
                }
            }

            return subscriptions;
        }

        public object Value
        {
            get
            {
                return this.mountPoint.Value;
            }

            set
            {
                this.mountPoint.Value = value;
            }
        }

        public Type Type => this.mountPoint.ProperyType;

        private class PropertyDefinition
        {
            public PropertyDefinition(INotifyPropertyChanged parent, string propertyName)
            {
                this.Parent = parent;
                this.PropertyName = propertyName;
            }

            public INotifyPropertyChanged Parent { get; }

            public string PropertyName { get; }
        }
    }
}