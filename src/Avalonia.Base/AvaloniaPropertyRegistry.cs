// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Data;

namespace Avalonia
{
    /// <summary>
    /// Tracks registered <see cref="AvaloniaProperty"/> instances.
    /// </summary>
    public class AvaloniaPropertyRegistry
    {
        private readonly Dictionary<int, AvaloniaProperty> _properties =
            new Dictionary<int, AvaloniaProperty>();
        private readonly Dictionary<Type, Dictionary<int, AvaloniaProperty>> _registered =
            new Dictionary<Type, Dictionary<int, AvaloniaProperty>>();
        private readonly Dictionary<Type, Dictionary<int, AvaloniaProperty>> _attached =
            new Dictionary<Type, Dictionary<int, AvaloniaProperty>>();
        private readonly Dictionary<Type, List<AvaloniaProperty>> _registeredCache =
            new Dictionary<Type, List<AvaloniaProperty>>();
        private readonly Dictionary<Type, List<AvaloniaProperty>> _attachedCache =
            new Dictionary<Type, List<AvaloniaProperty>>();
        private readonly Dictionary<Type, List<PropertyInitializationData>> _initializedCache =
            new Dictionary<Type, List<PropertyInitializationData>>();
        private readonly Dictionary<Type, List<AvaloniaProperty>> _inheritedCache =
            new Dictionary<Type, List<AvaloniaProperty>>();

        /// <summary>
        /// Gets the <see cref="AvaloniaPropertyRegistry"/> instance
        /// </summary>
        public static AvaloniaPropertyRegistry Instance { get; }
            = new AvaloniaPropertyRegistry();

        /// <summary>
        /// Gets a list of all registered properties.
        /// </summary>
        internal IReadOnlyCollection<AvaloniaProperty> Properties => _properties.Values;

        /// <summary>
        /// Gets all non-attached <see cref="AvaloniaProperty"/>s registered on a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>A collection of <see cref="AvaloniaProperty"/> definitions.</returns>
        public IEnumerable<AvaloniaProperty> GetRegistered(Type type)
        {
            Contract.Requires<ArgumentNullException>(type != null);

            if (_registeredCache.TryGetValue(type, out var result))
            {
                return result;
            }

            var t = type;
            result = new List<AvaloniaProperty>();

            while (t != null)
            {
                // Ensure the type's static ctor has been run.
                RuntimeHelpers.RunClassConstructor(t.TypeHandle);

                if (_registered.TryGetValue(t, out var registered))
                {
                    result.AddRange(registered.Values);
                }

                t = t.BaseType;
            }

            _registeredCache.Add(type, result);
            return result;
        }

        /// <summary>
        /// Gets all attached <see cref="AvaloniaProperty"/>s registered on a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>A collection of <see cref="AvaloniaProperty"/> definitions.</returns>
        public IEnumerable<AvaloniaProperty> GetRegisteredAttached(Type type)
        {
            Contract.Requires<ArgumentNullException>(type != null);

            if (_attachedCache.TryGetValue(type, out var result))
            {
                return result;
            }

            var t = type;
            result = new List<AvaloniaProperty>();

            while (t != null)
            {
                if (_attached.TryGetValue(t, out var attached))
                {
                    result.AddRange(attached.Values);
                }

                t = t.BaseType;
            }

            _attachedCache.Add(type, result);
            return result;
        }

        /// <summary>
        /// Gets all inherited <see cref="AvaloniaProperty"/>s registered on a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>A collection of <see cref="AvaloniaProperty"/> definitions.</returns>
        public IEnumerable<AvaloniaProperty> GetRegisteredInherited(Type type)
        {
            Contract.Requires<ArgumentNullException>(type != null);

            if (_inheritedCache.TryGetValue(type, out var result))
            {
                return result;
            }

            result = new List<AvaloniaProperty>();
            var visited = new HashSet<AvaloniaProperty>();

            foreach (var property in GetRegistered(type))
            {
                if (property.Inherits)
                {
                    result.Add(property);
                    visited.Add(property);
                }
            }
            foreach (var property in GetRegisteredAttached(type))
            {
                if (property.Inherits)
                {
                    if (!visited.Contains(property))
                    {
                        result.Add(property);
                    }
                }
            }

            _inheritedCache.Add(type, result);
            return result;
        }

