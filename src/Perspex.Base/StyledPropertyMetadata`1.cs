// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Diagnostics;
using Perspex.Data;

namespace Perspex
{
    /// <summary>
    /// Metadata for styled perspex properties.
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
            TValue defaultValue = default(TValue),
            Func<IPerspexObject, TValue, TValue> validate = null,
            BindingMode defaultBindingMode = BindingMode.Default)
                : base(defaultBindingMode)
        {
            DefaultValue = defaultValue;
            Validate = validate;
        }

        /// <summary>
        /// Gets the default value for the property.
        /// </summary>
        public TValue DefaultValue { get; private set; }

        /// <summary>
        /// Gets the validation callback.
        /// </summary>
        public Func<IPerspexObject, TValue, TValue> Validate { get; private set; }

        object IStyledPropertyMetadata.DefaultValue => DefaultValue;

        Func<IPerspexObject, object, object> IStyledPropertyMetadata.Validate => Cast(Validate);

        /// <inheritdoc/>
        public override void Merge(PropertyMetadata baseMetadata, PerspexProperty property)
        {
            base.Merge(baseMetadata, property);

            var src = baseMetadata as StyledPropertyMetadata<TValue>;

            if (src != null)
            {
                if (DefaultValue == null)
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
        private static Func<IPerspexObject, object, object> Cast(Func<IPerspexObject, TValue, TValue> f)
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
