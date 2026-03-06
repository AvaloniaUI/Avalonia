using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;
using Avalonia.Data.Core.ExpressionNodes;
using Avalonia.Data.Core.Parsers;

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
    public sealed class CompiledBindingExtension : CompiledBinding
    {
        public CompiledBindingExtension()
        {
        }

        public CompiledBindingExtension(CompiledBindingPath path)
        {
            Path = path;
        }

        public CompiledBinding ProvideValue(IServiceProvider? provider)
        {
            return new CompiledBinding
            {
                Path = Path,
                Delay = Delay,
                Converter = Converter,
                ConverterCulture = ConverterCulture,
                ConverterParameter = ConverterParameter,
                TargetNullValue = TargetNullValue,
                FallbackValue = FallbackValue,
                Mode = Mode,
                Priority = Priority,
                StringFormat = StringFormat,
                Source = Source,
                DefaultAnchor = new WeakReference(provider?.GetDefaultAnchor()),
                UpdateSourceTrigger = UpdateSourceTrigger,
            };
        }

        public Type? DataType { get; set; }
    }
}
