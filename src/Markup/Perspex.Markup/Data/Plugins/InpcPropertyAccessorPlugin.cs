// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reflection;

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
        /// <param name="instance">The object.</param>
        /// <returns>True if the plugin can handle the object; otherwise false.</returns>
        public bool Match(object instance)
        {
            Contract.Requires<ArgumentNullException>(instance != null);

            return true;
        }

        /// <summary>
        /// Starts monitoring the value of a property on an object.
        /// </summary>
        /// <param name="instance">The object.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="changed">A function to call when the property changes.</param>
        /// <returns>
        /// An <see cref="IPropertyAccessor"/> interface through which future interactions with the 
        /// property will be made, or null if the property was not found.
        /// </returns>
        public IPropertyAccessor Start(object instance, string propertyName, Action<object> changed)
        {
            Contract.Requires<ArgumentNullException>(instance != null);
            Contract.Requires<ArgumentNullException>(propertyName != null);
            Contract.Requires<ArgumentNullException>(changed != null);

            var p = instance.GetType().GetRuntimeProperty(propertyName);

            if (p != null)
            {
                return new Accessor(instance, p, changed);
            }
            else
            {
                return null;
            }
        }

        private class Accessor : IPropertyAccessor
        {
            private readonly object _instance;
            private readonly PropertyInfo _property;
            private readonly Action<object> _changed;

            public Accessor(object instance, PropertyInfo property, Action<object> changed)
            {
                Contract.Requires<ArgumentNullException>(instance != null);
                Contract.Requires<ArgumentNullException>(property != null);

                _instance = instance;
                _property = property;
                _changed = changed;

                var inpc = instance as INotifyPropertyChanged;

                if (inpc != null)
                {
                    inpc.PropertyChanged += PropertyChanged;
                }
            }

            public Type PropertyType => _property.PropertyType;

            public object Value => _property.GetValue(_instance);

            public void Dispose()
            {
                var inpc = _instance as INotifyPropertyChanged;

                if (inpc != null)
                {
                    inpc.PropertyChanged -= PropertyChanged;
                }
            }

            public bool SetValue(object value)
            {
                if (_property.CanWrite)
                {
                    _property.SetValue(_instance, value);
                    return true;
                }

                return false;
            }

            private void PropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == _property.Name)
                {
                    _changed(Value);
                }
            }
        }
    }
}
