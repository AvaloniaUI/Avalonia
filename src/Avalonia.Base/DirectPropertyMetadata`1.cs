// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Data;

namespace Avalonia
{
    /// <summary>
    /// Metadata for direct avalonia properties.
    /// </summary>
    public class DirectPropertyMetadata<TValue> : PropertyMetadata, IDirectPropertyMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StyledPropertyMetadata{TValue}"/> class.
        /// </summary>
        /// <param name="unsetValue">
        /// The value to use when the property is set to <see cref="AvaloniaProperty.UnsetValue"/>
        /// </param>
        /// <param name="defaultBindingMode">The default binding mode.</param>
        /// <param name="enableDataValidation">
        /// Whether the property is interested in data validation.
        /// </param>
        public DirectPropertyMetadata(
            TValue unsetValue = default(TValue),
            BindingMode defaultBindingMode = BindingMode.Default,
            bool enableDataValidation = false)
                : base(defaultBindingMode, enableDataValidation)
        {
            UnsetValue = unsetValue;
        }

        /// <summary>
        /// Gets the to use when the property is set to <see cref="AvaloniaProperty.UnsetValue"/>.
        /// </summary>
        public TValue UnsetValue { get; private set; }

        /// <inheritdoc/>
        object IDirectPropertyMetadata.UnsetValue => UnsetValue;

        /// <inheritdoc/>
        public override void Merge(PropertyMetadata baseMetadata, AvaloniaProperty property)
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
