// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Data;

namespace Avalonia.Data.Core.Plugins
{
    /// <summary>
    /// Reads a property from a <see cref="AvaloniaObject"/>.
    /// </summary>
    public class AvaloniaPropertyAccessorPlugin : IPropertyAccessorPlugin
    {
        /// <inheritdoc/>
        public bool Match(object obj, string propertyName)
        {
            if (obj is AvaloniaObject o)
            {
                return LookupProperty(o, propertyName) != null;
            }

            return false;
        }

        /// <summary>
        /// Starts monitoring the value of a property on an object.
        /// </summary>
        /// <param name="reference">A weak reference to the object.</param>
        /// <param name="propertyName">The property name.</param>
        /// <returns>
        /// An <see cref="IPropertyAccessor"/> interface through which future interactions with the 
        /// property will be made.
        /// </returns>
        public IPropertyAccessor Start(WeakReference reference, string propertyName)
        {
            Contract.Requires<ArgumentNullException>(reference != null);
            Contract.Requires<ArgumentNullException>(propertyName != null);

            var instance = reference.Target;
            var o = (AvaloniaObject)instance;
            var p = LookupProperty(o, propertyName);

            if (p != null)
            {
                return new Accessor(new WeakReference<AvaloniaObject>(o), p);
            }
            else if (instance != AvaloniaProperty.UnsetValue)
            {
                var message = $"Could not find AvaloniaProperty '{propertyName}' on '{instance}'";
                var exception = new MissingMemberException(message);
                return new PropertyError(new BindingNotification(exception, BindingErrorType.Error));
            }
            else
            {
                return null;
            }
        }

        private static AvaloniaProperty LookupProperty(AvaloniaObject o, string propertyName)
        {
            if (!propertyName.Contains("."))
            {
                return AvaloniaPropertyRegistry.Instance.FindRegistered(o, propertyName);
            }
            else
            {
                var split = propertyName.Split('.');

                if (split.Length == 2)
                {
                    // HACK: We need a way to resolve types here using something like IXamlTypeResolver.
                    // We don't currently have that so we have to make our best guess.
                    var type = split[0];
                    var name = split[1];
                    var registry = AvaloniaPropertyRegistry.Instance;
                    var registered = registry.GetRegisteredAttached(o.GetType())
                        .Concat(registry.GetRegistered(o.GetType()));

                    foreach (var p in registered)
                    {
                        if (p.Name == name && IsOfType(p.OwnerType, type))
                        {
                            return p;
                        }
                    }
                }
            }

            return null;
        }

        private static bool IsOfType(Type type, string typeName)
        {
            while (type != null)
            {
                if (type.Name == typeName)
                {
                    return true;
                }

                type = type.BaseType;
            }

            return false;
        }

        private class Accessor : PropertyAccessorBase
        {
            private readonly WeakReference<AvaloniaObject> _reference;
            private readonly AvaloniaProperty _property;
            private IDisposable _subscription;

            public Accessor(WeakReference<AvaloniaObject> reference, AvaloniaProperty property)
            {
                Contract.Requires<ArgumentNullException>(reference != null);
                Contract.Requires<ArgumentNullException>(property != null);

                _reference = reference;
                _property = property;
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

            public override Type PropertyType => _property.PropertyType;
            public override object Value => Instance?.GetValue(_property);

            public override bool SetValue(object value, BindingPriority priority)
            {
                if (!_property.IsReadOnly)
                {
                    Instance.SetValue(_property, value, priority);
                    return true;
                }

                return false;
            }

            protected override void Dispose(bool disposing)
            {
                _subscription?.Dispose();
                _subscription = null;
            }

            protected override void SubscribeCore(IObserver<object> observer)
            {
                _subscription = Instance?.GetObservable(_property).Subscribe(observer);
            }
        }
    }
}
