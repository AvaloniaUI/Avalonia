// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Diagnostics;
using Avalonia.Data;

namespace Avalonia
{
    /// <summary>
    /// Metadata for styled avalonia properties.
    /// </summary>
    public class StyledPropertyMetadata<TValue> : PropertyMetadata, IStyledPropertyMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StyledPropertyMetadata{TValue}"/> class.
        /// </summary>
        /// <param name="defaultValue">The default value of the property.</param>
        /// <param name="defaultBindingMode">The default binding mode.</param>
        public StyledPropertyMetadata(
            TValue defaultValue = default,
            BindingMode defaultBindingMode = BindingMode.Default)
                : base(defaultBindingMode)
        {
            DefaultValue = new BoxedValue<TValue>(defaultValue);
        }

        /// <summary>
        /// Gets the default value for the property.
        /// </summary>
        internal BoxedValue<TValue> DefaultValue { get; private set; }

        object IStyledPropertyMetadata.DefaultValue => DefaultValue.Boxed;

        /// <inheritdoc/>
        public override void Merge(PropertyMetadata baseMetadata, AvaloniaProperty property)
        {
            base.Merge(baseMetadata, property);

            if (baseMetadata is StyledPropertyMetadata<TValue> src)
            {
                if (DefaultValue.Boxed == null)
                {
                    DefaultValue = src.DefaultValue;
                }
            }
        }
    }
}
