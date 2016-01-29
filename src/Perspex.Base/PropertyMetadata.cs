// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Data;

namespace Perspex
{
    /// <summary>
    /// Base class for perspex property metadata.
    /// </summary>
    public class PropertyMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyMetadata"/> class.
        /// </summary>
        /// <param name="defaultBindingMode">The default binding mode.</param>
        public PropertyMetadata(BindingMode defaultBindingMode = BindingMode.Default)
        {
            DefaultBindingMode = defaultBindingMode;
        }

        /// <summary>
        /// Gets the default binding mode for the property.
        /// </summary>
        public BindingMode DefaultBindingMode { get; private set; }

        /// <summary>
        /// Merges the metadata with the base metadata.
        /// </summary>
        /// <param name="baseMetadata">The base metadata to merge.</param>
        /// <param name="property">The property to which the metadata is being applied.</param>
        public virtual void Merge(
            PropertyMetadata baseMetadata, 
            PerspexProperty property)
        {
            if (DefaultBindingMode == BindingMode.Default)
            {
                DefaultBindingMode = baseMetadata.DefaultBindingMode;
            }
        }
    }
}
