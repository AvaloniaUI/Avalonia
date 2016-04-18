// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using Perspex.Data;
using Perspex.Logging;
using Perspex.Utilities;
using System.Collections;

namespace Perspex.Markup.Data.Plugins
{
    /// <summary>
    /// Reads a property from a standard C# object that optionally supports the
    /// <see cref="INotifyPropertyChanged"/> interface.
    /// </summary>
    public class InpcPropertyAccessorPlugin : IPropertyAccessorPlugin
    {
        /// <summary>
        /// Checks whether this plugin can handle accessing the properties of the specified object.
        /// </summary>
        /// <param name="reference">The object.</param>
        /// <returns>True if the plugin can handle the object; otherwise false.</returns>
        public bool Match(WeakReference reference)
        {
            Contract.Requires<ArgumentNullException>(reference != null);

            return true;
        }

        /// <summary>
        /// Starts monitoring the value of a property on an object.
        /// </summary>
        /// <param name="reference">The object.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="changed">A function to call when the property changes.</param>
        /// <returns>
        /// An <see cref="IPropertyAccessor"/> interface through which future interactions with the 
        /// property will be made.
        /// </returns>
        public IPropertyAccessor Start(
            WeakReference reference, 
            string propertyName, 
            Action<object> changed)
        {
            Contract.Requires<ArgumentNullException>(reference != null);
            Contract.Requires<ArgumentNullException>(propertyName != null);
            Contract.Requires<ArgumentNullException>(changed != null);

            var instance = reference.Target;
            var p = instance.GetType().GetRuntimeProperties().FirstOrDefault(_ => _.Name == propertyName);

            if (p != null)
            {
                return new Accessor(reference, p, changed);
            }
            else
            {
                var message = $"Could not find CLR property '{propertyName}' on '{instance}'";
                var exception = new MissingMemberException(message);
                return new PropertyError(new BindingError(exception));
            }
        }

        private class Accessor : IPropertyAccessor, IWeakSubscriber<PropertyChangedEventArgs>
        {
            private readonly WeakReference _reference;
            private readonly PropertyInfo _property;
            private readonly Action<object> _changed;

            public Accessor(
                WeakReference reference, 
                PropertyInfo property, 
                Action<object> changed)
            {
                Contract.Requires<ArgumentNullException>(reference != null);
                Contract.Requires<ArgumentNullException>(property != null);

                _reference = reference;
                _property = property;
                _changed = changed;

                var inpc = reference.Target as INotifyPropertyChanged;

                if (inpc != null)
                {
                    WeakSubscriptionManager.Subscribe<PropertyChangedEventArgs>(
                        inpc,
                        nameof(inpc.PropertyChanged),
                        this);
                }
                else
                {
                    Logger.Warning(
                        LogArea.Binding,
                        this,
                        "Bound to property {Property} on {Source} which does not implement INotifyPropertyChanged",
                        property.Name,
                        reference.Target,
                        reference.Target.GetType());
                }
            }

            public Type PropertyType => _property.PropertyType;

            public object Value => _property.GetValue(_reference.Target);

            public void Dispose()
            {
                var inpc = _reference.Target as INotifyPropertyChanged;

                if (inpc != null)
                {
                    WeakSubscriptionManager.Unsubscribe<PropertyChangedEventArgs>(
                        inpc,
                        nameof(inpc.PropertyChanged),
                        this);
                }
            }

            public bool SetValue(object value, BindingPriority priority)
            {
                if (_property.CanWrite)
                {
                    _property.SetValue(_reference.Target, value);
                    return true;
                }

                return false;
            }

            void IWeakSubscriber<PropertyChangedEventArgs>.OnEvent(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == _property.Name || string.IsNullOrEmpty(e.PropertyName))
                {
                    _changed(Value);
                }
            }
        }
    }
}
