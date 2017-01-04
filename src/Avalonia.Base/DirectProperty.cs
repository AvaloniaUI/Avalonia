// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Data;

namespace Avalonia
{
    /// <summary>
    /// A direct avalonia property.
    /// </summary>
    /// <typeparam name="TOwner">The class that registered the property.</typeparam>
    /// <typeparam name="TValue">The type of the property's value.</typeparam>
    /// <remarks>
    /// Direct avalonia properties are backed by a field on the object, but exposed via the
    /// <see cref="AvaloniaProperty"/> system. They hold a getter and an optional setter which
    /// allows the avalonia property system to read and write the current value.
    /// </remarks>
    public class DirectProperty<TOwner, TValue> : AvaloniaProperty<TValue>, IDirectPropertyAccessor
        where TOwner : IAvaloniaObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DirectProperty{TOwner, TValue}"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="getter">Gets the current value of the property.</param>
        /// <param name="setter">Sets the value of the property. May be null.</param>
        /// <param name="metadata">The property metadata.</param>
        public DirectProperty(
            string name,
            Func<TOwner, TValue> getter,
            Action<TOwner, TValue> setter,
            DirectPropertyMetadata<TValue> metadata)
            : base(name, typeof(TOwner), metadata)
        {
            Contract.Requires<ArgumentNullException>(getter != null);

            Getter = getter;
            Setter = setter;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaProperty"/> class.
        /// </summary>
        /// <param name="source">The property to copy.</param>
        /// <param name="getter">Gets the current value of the property.</param>
        /// <param name="setter">Sets the value of the property. May be null.</param>
        /// <param name="metadata">Optional overridden metadata.</param>
        private DirectProperty(
            AvaloniaProperty<TValue> source,
            Func<TOwner, TValue> getter,
            Action<TOwner, TValue> setter,
            DirectPropertyMetadata<TValue> metadata)
            : base(source, typeof(TOwner), metadata)
        {
            Contract.Requires<ArgumentNullException>(getter != null);

            Getter = getter;
            Setter = setter;
        }

        /// <inheritdoc/>
        public override bool IsDirect => true;

        /// <inheritdoc/>
        public override bool IsReadOnly => Setter == null;

        /// <summary>
        /// Gets the getter function.
        /// </summary>
        public Func<TOwner, TValue> Getter { get; }

        /// <summary>
        /// Gets the setter function.
        /// </summary>
        public Action<TOwner, TValue> Setter { get; }

        /// <summary>
        /// Registers the direct property on another type.
        /// </summary>
        /// <typeparam name="TNewOwner">The type of the additional owner.</typeparam>
        /// <param name="getter">Gets the current value of the property.</param>
        /// <param name="setter">Sets the value of the property.</param>
        /// <param name="unsetValue">
        /// The value to use when the property is set to <see cref="AvaloniaProperty.UnsetValue"/>
        /// </param>
        /// <param name="defaultBindingMode">The default binding mode for the property.</param>
        /// <param name="enableDataValidation">
        /// Whether the property is interested in data validation.
        /// </param>
        /// <returns>The property.</returns>
        public DirectProperty<TNewOwner, TValue> AddOwner<TNewOwner>(
            Func<TNewOwner, TValue> getter,
            Action<TNewOwner, TValue> setter = null,
            TValue unsetValue = default(TValue),
            BindingMode defaultBindingMode = BindingMode.Default,
            bool enableDataValidation = false)
                where TNewOwner : AvaloniaObject
        {
            var metadata = new DirectPropertyMetadata<TValue>(
                unsetValue: unsetValue,
                defaultBindingMode: defaultBindingMode,
                enableDataValidation: enableDataValidation);

            metadata.Merge(GetMetadata<TOwner>(), this);

            var result = new DirectProperty<TNewOwner, TValue>(
                this,
                getter,
                setter,
                metadata);

            AvaloniaPropertyRegistry.Instance.Register(typeof(TNewOwner), result);
            return result;
        }

        /// <inheritdoc/>
        object IDirectPropertyAccessor.GetValue(IAvaloniaObject instance)
        {
            return Getter((TOwner)instance);
        }

        /// <inheritdoc/>
        void IDirectPropertyAccessor.SetValue(IAvaloniaObject instance, object value)
        {
            if (Setter == null)
            {
                throw new ArgumentException($"The property {Name} is readonly.");
            }

            Setter((TOwner)instance, (TValue)value);
        }
    }
}
