// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reflection;
using Perspex.Utilities;

namespace Perspex
{
    /// <summary>
    /// Base class for styled properties.
    /// </summary>
    public class StyledPropertyBase<TValue> : PerspexProperty<TValue>, IStyledPropertyAccessor
    {
        private bool _inherits;

        /// <summary>
        /// Initializes a new instance of the <see cref="StyledPropertyBase{T}"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="ownerType">The type of the class that registers the property.</param>
        /// <param name="inherits">Whether the property inherits its value.</param>
        /// <param name="metadata">The property metadata.</param>
        protected StyledPropertyBase(
            string name,
            Type ownerType,
            bool inherits,
            StyledPropertyMetadata metadata)
                : base(name, ownerType, CheckMetadata(metadata))
        {
            Contract.Requires<ArgumentNullException>(name != null);
            Contract.Requires<ArgumentNullException>(ownerType != null);

            if (name.Contains("."))
            {
                throw new ArgumentException("'name' may not contain periods.");
            }

            _inherits = inherits;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StyledPropertyBase{T}"/> class.
        /// </summary>
        /// <param name="source">The property to add the owner to.</param>
        /// <param name="ownerType">The type of the class that registers the property.</param>
        protected StyledPropertyBase(StyledPropertyBase<TValue> source, Type ownerType)
            : base(source, ownerType)
        {
            _inherits = source.Inherits;
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

            return (TValue)(GetMetadata(type) as StyledPropertyMetadata)?.DefaultValue;
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
            return null;
            ////Contract.Requires<ArgumentNullException>(type != null);

            ////while (type != null)
            ////{
            ////    Func<IPerspexObject, TValue, TValue> result;

            ////    if (_validation.TryGetValue(type, out result))
            ////    {
            ////        return result;
            ////    }

            ////    type = type.GetTypeInfo().BaseType;
            ////}

            ////return null;
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
            OverrideMetadata(type, new StyledPropertyMetadata(defaultValue));
        }

        /// <summary>
        /// Overrides the validation function for the property on the specified type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="validation">The validation function.</param>
        public void OverrideValidation<T>(Func<T, TValue, TValue> validation)
            where T : IPerspexObject
        {
            throw new NotImplementedException();
            ////var type = typeof(T);

            ////if (_validation.ContainsKey(type))
            ////{
            ////    throw new InvalidOperationException("Validation is already set for this property.");
            ////}

            ////_validation.Add(type, Cast(validation));
        }

        /// <summary>
        /// Overrides the validation function for the property on the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="validation">The validation function.</param>
        public void OverrideValidation(Type type, Func<IPerspexObject, TValue, TValue> validation)
        {
            throw new NotImplementedException();
            //Contract.Requires<ArgumentNullException>(type != null);

            //if (_validation.ContainsKey(type))
            //{
            //    throw new InvalidOperationException("Validation is already set for this property.");
            //}

            //_validation.Add(type, validation);
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

            if (typed != null)
            {
                return (o, v) => typed(o, (TValue)v);
            }
            else
            {
                return null;
            }
        }

        /// <inheritdoc/>
        object IStyledPropertyAccessor.GetDefaultValue(Type type) => GetDefaultValue(type);

        private static PropertyMetadata CheckMetadata(StyledPropertyMetadata metadata)
        {
            var valueType = typeof(TValue).GetTypeInfo();

            if (metadata.DefaultValue != null)
            {
                var defaultType = metadata.DefaultValue.GetType().GetTypeInfo();

                if (!valueType.IsAssignableFrom(defaultType))
                {
                    throw new ArgumentException(
                        "Invalid default property value. " +
                        $"Expected {typeof(TValue)} but recieved {metadata.DefaultValue.GetType()}.");
                }
            }
            else
            {
                if (!TypeUtilities.AcceptsNull(typeof(TValue)))
                {
                    throw new ArgumentException(
                        $"Invalid default property value. Null is not a valid value for {typeof(TValue)}.");
                }
            }

            return metadata;
        }
    }
}
