using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;
using Avalonia.Metadata;

namespace Avalonia.Data
{
    /// <summary>
    /// A XAML binding that calculates an aggregate value from multiple child <see cref="Bindings"/>.
    /// </summary>
    public sealed class MultiBinding : BindingBase
    {
        /// <summary>
        /// Gets the collection of child bindings.
        /// </summary>
        [Content, AssignBinding]
        public IList<BindingBase> Bindings { get; set; } = new List<BindingBase>();

        /// <summary>
        /// Gets or sets the <see cref="IMultiValueConverter"/> to use.
        /// </summary>
        public IMultiValueConverter? Converter { get; set; }

        /// <summary>
        /// Gets or sets the culture in which to evaluate the converter.
        /// </summary>
        /// <value>The default value is null.</value>
        /// <remarks>
        /// If this property is not set then <see cref="CultureInfo.CurrentCulture"/> will be used.
        /// </remarks>
        [TypeConverter(typeof(CultureInfoIetfLanguageTagConverter))]
        public CultureInfo? ConverterCulture { get; set; }

        /// <summary>
        /// Gets or sets a parameter to pass to <see cref="Converter"/>.
        /// </summary>
        public object? ConverterParameter { get; set; }

        /// <summary>
        /// Gets or sets the value to use when the binding is unable to produce a value.
        /// </summary>
        public object FallbackValue { get; set; }

        /// <summary>
        /// Gets or sets the value to use when the binding result is null.
        /// </summary>
        public object TargetNullValue { get; set; }

        /// <summary>
        /// Gets or sets the binding mode.
        /// </summary>
        public BindingMode Mode { get; set; } = BindingMode.OneWay;

        /// <summary>
        /// Gets or sets the binding priority.
        /// </summary>
        public BindingPriority Priority { get; set; }

        /// <summary>
        /// Gets or sets the relative source for the binding.
        /// </summary>
        public RelativeSource? RelativeSource { get; set; }

        /// <summary>
        /// Gets or sets the string format.
        /// </summary>
        public string? StringFormat { get; set; }

        public MultiBinding()
        {
            FallbackValue = AvaloniaProperty.UnsetValue;
            TargetNullValue = AvaloniaProperty.UnsetValue;
        }

        internal override BindingExpressionBase CreateInstance(
            AvaloniaObject target,
            AvaloniaProperty? targetProperty,
            object? anchor)
        {
            return InstanceCore(target, targetProperty);
        }

        private MultiBindingExpression InstanceCore(
            AvaloniaObject target,
            AvaloniaProperty? targetProperty)
        {
            var targetType = targetProperty?.PropertyType ?? typeof(object);
            var converter = Converter;

            // We only respect `StringFormat` if the type of the property we're assigning to will
            // accept a string. Note that this is slightly different to WPF in that WPF only applies
            // `StringFormat` for target type `string` (not `object`).
            if (!string.IsNullOrWhiteSpace(StringFormat) &&
                (targetType == typeof(string) || targetType == typeof(object)))
            {
                converter = new StringFormatMultiValueConverter(StringFormat!, converter);
            }

            return new MultiBindingExpression(
                Priority,
                Bindings,
                converter,
                ConverterCulture,
                ConverterParameter,
                FallbackValue,
                TargetNullValue);
        }
    }
}
