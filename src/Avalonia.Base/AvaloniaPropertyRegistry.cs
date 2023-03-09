using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

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
        private readonly Dictionary<Type, Dictionary<int, AvaloniaProperty>> _direct =
            new Dictionary<Type, Dictionary<int, AvaloniaProperty>>();
        private readonly Dictionary<Type, List<AvaloniaProperty>> _registeredCache =
            new Dictionary<Type, List<AvaloniaProperty>>();
        private readonly Dictionary<Type, List<AvaloniaProperty>> _attachedCache =
            new Dictionary<Type, List<AvaloniaProperty>>();
        private readonly Dictionary<Type, List<AvaloniaProperty>> _directCache =
            new Dictionary<Type, List<AvaloniaProperty>>();
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
        [UnconditionalSuppressMessage("Trimming", "IL2059", Justification = "If type was trimmed out, no properties were referenced")]
        public IReadOnlyList<AvaloniaProperty> GetRegistered(Type type)
        {
            _ = type ?? throw new ArgumentNullException(nameof(type));

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
        public IReadOnlyList<AvaloniaProperty> GetRegisteredAttached(Type type)
        {
            _ = type ?? throw new ArgumentNullException(nameof(type));

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
        /// Gets all direct <see cref="AvaloniaProperty"/>s registered on a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>A collection of <see cref="AvaloniaProperty"/> definitions.</returns>
        public IReadOnlyList<AvaloniaProperty> GetRegisteredDirect(Type type)
        {
            _ = type ?? throw new ArgumentNullException(nameof(type));

            if (_directCache.TryGetValue(type, out var result))
            {
                return result;
            }

            var t = type;
            result = new List<AvaloniaProperty>();

            while (t != null)
            {
                if (_direct.TryGetValue(t, out var direct))
                {
                    result.AddRange(direct.Values);
                }

                t = t.BaseType;
            }

            _directCache.Add(type, result);
            return result;
        }

        /// <summary>
        /// Gets all inherited <see cref="AvaloniaProperty"/>s registered on a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>A collection of <see cref="AvaloniaProperty"/> definitions.</returns>
        public IReadOnlyList<AvaloniaProperty> GetRegisteredInherited(Type type)
        {
            _ = type ?? throw new ArgumentNullException(nameof(type));

            if (_inheritedCache.TryGetValue(type, out var result))
            {
                return result;
            }

            result = new List<AvaloniaProperty>();
            var visited = new HashSet<AvaloniaProperty>();

            var registered = GetRegistered(type);
            var registeredCount = registered.Count;

            for (var i = 0; i < registeredCount; i++)
            {
                var property = registered[i];

                if (property.Inherits)
                {
                    result.Add(property);
                    visited.Add(property);
                }
            }

            var registeredAttached = GetRegisteredAttached(type);
            var registeredAttachedCount = registeredAttached.Count;

            for (var i = 0; i < registeredAttachedCount; i++)
            {
                var property = registeredAttached[i];

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
        public IReadOnlyList<AvaloniaProperty> GetRegistered(AvaloniaObject o)
        {
            _ = o ?? throw new ArgumentNullException(nameof(o));

            return GetRegistered(o.GetType());
        }

        /// <summary>
        /// Gets a direct property as registered on an object.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="property">The direct property.</param>
        /// <returns>
        /// The registered.
        /// </returns>
        public DirectPropertyBase<T> GetRegisteredDirect<T>(
            AvaloniaObject o,
            DirectPropertyBase<T> property)
        {
            return FindRegisteredDirect(o, property) ??
                throw new ArgumentException($"Property '{property.Name} not registered on '{o.GetType()}");
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
        public AvaloniaProperty? FindRegistered(Type type, string name)
        {
            _ = type ?? throw new ArgumentNullException(nameof(type));
            _ = name ?? throw new ArgumentNullException(nameof(name));

            if (name.Contains('.'))
            {
                throw new InvalidOperationException("Attached properties not supported.");
            }

            var registered = GetRegistered(type);
            var registeredCount = registered.Count;

            for (var i = 0; i < registeredCount; i++)
            {
                AvaloniaProperty x = registered[i];

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
        public AvaloniaProperty? FindRegistered(AvaloniaObject o, string name)
        {
            _ = o ?? throw new ArgumentNullException(nameof(o));
            _ = name ?? throw new ArgumentNullException(nameof(name));

            return FindRegistered(o.GetType(), name);
        }

        /// <summary>
        /// Finds a direct property as registered on an object.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="property">The direct property.</param>
        /// <returns>
        /// The registered property or null if no matching property found.
        /// </returns>
        public DirectPropertyBase<T>? FindRegisteredDirect<T>(
            AvaloniaObject o,
            DirectPropertyBase<T> property)
        {
            if (property.Owner == o.GetType())
            {
                return property;
            }

            var registeredDirect = GetRegisteredDirect(o.GetType());
            var registeredDirectCount = registeredDirect.Count;

            for (var i = 0; i < registeredDirectCount; i++)
            {
                var p = registeredDirect[i];

                if (p == property)
                {
                    return (DirectPropertyBase<T>)p;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds a registered property by Id.
        /// </summary>
        /// <param name="id">The property Id.</param>
        /// <returns>The registered property or null if no matching property found.</returns>
        internal AvaloniaProperty? FindRegistered(int id)
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
            _ = type ?? throw new ArgumentNullException(nameof(type));
            _ = property ?? throw new ArgumentNullException(nameof(property));

            static bool ContainsProperty(IReadOnlyList<AvaloniaProperty> properties, AvaloniaProperty property)
            {
                var propertiesCount = properties.Count;

                for (var i = 0; i < propertiesCount; i++)
                {
                    if (properties[i] == property)
                    {
                        return true;
                    }
                }

                return false;
            }

            return ContainsProperty(Instance.GetRegistered(type), property) ||
                   ContainsProperty(Instance.GetRegisteredAttached(type), property);
        }

        /// <summary>
        /// Checks whether a <see cref="AvaloniaProperty"/> is registered on a object.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="property">The property.</param>
        /// <returns>True if the property is registered, otherwise false.</returns>
        public bool IsRegistered(object o, AvaloniaProperty property)
        {
            _ = o ?? throw new ArgumentNullException(nameof(o));
            _ = property ?? throw new ArgumentNullException(nameof(property));

            return IsRegistered(o.GetType(), property);
        }

        /// <summary>
        /// Registers a <see cref="AvaloniaProperty"/> on a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="property">The property.</param>
        /// <remarks>
        /// You won't usually want to call this method directly, instead use the
        /// <see cref="AvaloniaProperty.Register{TOwner, TValue}(string, TValue, bool, Data.BindingMode, Func{TValue, bool}, Func{AvaloniaObject, TValue, TValue}, bool)"/>
        /// method.
        /// </remarks>
        public void Register(Type type, AvaloniaProperty property)
        {
            _ = type ?? throw new ArgumentNullException(nameof(type));
            _ = property ?? throw new ArgumentNullException(nameof(property));

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

            if (property.IsDirect)
            {
                if (!_direct.TryGetValue(type, out inner))
                {
                    inner = new Dictionary<int, AvaloniaProperty>();
                    inner.Add(property.Id, property);
                    _direct.Add(type, inner);
                }
                else if (!inner.ContainsKey(property.Id))
                {
                    inner.Add(property.Id, property);
                }

                _directCache.Clear();
            }

            if (!_properties.ContainsKey(property.Id))
            {
                _properties.Add(property.Id, property);
            }
            
            _registeredCache.Clear();
            _inheritedCache.Clear();
        }

        /// <summary>
        /// Registers an attached <see cref="AvaloniaProperty"/> on a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="property">The property.</param>
        /// <remarks>
        /// You won't usually want to call this method directly, instead use the
        /// <see cref="AvaloniaProperty.RegisterAttached{THost, TValue}(string, Type, TValue, bool, Data.BindingMode, Func{TValue, bool}, Func{AvaloniaObject, TValue, TValue})"/>
        /// method.
        /// </remarks>
        public void RegisterAttached(Type type, AvaloniaProperty property)
        {
            _ = type ?? throw new ArgumentNullException(nameof(type));
            _ = property ?? throw new ArgumentNullException(nameof(property));

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
            _inheritedCache.Clear();
        }
    }
}
