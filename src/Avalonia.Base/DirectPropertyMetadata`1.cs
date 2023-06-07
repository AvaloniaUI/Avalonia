using Avalonia.Data;

namespace Avalonia
{
    /// <summary>
    /// Metadata for direct avalonia properties.
    /// </summary>
    public class DirectPropertyMetadata<TValue> : AvaloniaPropertyMetadata, IDirectPropertyMetadata
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
            TValue unsetValue = default!,
            BindingMode defaultBindingMode = BindingMode.Default,
            bool? enableDataValidation = null)
                : base(defaultBindingMode, enableDataValidation)
        {
            UnsetValue = unsetValue;
        }

        /// <summary>
        /// Gets the value to use when the property is set to <see cref="AvaloniaProperty.UnsetValue"/>.
        /// </summary>
        public TValue UnsetValue { get; private set; }


        /// <inheritdoc/>
        object? IDirectPropertyMetadata.UnsetValue => UnsetValue;

        /// <inheritdoc/>
        public override void Merge(AvaloniaPropertyMetadata baseMetadata, AvaloniaProperty property)
        {
            base.Merge(baseMetadata, property);

            if (baseMetadata is DirectPropertyMetadata<TValue> src)
            {
                UnsetValue ??= src.UnsetValue;
            }
        }

        public override AvaloniaPropertyMetadata GenerateTypeSafeMetadata() => new DirectPropertyMetadata<TValue>(UnsetValue, DefaultBindingMode, EnableDataValidation);
    }
}
