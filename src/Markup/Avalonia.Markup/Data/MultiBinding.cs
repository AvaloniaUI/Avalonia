using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Reactive;
using Avalonia.Data.Converters;
using Avalonia.Metadata;
using Avalonia.Data.Core;

namespace Avalonia.Data
{
    /// <summary>
    /// A XAML binding that calculates an aggregate value from multiple child <see cref="Bindings"/>.
    /// </summary>
    public class MultiBinding : IBinding2
    {
        /// <summary>
        /// Gets the collection of child bindings.
        /// </summary>
        [Content, AssignBinding]
        public IList<IBinding> Bindings { get; set; } = new List<IBinding>();

        /// <summary>
        /// Gets or sets the <see cref="IMultiValueConverter"/> to use.
        /// </summary>
        public IMultiValueConverter? Converter { get; set; }

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

        /// <inheritdoc/>
        public InstancedBinding? Initiate(
            AvaloniaObject target,
            AvaloniaProperty? targetProperty,
            object? anchor = null,
            bool enableDataValidation = false)
        {
            var input = InstanceCore(target, targetProperty);
            var mode = Mode == BindingMode.Default ?
                targetProperty?.GetMetadata(target.GetType()).DefaultBindingMode : Mode;

            switch (mode)
            {
                case BindingMode.OneTime:
                    return InstancedBinding.OneTime(input, Priority);
                case BindingMode.OneWay:
                    return InstancedBinding.OneWay(input, Priority);
                default:
                    throw new NotSupportedException(
                        "MultiBinding currently only supports OneTime and OneWay BindingMode.");
            }
        }

        BindingExpressionBase IBinding2.Instance(AvaloniaObject target, AvaloniaProperty property, object? anchor)
        {
            // TODO: Implement MultiBindingExpression instead of wrapping an observable.
            var o = InstanceCore(target, property);
            return new UntypedObservableBindingExpression(o, BindingPriority.LocalValue);
        }

        private IObservable<object?> InstanceCore(AvaloniaObject target, AvaloniaProperty? targetProperty)
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

            var children = Bindings.Select(x => x.Initiate(target, null));

            return children.Select(x => x?.Source)
                .Where(x => x is not null)!
                .CombineLatest()
                .Select(x => ConvertValue(x, targetType, converter))
                .Where(x => x != BindingOperations.DoNothing);
        }

        private object ConvertValue(IList<object?> values, Type targetType, IMultiValueConverter? converter)
        {
            for (var i = 0; i < values.Count; ++i)
            {
                if (values[i] is BindingNotification notification)
                {
                    values[i] = notification.Value;
                }
            }

            var culture = CultureInfo.CurrentCulture;
            values = new System.Collections.ObjectModel.ReadOnlyCollection<object?>(values);
            object? converted;
            if (converter != null)
            {
                converted = converter.Convert(values, targetType, ConverterParameter, culture);
            }
            else
            {
                converted = values;
            }

            if (converted == null)
            {
                converted = TargetNullValue;
            }

            if (converted == AvaloniaProperty.UnsetValue)
            {
                converted = FallbackValue;
            }

            return converted;
        }
    }
}
