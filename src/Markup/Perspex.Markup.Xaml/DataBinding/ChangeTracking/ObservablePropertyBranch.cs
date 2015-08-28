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
        private readonly object root;
        private readonly PropertyPath propertyPath;
        private PropertyMountPoint mountPoint;

        public ObservablePropertyBranch(object root, PropertyPath propertyPath)
        {
            Guard.ThrowIfNull(root, nameof(root));
            Guard.ThrowIfNull(propertyPath, nameof(propertyPath));

            this.root = root;
            this.propertyPath = propertyPath;
            this.mountPoint = new PropertyMountPoint(root, propertyPath);
            var subscriptions = this.GetInpcNodes();
            this.Changed = this.CreateObservableFromNodes(subscriptions);
        }

        private IObservable<object> CreateObservableFromNodes(IEnumerable<InpcNode> subscriptions)
        {
            return subscriptions.Select(
                subscription => Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                    ev => subscription.Parent.PropertyChanged += ev,
                    handler => subscription.Parent.PropertyChanged -= handler)
                    .Do(_ => this.mountPoint = new PropertyMountPoint(this.root, this.propertyPath))
                    .Where(pattern => pattern.EventArgs.PropertyName == subscription.PropertyName))
                    .Merge();
        }

        private IEnumerable<InpcNode> GetInpcNodes()
        {
            return this.GetSubscriptionsRecursive(this.root, this.propertyPath, 0);
        }

        private IEnumerable<InpcNode> GetSubscriptionsRecursive(object current, PropertyPath propertyPath, int i)
        {
            var subscriptions = new List<InpcNode>();
            var inpc = current as INotifyPropertyChanged;

            if (inpc == null)
            {
                return subscriptions;
            }

            var nextPropertyName = propertyPath.Chunks[i];
            subscriptions.Add(new InpcNode(inpc, nextPropertyName));

            if (i < this.propertyPath.Chunks.Length)
            {
                var currentObjectTypeInfo = current.GetType().GetTypeInfo();
                var nextProperty = currentObjectTypeInfo.GetDeclaredProperty(nextPropertyName);
                var nextInstance = nextProperty.GetValue(current);
                subscriptions.AddRange(this.GetSubscriptionsRecursive(nextInstance, propertyPath, i + 1));
            }

            return subscriptions;
        }

        public IObservable<object> Changed { get; }

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

        private class InpcNode
        {
            public InpcNode(INotifyPropertyChanged parent, string propertyName)
            {
                this.Parent = parent;
                this.PropertyName = propertyName;
            }

            public INotifyPropertyChanged Parent { get; }

            public string PropertyName { get; }
        }
    }
}