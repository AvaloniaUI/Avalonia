// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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
    /// A XAML binding that calculates an aggregate value from multiple child <see cref="Bindings"/>.
    /// </summary>
    public class MultiBinding : IBinding
    {
        /// <summary>
        /// Gets the collection of child bindings.
        /// </summary>
        [Content]
        public IList<IBinding> Bindings { get; set; } = new List<IBinding>();

        /// <summary>
        /// Gets or sets the <see cref="IValueConverter"/> to use.
        /// </summary>
        public IMultiValueConverter Converter { get; set; }

        /// <summary>
        /// Gets or sets the value to use when the binding is unable to produce a value.
        /// </summary>
        public object FallbackValue { get; set; }

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
        public RelativeSource RelativeSource { get; set; }

        /// <inheritdoc/>
        public InstancedBinding Initiate(
            IAvaloniaObject target,
            AvaloniaProperty targetProperty,
            object anchor = null,
            bool enableDataValidation = false)
        {
            if (Converter == null)
            {
                throw new NotSupportedException("MultiBinding without Converter not currently supported.");
            }

            var targetType = targetProperty?.PropertyType ?? typeof(object);
            var children = Bindings.Select(x => x.Initiate(target, null));
            var input = children.Select(x => x.Observable).CombineLatest().Select(x => ConvertValue(x, targetType));
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

        private object ConvertValue(IList<object> values, Type targetType)
        {
            var converted = Converter.Convert(values, targetType, null, CultureInfo.CurrentCulture);

            if (converted == AvaloniaProperty.UnsetValue && FallbackValue != null)
            {
                converted = FallbackValue;
            }

            return converted;
        }
    }
}
