// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Windows.Markup;
using System.Xaml;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Styling;

namespace Avalonia.Markup.Xaml.MarkupExtensions
{

    [MarkupExtensionReturnType(typeof(IBinding))]
    public class BindingExtension : MarkupExtension
    {
        public BindingExtension()
        {
        }

        public BindingExtension(string path)
        {
            Path = path;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var descriptorContext = (ITypeDescriptorContext)serviceProvider;

            return new Binding
            {
                TypeResolver = descriptorContext.ResolveType,
                Converter = Converter,
                ConverterParameter = ConverterParameter,
                ElementName = ElementName,
                FallbackValue = FallbackValue,
                Mode = Mode,
                Path = Path,
                Priority = Priority,
                Source = Source,
                StringFormat = StringFormat,
                RelativeSource = RelativeSource,
                DefaultAnchor = new WeakReference(GetDefaultAnchor(descriptorContext))
            };
        }

        private static object GetDefaultAnchor(ITypeDescriptorContext context)
        {
            // If the target is not a control, so we need to find an anchor that will let us look
            // up named controls and style resources. First look for the closest Control in
            // the context.
            object anchor = context.GetFirstAmbientValue<Control>();

            // If a control was not found, then try to find the highest-level style as the XAML
            // file could be a XAML file containing only styles.
            return anchor ??
                    context.GetService<IRootObjectProvider>()?.RootObject as IStyle ??
                    context.GetLastOrDefaultAmbientValue<Style>();
        }

        public IValueConverter Converter { get; set; }

        public object ConverterParameter { get; set; }

        public string ElementName { get; set; }

        public object FallbackValue { get; set; } = AvaloniaProperty.UnsetValue;

        public BindingMode Mode { get; set; }

        [ConstructorArgument("path")]
        public string Path { get; set; }

        public BindingPriority Priority { get; set; } = BindingPriority.LocalValue;

        public object Source { get; set; }

        public string StringFormat { get; set; }

        public RelativeSource RelativeSource { get; set; }
    }
}
