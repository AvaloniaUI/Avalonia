// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Data;

namespace Perspex
{
    /// <summary>
    /// Styled perspex property metadata.
    /// </summary>
    public class StyledPropertyMetadata : PropertyMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StyledPropertyMetadata"/> class.
        /// </summary>
        /// <param name="defaultValue">The default value of the property.</param>
        /// <param name="validate">A validation function.</param>
        /// <param name="defaultBindingMode">The default binding mode.</param>
        /// <param name="notifyingCallback">The property notifying callback.</param>
        public StyledPropertyMetadata(
            object defaultValue,
            Func<IPerspexObject, object, object> validate = null,
            BindingMode defaultBindingMode = BindingMode.Default,
            Action<IPerspexObject, bool> notifyingCallback = null)
                : base(defaultBindingMode, notifyingCallback)
        {
            DefaultValue = defaultValue;
            Validate = validate;
        }

        /// <summary>
        /// Gets the default value for the property.
        /// </summary>
        public object DefaultValue { get; private set; }

        /// <summary>
        /// Gets the validation callback.
        /// </summary>
        public Func<IPerspexObject, object, object> Validate { get; private set; }

        /// <inheritdoc/>
        public override void Merge(PropertyMetadata baseMetadata, PerspexProperty property)
        {
            base.Merge(baseMetadata, property);

            var src = baseMetadata as StyledPropertyMetadata;

            if (src != null)
            {
                if (DefaultValue == null)
                {
                    DefaultValue = src.DefaultValue;
                }

                if (Validate != null)
                {
                    Validate = src.Validate;
                }
            }
        }
    }
}
