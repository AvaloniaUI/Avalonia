// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using Avalonia.Data;
using Avalonia.Logging;

namespace Avalonia.Markup.Data.Plugins
{
    /// <summary>
    /// Reads a property from a <see cref="AvaloniaObject"/>.
    /// </summary>
    public class AvaloniaPropertyAccessorPlugin : IPropertyAccessorPlugin
    {
        /// <summary>
        /// Checks whether this plugin can handle accessing the properties of the specified object.
        /// </summary>
        /// <param name="reference">A weak reference to the object.</param>
        /// <returns>True if the plugin can handle the object; otherwise false.</returns>
        public bool Match(WeakReference reference)
        {
            Contract.Requires<ArgumentNullException>(reference != null);

            return reference.Target is AvaloniaObject;
        }

        /// <summary>
        /// Starts monitoring the value of a property on an object.
        /// </summary>
        /// <param name="reference">A weak reference to the object.</param>
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
            var o = (AvaloniaObject)instance;
            var p = AvaloniaPropertyRegistry.Instance.FindRegistered(o, propertyName);

            if (p != null)
            {
                return new Accessor(new WeakReference<AvaloniaObject>(o), p, changed);
            }
            else if (instance != AvaloniaProperty.UnsetValue)
            {
                var message = $"Could not find AvaloniaProperty '{propertyName}' on '{instance}'";
                var exception = new MissingMemberException(message);
                return new PropertyError(new BindingError(exception));
            }
            else
            {
                return null;
            }
        }

        private class Accessor : IPropertyAccessor
        {
            private readonly WeakReference<AvaloniaObject> _reference;
            private readonly AvaloniaProperty _property;
            private IDisposable _subscription;

            public Accessor(
                WeakReference<AvaloniaObject> reference, 
                AvaloniaProperty property, 
                Action<object> changed)
            {
                Contract.Requires<ArgumentNullException>(reference != null);
                Contract.Requires<ArgumentNullException>(property != null);

                _reference = reference;
                _property = property;
                _subscription = Instance.GetWeakObservable(property).Skip(1).Subscribe(changed);
            }

            public AvaloniaObject Instance
            {
                get
                {
                    AvaloniaObject result;
                    _reference.TryGetTarget(out result);
                    return result;
                }
            }

            public Type PropertyType => _property.PropertyType;

            public object Value => Instance.GetValue(_property);

            public void Dispose()
            {
                _subscription?.Dispose();
                _subscription = null;
            }

            public bool SetValue(object value, BindingPriority priority)
            {
                if (!_property.IsReadOnly)
                {
                    Instance.SetValue(_property, value, priority);
                    return true;
                }

                return false;
            }
        }
    }
}
