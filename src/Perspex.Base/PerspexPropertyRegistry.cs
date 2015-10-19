// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Perspex
{
    /// <summary>
    /// Tracks registered <see cref="PerspexProperty"/> instances.
    /// </summary>
    public class PerspexPropertyRegistry
    {
        /// <summary>
        /// The registered properties by type.
        /// </summary>
        private readonly Dictionary<Type, List<PerspexProperty>> _registered =
            new Dictionary<Type, List<PerspexProperty>>();

        /// <summary>
        /// The registered attached properties by owner type.
        /// </summary>
        private readonly Dictionary<Type, List<PerspexProperty>> _attached =
            new Dictionary<Type, List<PerspexProperty>>();

        /// <summary>
        /// Gets the <see cref="PerspexPropertyRegistry"/> instance
        /// </summary>
        public static PerspexPropertyRegistry Instance { get; }
            = new PerspexPropertyRegistry();

        /// <summary>
        /// Gets all attached <see cref="PerspexProperty"/>s registered by an owner.
        /// </summary>
        /// <param name="ownerType">The owner type.</param>
        /// <returns>A collection of <see cref="PerspexProperty"/> definitions.</returns>
        public IEnumerable<PerspexProperty> GetAttached(Type ownerType)
        {
            List<PerspexProperty> list;

            if (_attached.TryGetValue(ownerType, out list))
            {
                return list;
            }

            return Enumerable.Empty<PerspexProperty>();
        }

        /// <summary>
        /// Gets all <see cref="PerspexProperty"/>s registered on a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>A collection of <see cref="PerspexProperty"/> definitions.</returns>
        public IEnumerable<PerspexProperty> GetRegistered(Type type)
        {
            Contract.Requires<ArgumentNullException>(type != null);

            var i = type.GetTypeInfo();

            while (type != null)
            {
                List<PerspexProperty> list;

                if (_registered.TryGetValue(type, out list))
                {
                    foreach (PerspexProperty p in list)
                    {
                        yield return p;
                    }
                }

                type = type.GetTypeInfo().BaseType;
            }
        }

        /// <summary>
        /// Gets all <see cref="PerspexProperty"/>s registered on a object.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <returns>A collection of <see cref="PerspexProperty"/> definitions.</returns>
        public IEnumerable<PerspexProperty> GetRegistered(PerspexObject o)
        {
            Contract.Requires<ArgumentNullException>(o != null);

            return GetRegistered(o.GetType());
        }

        /// <summary>
        /// Finds <see cref="PerspexProperty"/> registered on a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="property">The property.</param>
        /// <returns>The registered property or null if not found.</returns>
        /// <remarks>
        /// Calling AddOwner on a PerspexProperty creates a new PerspexProperty that is a 
        /// different object but is equal according to <see cref="object.Equals(object)"/>.
        /// </remarks>
        public PerspexProperty FindRegistered(Type type, PerspexProperty property)
        {
            while (type != null)
            {
                List<PerspexProperty> list;

                if (_registered.TryGetValue(type, out list))
                {
                    var index = list.IndexOf(property);

                    if (index != -1)
                    {
                        return list[index];
                    }
                }

                type = type.GetTypeInfo().BaseType;
            }

            return null;
        }

        /// <summary>
        /// Finds <see cref="PerspexProperty"/> registered on an object.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="property">The property.</param>
        /// <returns>The registered property or null if not found.</returns>
        /// <remarks>
        /// Calling AddOwner on a PerspexProperty creates a new PerspexProperty that is a 
        /// different object but is equal according to <see cref="object.Equals(object)"/>.
        /// </remarks>
        public PerspexProperty FindRegistered(object o, PerspexProperty property)
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
        public PerspexProperty FindRegistered(Type type, string name)
        {
            Contract.Requires<ArgumentNullException>(type != null);
            Contract.Requires<ArgumentNullException>(name != null);

            var parts = name.Split('.');

            if (parts.Length < 1 || parts.Length > 2)
            {
                throw new ArgumentException("Invalid property name.");
            }

            string propertyName;
            var results = GetRegistered(type);

            if (parts.Length == 1)
            {
                propertyName = parts[0];
            }
            else
            {
                var types = GetImplementedTypes(type);

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
        public PerspexProperty FindRegistered(PerspexObject o, string name)
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
        /// Checks whether a <see cref="PerspexProperty"/> is registered on a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="property">The property.</param>
        /// <returns>True if the property is registered, otherwise false.</returns>
        public bool IsRegistered(Type type, PerspexProperty property)
        {
            return FindRegistered(type, property) != null;
        }

        /// <summary>
        /// Checks whether a <see cref="PerspexProperty"/> is registered on a object.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="property">The property.</param>
        /// <returns>True if the property is registered, otherwise false.</returns>
        public bool IsRegistered(object o, PerspexProperty property)
        {
            return IsRegistered(o.GetType(), property);
        }

        /// <summary>
        /// Registers a <see cref="PerspexProperty"/> on a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="property">The property.</param>
        /// <remarks>
        /// You won't usually want to call this method directly, instead use the
        /// <see cref="PerspexProperty.Register"/> method.
        /// </remarks>
        public void Register(Type type, PerspexProperty property)
        {
            Contract.Requires<ArgumentNullException>(type != null);
            Contract.Requires<ArgumentNullException>(property != null);

            List<PerspexProperty> list;

            if (!_registered.TryGetValue(type, out list))
            {
                list = new List<PerspexProperty>();
                _registered.Add(type, list);
            }

            if (!list.Contains(property))
            {
                list.Add(property);
            }

            if (property.IsAttached)
            {
                if (!_attached.TryGetValue(property.OwnerType, out list))
                {
                    list = new List<PerspexProperty>();
                    _attached.Add(property.OwnerType, list);
                }

                if (!list.Contains(property))
                {
                    list.Add(property);
                }
            }
        }
    }
}
