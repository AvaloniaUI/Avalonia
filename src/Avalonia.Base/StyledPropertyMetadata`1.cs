using System;
using Avalonia.Data;

namespace Avalonia
{
    /// <summary>
    /// Metadata for styled avalonia properties.
    /// </summary>
    public class StyledPropertyMetadata<TValue> : AvaloniaPropertyMetadata, IStyledPropertyMetadata
    {
        private Optional<TValue> _defaultValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="StyledPropertyMetadata{TValue}"/> class.
        /// </summary>
        /// <param name="defaultValue">The default value of the property.</param>
        /// <param name="defaultBindingMode">The default binding mode.</param>
        /// <param name="coerce">A value coercion callback.</param>
        /// <param name="enableDataValidation">Whether the property is interested in data validation.</param>
        public StyledPropertyMetadata(
            Optional<TValue> defaultValue = default,
            BindingMode defaultBindingMode = BindingMode.Default,
            Func<AvaloniaObject, TValue, TValue>? coerce = null,
            bool enableDataValidation = false)
                : base(defaultBindingMode, enableDataValidation)
        {
            _defaultValue = defaultValue;
            CoerceValue = coerce;
        }

        /// <summary>
        /// Gets the default value for the property.
        /// </summary>
        public TValue DefaultValue => _defaultValue.GetValueOrDefault()!;

        /// <summary>
        /// Gets the value coercion callback, if any.
        /// </summary>
        public Func<AvaloniaObject, TValue, TValue>? CoerceValue { get; private set; }

        object? IStyledPropertyMetadata.DefaultValue => DefaultValue;

        /// <inheritdoc/>
        public override void Merge(AvaloniaPropertyMetadata baseMetadata, AvaloniaProperty property)
        {
            base.Merge(baseMetadata, property);

            if (baseMetadata is StyledPropertyMetadata<TValue> src)
            {
                if (!_defaultValue.HasValue)
                {
                    _defaultValue = src.DefaultValue;
                }

                if (CoerceValue == null)
                {
                    CoerceValue = src.CoerceValue;
                }
            }
        }

        public override AvaloniaPropertyMetadata GenerateTypeSafeMetadata() => new StyledPropertyMetadata<TValue>(DefaultValue, DefaultBindingMode, enableDataValidation: EnableDataValidation ?? false);
    }
}
