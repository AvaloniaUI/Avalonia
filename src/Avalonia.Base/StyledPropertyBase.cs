// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Avalonia
{
    /// <summary>
    /// Base class for styled properties.
    /// </summary>
    public class StyledPropertyBase<TValue> : AvaloniaProperty<TValue>, IStyledPropertyAccessor
    {
        private bool _inherits;

        /// <summary>
        /// Initializes a new instance of the <see cref="StyledPropertyBase{T}"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="ownerType">The type of the class that registers the property.</param>
        /// <param name="metadata">The property metadata.</param>
        /// <param name="inherits">Whether the property inherits its value.</param>
        /// <param name="notifying">A <see cref="AvaloniaProperty.Notifying"/> callback.</param>
        protected StyledPropertyBase(
            string name,
            Type ownerType,            
            StyledPropertyMetadata<TValue> metadata,
            bool inherits = false,
            Action<IAvaloniaObject, bool> notifying = null)
                : base(name, ownerType, metadata, notifying)
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
            : base(source, ownerType, null)
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

            return GetMetadata(type).DefaultValue.Typed;
        }

        /// <summary>
        /// Gets the property metadata for the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// The property metadata.
        /// </returns>
        public new StyledPropertyMetadata<TValue> GetMetadata(Type type)
        {
            return (StyledPropertyMetadata<TValue>)base.GetMetadata(type);
        }

        /// <summary>
        /// Overrides the default value for the property on the specified type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="defaultValue">The default value.</param>
        public void OverrideDefaultValue<T>(TValue defaultValue) where T : IAvaloniaObject
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
            OverrideMetadata(type, new StyledPropertyMetadata<TValue>(defaultValue));
        }

        /// <summary>
        /// Overrides the metadata for the property on the specified type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="metadata">The metadata.</param>
        public void OverrideMetadata<T>(StyledPropertyMetadata<TValue> metadata) where T : IAvaloniaObject
        {
            base.OverrideMetadata(typeof(T), metadata);
        }

        /// <summary>
        /// Overrides the metadata for the property on the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="metadata">The metadata.</param>
        public void OverrideMetadata(Type type, StyledPropertyMetadata<TValue> metadata)
        {
            base.OverrideMetadata(type, metadata);
        }

        /// <summary>
        /// Overrides the validation function for the specified type.
        /// </summary>
        /// <typeparam name="THost">The type.</typeparam>
        /// <param name="validate">The validation function.</param>
        public void OverrideValidation<THost>(Func<THost, TValue, TValue> validate)
            where THost : IAvaloniaObject
        {
            Func<IAvaloniaObject, TValue, TValue> f;

            if (validate != null)
            {
                f = Cast(validate);
            }
            else
            {
                // Passing null to the validation function means that the property metadata merge
                // will take the base validation function, so instead use an empty validation.
                f = (o, v) => v;
            }

            base.OverrideMetadata(typeof(THost), new StyledPropertyMetadata<TValue>(validate: f));
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
        Func<IAvaloniaObject, object, object> IStyledPropertyAccessor.GetValidationFunc(Type type)
        {
            Contract.Requires<ArgumentNullException>(type != null);
            return ((IStyledPropertyMetadata)base.GetMetadata(type)).Validate;
        }

        /// <inheritdoc/>
        object IStyledPropertyAccessor.GetDefaultValue(Type type) => GetDefaultBoxedValue(type);

        private object GetDefaultBoxedValue(Type type)
        {
            Contract.Requires<ArgumentNullException>(type != null);

            return GetMetadata(type).DefaultValue.Boxed;
        }

        [DebuggerHidden]
        private Func<IAvaloniaObject, TValue, TValue> Cast<THost>(Func<THost, TValue, TValue> validate)
        {
            return (o, v) => validate((THost)o, v);
        }
    }
}
