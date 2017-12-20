// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Data;
using Avalonia.Markup.Xaml.Data;

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
    using System;
    using Portable.Xaml.Markup;

    [MarkupExtensionReturnType(typeof(Binding))]
    public class TemplateBindingExtension : MarkupExtension
    {
        public TemplateBindingExtension()
        {
        }

        public TemplateBindingExtension(string path)
        {
            Path = path;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new Binding
            {
                Converter = Converter,
                ElementName = ElementName,
                Mode = Mode,
                RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
                Path = Path ?? string.Empty,
                Priority = Priority,
            };
        }

        public IValueConverter Converter { get; set; }

        public string ElementName { get; set; }

        public object FallbackValue { get; set; }

        public BindingMode Mode { get; set; }

        [ConstructorArgument("path")]
        public string Path { get; set; }

        public BindingPriority Priority { get; set; } = BindingPriority.TemplatedParent;
    }
}