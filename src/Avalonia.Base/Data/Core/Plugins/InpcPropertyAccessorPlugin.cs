using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Avalonia.Utilities;

namespace Avalonia.Data.Core.Plugins
{
    /// <summary>
    /// Reads a property from a standard C# object that optionally supports the
    /// <see cref="INotifyPropertyChanged"/> interface.
    /// </summary>
    public class InpcPropertyAccessorPlugin : IPropertyAccessorPlugin
    {
        private readonly Dictionary<(Type, string), PropertyInfo> _propertyLookup =
            new Dictionary<(Type, string), PropertyInfo>();

        /// <inheritdoc/>
        public bool Match(object obj, string propertyName) => GetFirstPropertyWithName(obj.GetType(), propertyName) != null;

        /// <summary>
        /// Starts monitoring the value of a property on an object.
        /// </summary>
        /// <param name="reference">The object.</param>
        /// <param name="propertyName">The property name.</param>
        /// <returns>
        /// An <see cref="IPropertyAccessor"/> interface through which future interactions with the 
        /// property will be made.
        /// </returns>
        public IPropertyAccessor Start(WeakReference<object> reference, string propertyName)
        {
            Contract.Requires<ArgumentNullException>(reference != null);
            Contract.Requires<ArgumentNullException>(propertyName != null);

            reference.TryGetTarget(out object instance);

            var p = GetFirstPropertyWithName(instance.GetType(), propertyName);

            if (p != null)
            {
                return new Accessor(reference, p);
            }
            else
            {
                var message = $"Could not find CLR property '{propertyName}' on '{instance}'";
                var exception = new MissingMemberException(message);
                return new PropertyError(new BindingNotification(exception, BindingErrorType.Error));
            }
        }

        private PropertyInfo GetFirstPropertyWithName(Type type, string propertyName)
        {
            var key = (type, propertyName);

            if (!_propertyLookup.TryGetValue(key, out PropertyInfo propertyInfo))
            {
                propertyInfo = TryFindAndCacheProperty(type, propertyName);
            }

            return propertyInfo;
        }

        private PropertyInfo TryFindAndCacheProperty(Type type, string propertyName)
        {
            PropertyInfo found = null;

            const BindingFlags bindingFlags =
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;

            var properties = type.GetProperties(bindingFlags);

            foreach (PropertyInfo propertyInfo in properties)
            {
                if (propertyInfo.Name == propertyName)
                {
                    found = propertyInfo;

                    break;
                }
            }

            _propertyLookup.Add((type, propertyName), found);

            return found;
        }

        private class Accessor : PropertyAccessorBase, IWeakSubscriber<PropertyChangedEventArgs>
        {
            private readonly WeakReference<object> _reference;
            private readonly PropertyInfo _property;
            private bool _eventRaised;

            public Accessor(WeakReference<object> reference, PropertyInfo property)
            {
                Contract.Requires<ArgumentNullException>(reference != null);
                Contract.Requires<ArgumentNullException>(property != null);

                _reference = reference;
                _property = property;
            }

            public override Type PropertyType => _property.PropertyType;

            public override object Value
            {
                get
                {
                    var o = GetReferenceTarget();
                    return (o != null) ? _property.GetValue(o) : null;
                }
            }

            public override bool SetValue(object value, BindingPriority priority)
            {
                if (_property.CanWrite)
                {
                    _eventRaised = false;
                    _property.SetValue(GetReferenceTarget(), value);

                    if (!_eventRaised)
                    {
                        SendCurrentValue();
                    }

                    return true;
                }

                return false;
            }

            void IWeakSubscriber<PropertyChangedEventArgs>.OnEvent(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == _property.Name || string.IsNullOrEmpty(e.PropertyName))
                {
                    _eventRaised = true;
                    SendCurrentValue();
                }
            }

            protected override void SubscribeCore()
            {
                SubscribeToChanges();
                SendCurrentValue();
            }

            protected override void UnsubscribeCore()
            {
                var inpc = GetReferenceTarget() as INotifyPropertyChanged;

                if (inpc != null)
                {
                    WeakSubscriptionManager.Unsubscribe(
                        inpc,
                        nameof(inpc.PropertyChanged),
                        this);
                }
            }

            private object GetReferenceTarget()
            {
                _reference.TryGetTarget(out object target);

                return target;
            }

            private void SendCurrentValue()
            {
                try
                {
                    var value = Value;
                    PublishValue(value);
                }
                catch { }
            }

            private void SubscribeToChanges()
            {
                var inpc = GetReferenceTarget() as INotifyPropertyChanged;

                if (inpc != null)
                {
                    WeakSubscriptionManager.Subscribe(
                        inpc,
                        nameof(inpc.PropertyChanged),
                        this);
                }
            }
        }
    }
}
