// -----------------------------------------------------------------------
// <copyright file="PerspexProperty.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// A perspex property.
    /// </summary>
    /// <remarks>
    /// This class is analogous to DependencyProperty in WPF.
    /// </remarks>
    public class PerspexProperty
    {
        /// <summary>
        /// The default values for the property, by type.
        /// </summary>
        private Dictionary<Type, object> defaultValues = new Dictionary<Type, object>();

        /// <summary>
        /// Initializes a new instance of the <see cref="PerspexProperty"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="valueType">The type of the property's value.</param>
        /// <param name="ownerType">The type of the class that registers the property.</param>
        /// <param name="defaultValue">The default value of the property.</param>
        /// <param name="inherits">Whether the property inherits its value.</param>
        public PerspexProperty(
            string name,
            Type valueType,
            Type ownerType,
            object defaultValue,
            bool inherits)
        {
            Contract.Requires<NullReferenceException>(name != null);
            Contract.Requires<NullReferenceException>(valueType != null);
            Contract.Requires<NullReferenceException>(ownerType != null);

            this.Name = name;
            this.PropertyType = valueType;
            this.OwnerType = ownerType;
            this.Inherits = inherits;
            this.defaultValues.Add(ownerType, defaultValue);
        }

        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the type of the property's value.
        /// </summary>
        public Type PropertyType { get; private set; }

        /// <summary>
        /// Gets the type of the class that registers the property.
        /// </summary>
        public Type OwnerType { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the property inherits its value.
        /// </summary>
        public bool Inherits { get; private set; }

        /// <summary>
        /// Registers a <see cref="PerspexProperty"/>.
        /// </summary>
        /// <typeparam name="TOwner">The type of the class that is registering the property.</typeparam>
        /// <typeparam name="TValue">The type of the property's value.</typeparam>
        /// <param name="name">The name of the property.</param>
        /// <param name="defaultValue">The default value of the property.</param>
        /// <param name="inherits">Whether the property inherits its value.</param>
        /// <returns></returns>
        public static PerspexProperty<TValue> Register<TOwner, TValue>(
            string name,
            TValue defaultValue = default(TValue),
            bool inherits = false)
            where TOwner : PerspexObject
        {
            Contract.Requires<NullReferenceException>(name != null);

            PerspexProperty<TValue> result = new PerspexProperty<TValue>(
                name,
                typeof(TOwner),
                defaultValue,
                inherits);

            PerspexObject.Register(typeof(TOwner), result);

            return result;
        }

        /// <summary>
        /// Gets the default value for the property on the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The default value.</returns>
        public object GetDefaultValue(Type type)
        {
            Contract.Requires<NullReferenceException>(type != null);

            while (type != null)
            {
                object result;

                if (this.defaultValues.TryGetValue(type, out result))
                {
                    return result;
                }

                type = type.GetTypeInfo().BaseType;
            }

            return this.defaultValues[this.OwnerType];
        }

        public bool IsValidType(object value)
        {
            if (value == null)
            {
                return !this.PropertyType.GetTypeInfo().IsValueType ||
                    Nullable.GetUnderlyingType(this.PropertyType) != null;
            }

            return this.PropertyType.GetTypeInfo().IsAssignableFrom(value.GetType().GetTypeInfo());
        }

        /// <summary>
        /// Gets the default value for the property on the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="defaultValue">The default value.</param>
        public void OverrideDefaultValue(Type type, object defaultValue)
        {
            Contract.Requires<NullReferenceException>(type != null);

            // TODO: Ensure correct type.

            if (this.defaultValues.ContainsKey(type))
            {
                throw new InvalidOperationException("Default value is already set for this property.");
            }

            this.defaultValues.Add(type, defaultValue);
        }
    }

    /// <summary>
    /// A typed perspex property.
    /// </summary>
    public class PerspexProperty<TValue> : PerspexProperty
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PerspexProperty"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="ownerType">The type of the class that registers the property.</param>
        /// <param name="defaultValue">The default value of the property.</param>
        /// <param name="inherits">Whether the property inherits its value.</param>
        public PerspexProperty(
            string name,
            Type ownerType,
            TValue defaultValue,
            bool inherits)
            : base(name, typeof(TValue), ownerType, defaultValue, inherits)
        {
            Contract.Requires<NullReferenceException>(name != null);
            Contract.Requires<NullReferenceException>(ownerType != null);
        }

        /// <summary>
        /// Registers the property on another type.
        /// </summary>
        /// <typeparam name="TOwner">The type of the additional owner.</typeparam>
        /// <returns>The property.</returns>
        public PerspexProperty<TValue> AddOwner<TOwner>()
        {
            PerspexObject.Register(typeof(TOwner), this);
            return this;
        }

        /// <summary>
        /// Gets the default value for the property on the specified type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <returns>The default value.</returns>
        public TValue GetDefaultValue<T>()
        {
            return (TValue)this.GetDefaultValue(typeof(T));
        }
    }
}
