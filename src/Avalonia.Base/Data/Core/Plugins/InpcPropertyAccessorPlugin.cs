using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Avalonia.Utilities;

namespace Avalonia.Data.Core.Plugins
{
    /// <summary>
    /// Reads a property from a standard C# object that optionally supports the
    /// <see cref="INotifyPropertyChanged"/> interface.
    /// </summary>
    [RequiresUnreferencedCode(TrimmingMessages.PropertyAccessorsRequiresUnreferencedCodeMessage)]
    internal class InpcPropertyAccessorPlugin : IPropertyAccessorPlugin
    {
        private readonly Dictionary<(Type, string), PropertyInfo?> _propertyLookup =
            new Dictionary<(Type, string), PropertyInfo?>();

        /// <inheritdoc/>
        public bool Match(object obj, string propertyName) => GetFirstPropertyWithName(obj, propertyName) != null;

        /// <summary>
        /// Starts monitoring the value of a property on an object.
        /// </summary>
        /// <param name="reference">The object.</param>
        /// <param name="propertyName">The property name.</param>
        /// <returns>
        /// An <see cref="IPropertyAccessor"/> interface through which future interactions with the 
        /// property will be made.
        /// </returns>
        public IPropertyAccessor? Start(WeakReference<object?> reference, string propertyName)
        {
            _ = reference ?? throw new ArgumentNullException(nameof(reference));
            _ = propertyName ?? throw new ArgumentNullException(nameof(propertyName));

            if (!reference.TryGetTarget(out var instance) || instance is null)
                return null;

            var p = GetFirstPropertyWithName(instance, propertyName);

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

        private const BindingFlags PropertyBindingFlags =
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;

        private PropertyInfo? GetFirstPropertyWithName(object instance, string propertyName)
        {
            if (instance is IReflectableType reflectableType && instance is not Type)
                return reflectableType.GetTypeInfo().GetProperty(propertyName, PropertyBindingFlags);

            var type = instance.GetType();

            var key = (type, propertyName);

            if (!_propertyLookup.TryGetValue(key, out var propertyInfo))
            {
                propertyInfo = TryFindAndCacheProperty(type, propertyName);
            }

            return propertyInfo;
        }

        private PropertyInfo? TryFindAndCacheProperty(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type type, string propertyName)
        {
            PropertyInfo? found = null;

            var properties = type.GetProperties(PropertyBindingFlags);

            foreach (var propertyInfo in properties)
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

        [RequiresUnreferencedCode(TrimmingMessages.PropertyAccessorsRequiresUnreferencedCodeMessage)]
        private class Accessor : PropertyAccessorBase, IWeakEventSubscriber<PropertyChangedEventArgs>
        {
            private readonly WeakReference<object?> _reference;
            private readonly PropertyInfo _property;
            private bool _eventRaised;

            public Accessor(WeakReference<object?> reference, PropertyInfo property)
            {
                _ = reference ?? throw new ArgumentNullException(nameof(reference));
                _ = property ?? throw new ArgumentNullException(nameof(property));

                _reference = reference;
                _property = property;
            }

            public override Type? PropertyType => _property.PropertyType;

            public override object? Value
            {
                get
                {
                    var o = GetReferenceTarget();
                    return o != null ? _property.GetValue(o) : null;
                }
            }

            public override bool SetValue(object? value, BindingPriority priority)
            {
                if (_property.CanWrite)
                {
                    if (!TypeUtilities.TryConvert(_property.PropertyType, value, null, out var converted))
                        throw new ArgumentException($"Object of type '{value?.GetType()}' " +
                            $"cannot be converted to type '{_property.PropertyType}'.");

                    _eventRaised = false;
                    _property.SetValue(GetReferenceTarget(), converted);

                    if (!_eventRaised)
                    {
                        SendCurrentValue();
                    }

                    return true;
                }

                return false;
            }

            void IWeakEventSubscriber<PropertyChangedEventArgs>.
                OnEvent(object? notifyPropertyChanged, WeakEvent ev, PropertyChangedEventArgs e)
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
                    WeakEvents.ThreadSafePropertyChanged.Unsubscribe(inpc, this);
            }

            private object? GetReferenceTarget()
            {
                _reference.TryGetTarget(out var target);

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
                    WeakEvents.ThreadSafePropertyChanged.Subscribe(inpc, this);
            }
        }
    }
}
