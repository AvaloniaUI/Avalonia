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
        /// <param name="notifying">
        /// A method that gets called before and after the property starts being notified on an
        /// object; the bool argument will be true before and false afterwards. This callback is
        /// intended to support IsDataContextChanging.
        /// </param>
        /// <param name="isAttached">Whether the property is an attached property.</param>
        public PerspexProperty(
            string name,
            Type ownerType,
            TValue defaultValue = default(TValue),
            bool inherits = false,
            BindingMode defaultBindingMode = BindingMode.Default,
            Func<PerspexObject, TValue, TValue> validate = null,
            Action<PerspexObject, bool> notifying = null,
            bool isAttached = false)
            : base(
                name,
                typeof(TValue),
                ownerType,
                defaultValue,
                inherits,
                defaultBindingMode,
                Cast(validate),
                notifying,
                isAttached)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PerspexProperty{TValue}"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="ownerType">The type of the class that registers the property.</param>
        /// <param name="getter">Gets the current value of the property.</param>
        /// <param name="setter">Sets the value of the property.</param>
        public PerspexProperty(
            string name,
            Type ownerType,
            Func<PerspexObject, TValue> getter,
            Action<PerspexObject, TValue> setter)
            : base(name, typeof(TValue), ownerType, CastParamReturn(getter), CastParams(setter))
        {
            Getter = getter;
            Setter = setter;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PerspexProperty"/> class.
        /// </summary>
        /// <param name="source">The direct property to copy.</param>
        /// <param name="getter">A new getter.</param>
        /// <param name="setter">A new setter.</param>
        private PerspexProperty(
            PerspexProperty source,
            Func<PerspexObject, TValue> getter,
            Action<PerspexObject, TValue> setter)
            : base(source, CastParamReturn(getter), CastParams(setter))
        {
            Getter = getter;
            Setter = setter;
        }

        /// <summary>
        /// Gets the getter function for direct properties.
        /// </summary>
        internal new Func<PerspexObject, TValue> Getter { get; }

        /// <summary>
        /// Gets the etter function for direct properties.
        /// </summary>
        internal new Action<PerspexObject, TValue> Setter { get; }

        /// <summary>
        /// Registers the property on another type.
        /// </summary>
        /// <typeparam name="TOwner">The type of the additional owner.</typeparam>
        /// <returns>The property.</returns>
        public PerspexProperty<TValue> AddOwner<TOwner>() where TOwner : PerspexObject
        {
            if (IsDirect)
            {
                throw new InvalidOperationException(
                    "You must provide a new getter and setter when calling AddOwner on a direct PerspexProperty.");
            }

            PerspexObject.Register(typeof(TOwner), this);
            return this;
        }

        /// <summary>
        /// Registers the direct property on another type.
        /// </summary>
        /// <typeparam name="TOwner">The type of the additional owner.</typeparam>
        /// <returns>The property.</returns>
        public PerspexProperty<TValue> AddOwner<TOwner>(
            Func<TOwner, TValue> getter,
            Action<TOwner, TValue> setter = null)
                where TOwner : PerspexObject
        {
            var result = new PerspexProperty<TValue>(this, CastReturn(getter), CastParam1(setter));
            PerspexObject.Register(typeof(TOwner), result);
            return result;
        }

        /// <summary>
        /// Gets the default value for the property on the specified type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <returns>The default value.</returns>
        public TValue GetDefaultValue<T>()
        {
            return (TValue)GetDefaultValue(typeof(T));
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
            OverrideValidation(typeof(T), f);
        }

        /// <summary>
        /// Casts a typed getter function to an untyped.
        /// </summary>
        /// <typeparam name="TOwner">The owner type.</typeparam>
        /// <param name="f">The typed function.</param>
        /// <returns>The untyped function.</returns>
        private static Func<PerspexObject, object> CastParamReturn<TOwner>(Func<TOwner, TValue> f)
            where TOwner : PerspexObject
        {
            return (f != null) ? o => f((TOwner)o) : (Func<PerspexObject, object>)null;
        }

        /// <summary>
        /// Casts a typed getter function to an untyped.
        /// </summary>
        /// <typeparam name="TOwner">The owner type.</typeparam>
        /// <param name="f">The typed function.</param>
        /// <returns>The untyped function.</returns>
        private static Func<PerspexObject, TValue> CastReturn<TOwner>(Func<TOwner, TValue> f)
            where TOwner : PerspexObject
        {
            return (f != null) ? o => f((TOwner)o) : (Func<PerspexObject, TValue>)null;
        }

        /// <summary>
        /// Casts a typed setter function to an untyped.
        /// </summary>
        /// <typeparam name="TOwner">The owner type.</typeparam>
        /// <param name="f">The typed function.</param>
        /// <returns>The untyped function.</returns>
        private static Action<PerspexObject, object> CastParams<TOwner>(Action<TOwner, TValue> f)
            where TOwner : PerspexObject
        {
            return (f != null) ? (o, v) => f((TOwner)o, (TValue)v) : (Action<PerspexObject, object>)null;
        }

        /// <summary>
        /// Casts a typed setter function to an untyped.
        /// </summary>
        /// <typeparam name="TOwner">The owner type.</typeparam>
        /// <param name="f">The typed function.</param>
        /// <returns>The untyped function.</returns>
        private static Action<PerspexObject, TValue> CastParam1<TOwner>(Action<TOwner, TValue> f)
            where TOwner : PerspexObject
        {
            return (f != null) ? (o, v) => f((TOwner)o, v) : (Action<PerspexObject, TValue>)null;
        }

        /// <summary>
        /// Casts a typed validation function to an untyped.
        /// </summary>
        /// <param name="f">The typed validation function.</param>
        /// <returns>The untyped validation function.</returns>
        private static Func<PerspexObject, object, object> Cast(Func<PerspexObject, TValue, TValue> f)
        {
            return f != null ? (o, v) => f(o, (TValue)v) : (Func<PerspexObject, object, object>)null;
        }
    }
}
