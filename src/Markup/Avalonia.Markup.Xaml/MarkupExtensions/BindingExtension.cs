// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Data;
using System;

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
    using Avalonia.Controls;
    using Avalonia.Data.Converters;
    using Avalonia.Markup.Data;
    using Avalonia.Styling;
    using System.ComponentModel;

    public class BindingExtension
    {
        public BindingExtension()
        {
        }

        public BindingExtension(string path)
        {
            Path = path;
        }

        public Binding ProvideValue(IServiceProvider serviceProvider)
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
                DefaultAnchor = new WeakReference(GetDefaultAnchor(descriptorContext)),
                NameScope = new WeakReference<INameScope>(serviceProvider.GetService<INameScope>())
            };
        }

        private static object GetDefaultAnchor(IServiceProvider context)
        {
            // If the target is not a control, so we need to find an anchor that will let us look
            // up named controls and style resources. First look for the closest IControl in
            // the context.
            object anchor = context.GetFirstParent<IControl>();

            // If a control was not found, then try to find the highest-level style as the XAML
            // file could be a XAML file containing only styles.
            return anchor ??
                    context.GetService<IRootObjectProvider>()?.RootObject as IStyle ??
                    context.GetLastParent<IStyle>();
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
