// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Avalonia
{
    /// <summary>
    /// Tracks registered <see cref="AvaloniaProperty"/> instances.
    /// </summary>
    public class AvaloniaPropertyRegistry
    {
        /// <summary>
        /// The registered properties by type.
        /// </summary>
        private readonly Dictionary<Type, Dictionary<int, AvaloniaProperty>> _registered =
            new Dictionary<Type, Dictionary<int, AvaloniaProperty>>();

        /// <summary>
        /// The registered properties by type cached values to increase performance.
        /// </summary>
        private readonly Dictionary<Type, Dictionary<int, AvaloniaProperty>> _registeredCache =
            new Dictionary<Type, Dictionary<int, AvaloniaProperty>>();

        /// <summary>
        /// The registered attached properties by owner type.
        /// </summary>
        private readonly Dictionary<Type, Dictionary<int, AvaloniaProperty>> _attached =
            new Dictionary<Type, Dictionary<int, AvaloniaProperty>>();

        /// <summary>
        /// Gets the <see cref="AvaloniaPropertyRegistry"/> instance
        /// </summary>
        public static AvaloniaPropertyRegistry Instance { get; }
            = new AvaloniaPropertyRegistry();

        /// <summary>
        /// Gets all attached <see cref="AvaloniaProperty"/>s registered by an owner.
        /// </summary>
        /// <param name="ownerType">The owner type.</param>
        /// <returns>A collection of <see cref="AvaloniaProperty"/> definitions.</returns>
        public IEnumerable<AvaloniaProperty> GetAttached(Type ownerType)
        {
            Dictionary<int, AvaloniaProperty> inner;

            // Ensure the type's static ctor has been run.
            RuntimeHelpers.RunClassConstructor(ownerType.TypeHandle);

            if (_attached.TryGetValue(ownerType, out inner))
            {
                return inner.Values;
            }

            return Enumerable.Empty<AvaloniaProperty>();
        }

        /// <summary>
        /// Gets all <see cref="AvaloniaProperty"/>s registered on a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>A collection of <see cref="AvaloniaProperty"/> definitions.</returns>
        public IEnumerable<AvaloniaProperty> GetRegistered(Type type)
        {
            Contract.Requires<ArgumentNullException>(type != null);

            while (type != null)
            {
                // Ensure the type's static ctor has been run.
                RuntimeHelpers.RunClassConstructor(type.TypeHandle);

                Dictionary<int, AvaloniaProperty> inner;

                if (_registered.TryGetValue(type, out inner))
                {
                    foreach (var p in inner)
                    {
                        yield return p.Value;
                    }
                }

                type = type.GetTypeInfo().BaseType;
            }
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
        /// Finds a <see cref="AvaloniaProperty"/> registered on a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="property">The property.</param>
        /// <returns>The registered property or null if not found.</returns>
        /// <remarks>
        /// Calling AddOwner on a AvaloniaProperty creates a new AvaloniaProperty that is a 
        /// different object but is equal according to <see cref="object.Equals(object)"/>.
        /// </remarks>
        public AvaloniaProperty FindRegistered(Type type, AvaloniaProperty property)
        {
            Type currentType = type;
            Dictionary<int, AvaloniaProperty> cache;
            AvaloniaProperty result;

            if (_registeredCache.TryGetValue(type, out cache))
            {
                if (cache.TryGetValue(property.Id, out result))
                {
                    return result;
                }
            }

            while (currentType != null)
            {
                Dictionary<int, AvaloniaProperty> inner;

                if (_registered.TryGetValue(currentType, out inner))
                {
                    if (inner.TryGetValue(property.Id, out result))
                    {
                        if (cache == null)
                        {
                            _registeredCache[type] = cache = new Dictionary<int, AvaloniaProperty>();
                        }

                        cache[property.Id] = result;

                        return result;
                    }
                }

                currentType = currentType.GetTypeInfo().BaseType;
            }

            return null;
        }

        /// <summary>
        /// Finds <see cref="AvaloniaProperty"/> registered on an object.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="property">The property.</param>
        /// <returns>The registered property or null if not found.</returns>
        /// <remarks>
        /// Calling AddOwner on a AvaloniaProperty creates a new AvaloniaProperty that is a
        /// different object but is equal according to <see cref="object.Equals(object)"/>.
        /// </remarks>
        public AvaloniaProperty FindRegistered(object o, AvaloniaProperty property)
        {
            return FindRegistered(o.GetType(), property);
        }

        /// <summary>
        /// Finds a registered property on a type by name.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="name">
        /// The property name. If an attached property it should be in the form
        /// "OwnerType.PropertyName".
        /// </param>
        /// <returns>
        /// The registered property or null if no matching property found.
        /// </returns>
        public AvaloniaProperty FindRegistered(Type type, string name)
        {
            Contract.Requires<ArgumentNullException>(type != null);
            Contract.Requires<ArgumentNullException>(name != null);

            var parts = name.Split('.');
            var types = GetImplementedTypes(type).ToList();

            if (parts.Length < 1 || parts.Length > 2)
            {
                throw new ArgumentException("Invalid property name.");
            }

            string propertyName;
            var results = GetRegistered(type);

            if (parts.Length == 1)
            {
                propertyName = parts[0];
                results = results.Where(x => !x.IsAttached || types.Contains(x.OwnerType.Name));
            }
            else
            {
                if (!types.Contains(parts[0]))
                {
                    results = results.Where(x => x.OwnerType.Name == parts[0]);
                }

                propertyName = parts[1];
            }

            return results.FirstOrDefault(x => x.Name == propertyName);
        }

        /// <summary>
        /// Finds a registered property on an object by name.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="name">
        /// The property name. If an attached property it should be in the form
        /// "OwnerType.PropertyName".
        /// </param>
        /// <returns>
        /// The registered property or null if no matching property found.
        /// </returns>
        public AvaloniaProperty FindRegistered(AvaloniaObject o, string name)
        {
            return FindRegistered(o.GetType(), name);
        }

        /// <summary>
        /// Returns a type and all its base types.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The type and all its base types.</returns>
        private IEnumerable<string> GetImplementedTypes(Type type)
        {
            while (type != null)
            {
                yield return type.Name;
                type = type.GetTypeInfo().BaseType;
            }
        }

        /// <summary>
        /// Checks whether a <see cref="AvaloniaProperty"/> is registered on a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="property">The property.</param>
        /// <returns>True if the property is registered, otherwise false.</returns>
        public bool IsRegistered(Type type, AvaloniaProperty property)
        {
            return FindRegistered(type, property) != null;
        }

        /// <summary>
        /// Checks whether a <see cref="AvaloniaProperty"/> is registered on a object.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="property">The property.</param>
        /// <returns>True if the property is registered, otherwise false.</returns>
        public bool IsRegistered(object o, AvaloniaProperty property)
        {
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

            Dictionary<int, AvaloniaProperty> inner;

            if (!_registered.TryGetValue(type, out inner))
            {
                inner = new Dictionary<int, AvaloniaProperty>();
                _registered.Add(type, inner);
            }

            if (!inner.ContainsKey(property.Id))
            {
                inner.Add(property.Id, property);
            }

            if (property.IsAttached)
            {
                if (!_attached.TryGetValue(property.OwnerType, out inner))
                {
                    inner = new Dictionary<int, AvaloniaProperty>();
                    _attached.Add(property.OwnerType, inner);
                }

                if (!inner.ContainsKey(property.Id))
                {
                    inner.Add(property.Id, property);
                }
            }

            _registeredCache.Clear();
        }
    }
}