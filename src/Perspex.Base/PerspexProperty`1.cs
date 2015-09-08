// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex
{
    /// <summary>
    /// A typed perspex property.
    /// </summary>
    /// <typeparam name="TValue">The value type of the property.</typeparam>
    public class PerspexProperty<TValue> : PerspexProperty
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PerspexProperty{TValue}"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="ownerType">The type of the class that registers the property.</param>
        /// <param name="defaultValue">The default value of the property.</param>
        /// <param name="inherits">Whether the property inherits its value.</param>
        /// <param name="defaultBindingMode">The default binding mode for the property.</param>
        /// <param name="validate">A validation function.</param>
        /// <param name="isAttached">Whether the property is an attached property.</param>
        public PerspexProperty(
            string name,
            Type ownerType,
            TValue defaultValue = default(TValue),
            bool inherits = false,
            BindingMode defaultBindingMode = BindingMode.Default,
            Func<PerspexObject, TValue, TValue> validate = null,
            bool isAttached = false)
            : base(
                name,
                typeof(TValue),
                ownerType,
                defaultValue,
                inherits,
                defaultBindingMode,
                Convert(validate),
                isAttached)
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

        /// <summary>
        /// Overrides the validation function for the property on the specified type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="validation">The validation function.</param>
        public void OverrideValidation<T>(Func<T, TValue, TValue> validation) where T : PerspexObject
        {
            var f = validation != null ?
                (o, v) => validation((T)o, (TValue)v) :
                (Func<PerspexObject, object, object>)null;
            this.OverrideValidation(typeof(T), f);
        }

        /// <summary>
        /// Converts from a typed validation function to an untyped.
        /// </summary>
        /// <param name="f">The typed validation function.</param>
        /// <returns>The untyped validation function.</returns>
        private static Func<PerspexObject, object, object> Convert(Func<PerspexObject, TValue, TValue> f)
        {
            return f != null ? (o, v) => f(o, (TValue)v) : (Func<PerspexObject, object, object>)null;
        }
    }
}
