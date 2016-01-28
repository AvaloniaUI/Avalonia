// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Perspex.Data;

namespace Perspex
{
    /// <summary>
    /// Base class for styled properties.
    /// </summary>
    public class StyledPropertyBase<TValue> : PerspexProperty<TValue>, IStyledPropertyAccessor
    {
        private readonly TValue _defaultValue;
        private readonly Dictionary<Type, TValue> _defaultValues;
        private bool _inherits;
        private readonly Dictionary<Type, Func<IPerspexObject, TValue, TValue>> _validation;

        /// <summary>
        /// Initializes a new instance of the <see cref="StyledPropertyBase{T}"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="ownerType">The type of the class that registers the property.</param>
        /// <param name="defaultValue">The default value of the property.</param>
        /// <param name="inherits">Whether the property inherits its value.</param>
        /// <param name="defaultBindingMode">The default binding mode for the property.</param>
        /// <param name="validate">A validation function.</param>
        /// <param name="notifying">
        /// A method that gets called before and after the property starts being notified on an
        /// object; the bool argument will be true before and false afterwards. This callback is
        /// intended to support IsDataContextChanging.
        /// </param>
        protected StyledPropertyBase(
            string name,
            Type ownerType,
            TValue defaultValue,
            bool inherits = false,
            BindingMode defaultBindingMode = BindingMode.Default,
            Func<IPerspexObject, TValue, TValue> validate = null,
            Action<IPerspexObject, bool> notifying = null)
                : base(name, ownerType, defaultBindingMode, notifying)
        {
            Contract.Requires<ArgumentNullException>(name != null);
            Contract.Requires<ArgumentNullException>(ownerType != null);

            if (name.Contains("."))
            {
                throw new ArgumentException("'name' may not contain periods.");
            }

            _defaultValues = new Dictionary<Type, TValue>();
            _validation = new Dictionary<Type, Func<IPerspexObject, TValue, TValue>>();

            _defaultValue = defaultValue;
            _inherits = inherits;

            if (validate != null)
            {
                _validation.Add(ownerType, validate);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the property inherits its value.
        /// </summary>
        /// <value>
        /// A value indicating whether the property inherits its value.
        /// </value>
        public override bool Inherits => _inherits;

        /// <summary>
        /// Gets the default value for the property on the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The default value.</returns>
        public TValue GetDefaultValue(Type type)
        {
            Contract.Requires<ArgumentNullException>(type != null);

            while (type != null)
            {
                TValue result;

                if (_defaultValues.TryGetValue(type, out result))
                {
                    return result;
                }

                type = type.GetTypeInfo().BaseType;
            }

            return _defaultValue;
        }

        /// <summary>
        /// Gets the validation function for the property on the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// The validation function, or null if no validation function registered for this type.
        /// </returns>
        public Func<IPerspexObject, TValue, TValue> GetValidationFunc(Type type)
        {
            Contract.Requires<ArgumentNullException>(type != null);

            while (type != null)
            {
                Func<IPerspexObject, TValue, TValue> result;

                if (_validation.TryGetValue(type, out result))
                {
                    return result;
                }

                type = type.GetTypeInfo().BaseType;
            }

            return null;
        }

        /// <summary>
        /// Overrides the default value for the property on the specified type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="defaultValue">The default value.</param>
        public void OverrideDefaultValue<T>(TValue defaultValue) where T : IPerspexObject
        {
            OverrideDefaultValue(typeof(T), defaultValue);
        }

        /// <summary>
        /// Overrides the default value for the property on the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="defaultValue">The default value.</param>
        public void OverrideDefaultValue(Type type, TValue defaultValue)
        {
            Contract.Requires<ArgumentNullException>(type != null);

            if (_defaultValues.ContainsKey(type))
            {
                throw new InvalidOperationException("Default value is already set for this property.");
            }

            _defaultValues.Add(type, defaultValue);
        }

        /// <summary>
        /// Overrides the validation function for the property on the specified type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="validation">The validation function.</param>
        public void OverrideValidation<T>(Func<T, TValue, TValue> validation)
            where T : IPerspexObject
        {
            var type = typeof(T);

            if (_validation.ContainsKey(type))
            {
                throw new InvalidOperationException("Validation is already set for this property.");
            }

            _validation.Add(type, Cast(validation));
        }

        /// <summary>
        /// Overrides the validation function for the property on the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="validation">The validation function.</param>
        public void OverrideValidation(Type type, Func<IPerspexObject, TValue, TValue> validation)
        {
            Contract.Requires<ArgumentNullException>(type != null);

            if (_validation.ContainsKey(type))
            {
                throw new InvalidOperationException("Validation is already set for this property.");
            }

            _validation.Add(type, validation);
        }

        /// <summary>
        /// Gets the string representation of the property.
        /// </summary>
        /// <returns>The property's string representation.</returns>
        public override string ToString()
        {
            return Name;
        }

        /// <inheritdoc/>
        Func<IPerspexObject, object, object> IStyledPropertyAccessor.GetValidationFunc(Type type)
        {
            var typed = GetValidationFunc(type);
            return (o, v) => typed(o, (TValue)v);
        }

        /// <inheritdoc/>
        object IStyledPropertyAccessor.GetDefaultValue(Type type) => GetDefaultValue(type);
    }
}
