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
            bool? enableDataValidation = null)
                : base(defaultBindingMode)
        {
            UnsetValue = unsetValue;
            EnableDataValidation = enableDataValidation;
        }

        /// <summary>
        /// Gets the value to use when the property is set to <see cref="AvaloniaProperty.UnsetValue"/>.
        /// </summary>
        public TValue UnsetValue { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the property is interested in data validation.
        /// </summary>
        /// <remarks>
        /// Data validation is validation performed at the target of a binding, for example in a
        /// view model using the INotifyDataErrorInfo interface. Only certain properties on a
        /// control (such as a TextBox's Text property) will be interested in recieving data
        /// validation messages so this feature must be explicitly enabled by setting this flag.
        /// </remarks>
        public bool? EnableDataValidation { get; private set; }

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

                if (EnableDataValidation == null)
                {
                    EnableDataValidation = src.EnableDataValidation;
                }
            }
        }
    }
}
