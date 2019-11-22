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
        private Optional<TValue> _defaultValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="StyledPropertyMetadata{TValue}"/> class.
        /// </summary>
        /// <param name="defaultValue">The default value of the property.</param>
        /// <param name="defaultBindingMode">The default binding mode.</param>
        public StyledPropertyMetadata(
            Optional<TValue> defaultValue = default,
            BindingMode defaultBindingMode = BindingMode.Default)
                : base(defaultBindingMode)
        {
            _defaultValue = defaultValue;
        }

        /// <summary>
        /// Gets the default value for the property.
        /// </summary>
        internal TValue DefaultValue => _defaultValue.ValueOrDefault();

        object IStyledPropertyMetadata.DefaultValue => DefaultValue;

        /// <inheritdoc/>
        public override void Merge(PropertyMetadata baseMetadata, AvaloniaProperty property)
        {
            base.Merge(baseMetadata, property);

            if (baseMetadata is StyledPropertyMetadata<TValue> src)
            {
                if (!_defaultValue.HasValue)
                {
                    _defaultValue = src.DefaultValue;
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
