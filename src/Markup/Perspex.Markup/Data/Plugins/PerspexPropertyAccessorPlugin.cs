// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reflection;

namespace Perspex.Markup.Data.Plugins
{
    /// <summary>
    /// Reads a property from a <see cref="PerspexObject"/>.
    /// </summary>
    public class PerspexPropertyAccessorPlugin : IPropertyAccessorPlugin
    {
        /// <summary>
        /// Checks whether this plugin can handle accessing the properties of the specified object.
        /// </summary>
        /// <param name="instance">The object.</param>
        /// <returns>True if the plugin can handle the object; otherwise false.</returns>
        public bool Match(object instance)
        {
            Contract.Requires<ArgumentNullException>(instance != null);

            return instance is PerspexObject;
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

            var o = (PerspexObject)instance;
            var p = PerspexPropertyRegistry.Instance.FindRegistered(o, propertyName);

            if (p != null)
            {
                return new Accessor(o, p, changed);
            }
            else
            {
                return null;
            }
        }

        private class Accessor : IPropertyAccessor
        {
            private readonly PerspexObject _instance;
            private readonly PerspexProperty _property;
            private IDisposable _subscription;

            public Accessor(PerspexObject instance, PerspexProperty property, Action<object> changed)
            {
                Contract.Requires<ArgumentNullException>(instance != null);
                Contract.Requires<ArgumentNullException>(property != null);

                _instance = instance;
                _property = property;
                _subscription = instance.GetObservable(property).Skip(1).Subscribe(changed);
            }

            public Type PropertyType => _property.PropertyType;

            public object Value => _instance.GetValue(_property);

            public void Dispose()
            {
                _subscription?.Dispose();
                _subscription = null;
            }

            public bool SetValue(object value)
            {
                if (!_property.IsReadOnly)
                {
                    _instance.SetValue(_property, value);
                    return true;
                }

                return false;
            }
        }
    }
}
