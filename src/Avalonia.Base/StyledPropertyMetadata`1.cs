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
        /// <param name="validate">A validation function.</param>
        /// <param name="defaultBindingMode">The default binding mode.</param>
        public StyledPropertyMetadata(
            TValue defaultValue = default,
            Func<IAvaloniaObject, TValue, TValue> validate = null,
            BindingMode defaultBindingMode = BindingMode.Default)
                : base(defaultBindingMode)
        {
            DefaultValue = new BoxedValue<TValue>(defaultValue);
            Validate = validate;
        }

        /// <summary>
        /// Gets the default value for the property.
        /// </summary>
        internal BoxedValue<TValue> DefaultValue { get; private set; }

        /// <summary>
        /// Gets the validation callback.
        /// </summary>
        public Func<IAvaloniaObject, TValue, TValue> Validate { get; private set; }

        object IStyledPropertyMetadata.DefaultValue => DefaultValue.Boxed;

        Func<IAvaloniaObject, object, object> IStyledPropertyMetadata.Validate => Cast(Validate);

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

                if (Validate == null)
                {
                    Validate = src.Validate;
                }
            }
        }

        [DebuggerHidden]
        private static Func<IAvaloniaObject, object, object> Cast(Func<IAvaloniaObject, TValue, TValue> f)
        {
            if (f == null)
            {
                return null;
            }
            else
            {
                return (o, v) => f(o, (TValue)v);
            }
        }
    }
}