        /// <summary>
        /// Gets all <see cref="AvaloniaProperty"/>s registered on a object.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <returns>A collection of <see cref="AvaloniaProperty"/> definitions.</returns>
        public IEnumerable<AvaloniaProperty> GetRegistered(AvaloniaObject o)
        {
            Contract.Requires<ArgumentNullException>(o != null);

            return GetRegistered(o.GetType());
        }

        /// <summary>
        /// Finds a registered property on a type by name.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="name">The property name.</param>
        /// <returns>
        /// The registered property or null if no matching property found.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The property name contains a '.'.
        /// </exception>
        public AvaloniaProperty FindRegistered(Type type, string name)
        {
            Contract.Requires<ArgumentNullException>(type != null);
            Contract.Requires<ArgumentNullException>(name != null);

            if (name.Contains("."))
            {
                throw new InvalidOperationException("Attached properties not supported.");
            }

            foreach (AvaloniaProperty x in GetRegistered(type))
            {
                if (x.Name == name)
                {
                    return x;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds a registered property on an object by name.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="name">The property name.</param>
        /// <returns>
        /// The registered property or null if no matching property found.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The property name contains a '.'.
        /// </exception>
        public AvaloniaProperty FindRegistered(AvaloniaObject o, string name)
        {
            Contract.Requires<ArgumentNullException>(o != null);
            Contract.Requires<ArgumentNullException>(name != null);

            return FindRegistered(o.GetType(), name);
        }

        /// <summary>
        /// Finds a registered property by Id.
        /// </summary>
        /// <param name="id">The property Id.</param>
        /// <returns>The registered property or null if no matching property found.</returns>
        internal AvaloniaProperty FindRegistered(int id)
        {
            return id < _properties.Count ? _properties[id] : null;
        }

        /// <summary>
        /// Checks whether a <see cref="AvaloniaProperty"/> is registered on a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="property">The property.</param>
        /// <returns>True if the property is registered, otherwise false.</returns>
        public bool IsRegistered(Type type, AvaloniaProperty property)
        {
            Contract.Requires<ArgumentNullException>(type != null);
            Contract.Requires<ArgumentNullException>(property != null);

            return Instance.GetRegistered(type).Any(x => x == property) ||
                Instance.GetRegisteredAttached(type).Any(x => x == property);
        }

        /// <summary>
        /// Checks whether a <see cref="AvaloniaProperty"/> is registered on a object.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="property">The property.</param>
        /// <returns>True if the property is registered, otherwise false.</returns>
        public bool IsRegistered(object o, AvaloniaProperty property)
        {
            Contract.Requires<ArgumentNullException>(o != null);
            Contract.Requires<ArgumentNullException>(property != null);

            return IsRegistered(o.GetType(), property);
        }

        /// <summary>
        /// Registers a <see cref="AvaloniaProperty"/> on a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="property">The property.</param>
        /// <remarks>
        /// You won't usually want to call this method directly, instead use the
        /// <see cref="AvaloniaProperty.Register{TOwner, TValue}(string, TValue, bool, Data.BindingMode, Func{TOwner, TValue, TValue}, Action{IAvaloniaObject, bool})"/>
        /// method.
        /// </remarks>
        public void Register(Type type, AvaloniaProperty property)
        {
            Contract.Requires<ArgumentNullException>(type != null);
            Contract.Requires<ArgumentNullException>(property != null);

            if (!_registered.TryGetValue(type, out var inner))
            {
                inner = new Dictionary<int, AvaloniaProperty>();
                inner.Add(property.Id, property);
                _registered.Add(type, inner);
            }
            else if (!inner.ContainsKey(property.Id))
            {
                inner.Add(property.Id, property);
            }

            if (!_properties.ContainsKey(property.Id))
            {
                _properties.Add(property.Id, property);
            }
            
            _registeredCache.Clear();
            _initializedCache.Clear();
            _inheritedCache.Clear();
        }

        /// <summary>
        /// Registers an attached <see cref="AvaloniaProperty"/> on a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="property">The property.</param>
        /// <remarks>
        /// You won't usually want to call this method directly, instead use the
        /// <see cref="AvaloniaProperty.RegisterAttached{THost, TValue}(string, Type, TValue, bool, Data.BindingMode, Func{THost, TValue, TValue})"/>
        /// method.
        /// </remarks>
        public void RegisterAttached(Type type, AvaloniaProperty property)
        {
            Contract.Requires<ArgumentNullException>(type != null);
            Contract.Requires<ArgumentNullException>(property != null);

            if (!property.IsAttached)
            {
                throw new InvalidOperationException(
                    "Cannot register a non-attached property as attached.");
            }

            if (!_attached.TryGetValue(type, out var inner))
            {
                inner = new Dictionary<int, AvaloniaProperty>();
                inner.Add(property.Id, property);
                _attached.Add(type, inner);
            }
            else
            {
                inner.Add(property.Id, property);
            }
            
            _attachedCache.Clear();
            _initializedCache.Clear();
            _inheritedCache.Clear();
        }

        internal void NotifyInitialized(AvaloniaObject o)
        {
            Contract.Requires<ArgumentNullException>(o != null);

            var type = o.GetType();

            void Notify(AvaloniaProperty property, object value)
            {
                var e = new AvaloniaPropertyChangedEventArgs(
                    o,
                    property,
                    AvaloniaProperty.UnsetValue,
                    value,
                    BindingPriority.Unset);

                property.NotifyInitialized(e);
            }

            if (!_initializedCache.TryGetValue(type, out var initializationData))
            {
                var visited = new HashSet<AvaloniaProperty>();

                initializationData = new List<PropertyInitializationData>();

                foreach (AvaloniaProperty property in GetRegistered(type))
                {
                    if (property.IsDirect)
                    {
                        initializationData.Add(new PropertyInitializationData(property, (IDirectPropertyAccessor)property));
                    }
                    else
                    {
                        initializationData.Add(new PropertyInitializationData(property, (IStyledPropertyAccessor)property, type));
                    }

                    visited.Add(property);
                }

                foreach (AvaloniaProperty property in GetRegisteredAttached(type))
                {
                    if (!visited.Contains(property))
                    {
                        initializationData.Add(new PropertyInitializationData(property, (IStyledPropertyAccessor)property, type));

                        visited.Add(property);
                    }
                }

                _initializedCache.Add(type, initializationData);
            }

            foreach (PropertyInitializationData data in initializationData)
            {
                if (!data.Property.HasNotifyInitializedObservers)
                {
                    continue;
                }

                object value = data.IsDirect ? data.DirectAccessor.GetValue(o) : data.Value;

                Notify(data.Property, value);
            }
        }

        private readonly struct PropertyInitializationData
        {
            public AvaloniaProperty Property { get; }
            public object Value { get; }
            public bool IsDirect { get; }
            public IDirectPropertyAccessor DirectAccessor { get; }

            public PropertyInitializationData(AvaloniaProperty property, IDirectPropertyAccessor directAccessor)
            {
                Property = property;
                Value = null;
                IsDirect = true;
                DirectAccessor = directAccessor;
            }

            public PropertyInitializationData(AvaloniaProperty property, IStyledPropertyAccessor styledAccessor, Type type)
            {
                Property = property;
                Value = styledAccessor.GetDefaultValue(type);
                IsDirect = false;
                DirectAccessor = null;
            }
        }
    }
}
