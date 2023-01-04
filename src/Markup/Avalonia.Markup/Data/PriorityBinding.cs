using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Data.Converters;
using Avalonia.Metadata;

namespace Avalonia.Data
{
    /// <summary>
    /// A XAML binding that uses the first valid result from multiple child <see cref="Bindings"/>.
    /// </summary>
    /// <remarks>
    /// A valid biding result is anything other than <see cref="BindingOperations.DoNothing"/> 
    /// and <see cref="AvaloniaProperty.UnsetValue"/>.
    /// </remarks>
    public class PriorityBinding : IBinding
    {
        /// <summary>
        /// Gets the collection of child bindings.
        /// </summary>
        [Content, AssignBinding]
        public IList<IBinding> Bindings { get; set; } = new List<IBinding>();

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
        /// Gets or sets the string format.
        /// </summary>
        public string? StringFormat { get; set; }

        public PriorityBinding()
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
            var targetType = targetProperty?.PropertyType ?? typeof(object);
            IValueConverter? converter = null;
            // We only respect `StringFormat` if the type of the property we're assigning to will
            // accept a string. Note that this is slightly different to WPF in that WPF only applies
            // `StringFormat` for target type `string` (not `object`).
            if (!string.IsNullOrWhiteSpace(StringFormat) &&
                (targetType == typeof(string) || targetType == typeof(object)))
            {
                converter = new StringFormatValueConverter(StringFormat!, converter);
            }

            var children = Bindings.Select(x => x.Initiate(target, null));

            var input = children.Select(x => x?.Observable!)
                                .Where(x => x is not null)
                                .CombineLatest()
                                .Select(BoundValuesGetFirstOrDefault)
                                .Select(x => converter == null ? x : converter.Convert(x, targetType, null, CultureInfo.CurrentCulture));

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
                        "PriorityBinding currently only supports OneTime and OneWay BindingMode.");
            }
        }

        object? BoundValuesGetFirstOrDefault(IList<object?> values)
        {
            object? result = null;
            foreach (object? i in values)
            {
                object? value = i;
                if (value is BindingNotification notification)
                {
                    value = notification.Value;
                }

                if (value != AvaloniaProperty.UnsetValue && value != BindingOperations.DoNothing)
                {
                    result = value;
                }
            }

            if(result == null)
            {
                result = TargetNullValue;
            }

            if(result == AvaloniaProperty.UnsetValue)
            {
                result = FallbackValue;
            }

            return result;
        }
    }
}
