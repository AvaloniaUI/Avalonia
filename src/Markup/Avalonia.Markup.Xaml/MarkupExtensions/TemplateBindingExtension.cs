// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Portable.Xaml.Markup;

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
    [MarkupExtensionReturnType(typeof(IBinding))]
    public class TemplateBindingExtension : MarkupExtension
    {
        public TemplateBindingExtension()
        {
        }

        public TemplateBindingExtension(AvaloniaProperty property)
        {
            Property = property;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new TemplateBinding
            {
                Converter = Converter,
                ConverterParameter = ConverterParameter,
                Mode = Mode,
                Property = Property,
            };
        }

        /// <summary>
        /// Gets or sets the <see cref="IValueConverter"/> to use.
        /// </summary>
        public IValueConverter Converter { get; set; }

        /// <summary>
        /// Gets or sets a parameter to pass to <see cref="Converter"/>.
        /// </summary>
        public object ConverterParameter { get; set; }

        /// <summary>
        /// Gets or sets the binding mode.
        /// </summary>
        public BindingMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the name of the source property on the templated parent.
        /// </summary>
        [ConstructorArgument("property")]
        public AvaloniaProperty Property { get; set; }
    }
}