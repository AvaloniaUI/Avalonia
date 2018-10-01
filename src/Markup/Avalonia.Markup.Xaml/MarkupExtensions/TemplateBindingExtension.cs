using System;
using System.Windows.Markup;
using Avalonia.Data;
using Avalonia.Data.Converters;

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
        public AvaloniaProperty Property { get; set; }

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
    }
}
