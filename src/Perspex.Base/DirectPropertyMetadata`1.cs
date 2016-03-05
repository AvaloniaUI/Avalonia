// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Data;

namespace Perspex
{
    /// <summary>
    /// Metadata for direct perspex properties.
    /// </summary>
    public class DirectPropertyMetadata<TValue> : PropertyMetadata, IDirectPropertyMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StyledPropertyMetadata{TValue}"/> class.
        /// </summary>
        /// <param name="unsetValue">
        /// The value to use when the property is set to <see cref="PerspexProperty.UnsetValue"/>
        /// </param>
        /// <param name="defaultBindingMode">The default binding mode.</param>
        public DirectPropertyMetadata(
            TValue unsetValue = default(TValue),
            BindingMode defaultBindingMode = BindingMode.Default)
                : base(defaultBindingMode)
        {
            UnsetValue = unsetValue;
        }

        /// <summary>
        /// Gets the to use when the property is set to <see cref="PerspexProperty.UnsetValue"/>.
        /// </summary>
        public TValue UnsetValue { get; private set; }

        /// <inheritdoc/>
        object IDirectPropertyMetadata.UnsetValue => UnsetValue;

        /// <inheritdoc/>
        public override void Merge(PropertyMetadata baseMetadata, PerspexProperty property)
        {
            base.Merge(baseMetadata, property);

            var src = baseMetadata as DirectPropertyMetadata<TValue>;

            if (src != null)
            {
                if (UnsetValue == null)
                {
                    UnsetValue = src.UnsetValue;
                }
            }
        }
    }
}
