using Avalonia.Data;
using System;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel;
using System.Globalization;

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
    [RequiresUnreferencedCode(TrimmingMessages.ReflectionBindingRequiresUnreferencedCodeMessage)]
    public class ReflectionBindingExtension
    {
        public ReflectionBindingExtension()
        {
        }

        public ReflectionBindingExtension(string path)
        {
            Path = path;
        }

        public Binding ProvideValue(IServiceProvider serviceProvider)
        {
            return new Binding
            {
                TypeResolver = serviceProvider.ResolveType,
                Converter = Converter,
                ConverterCulture = ConverterCulture,
                ConverterParameter = ConverterParameter,
                ElementName = ElementName,
                FallbackValue = FallbackValue,
                Mode = Mode,
                Path = Path,
                Priority = Priority,
                Source = Source,
                StringFormat = StringFormat,
                RelativeSource = RelativeSource,
                DefaultAnchor = new WeakReference(serviceProvider.GetDefaultAnchor()),
                TargetNullValue = TargetNullValue,
                NameScope = new WeakReference<INameScope?>(serviceProvider.GetService<INameScope>())
            };
        }

        public IValueConverter? Converter { get; set; }

        [TypeConverter(typeof(CultureInfoIetfLanguageTagConverter))]
        public CultureInfo? ConverterCulture { get; set; }

        public object? ConverterParameter { get; set; }

        public string? ElementName { get; set; }

        public object? FallbackValue { get; set; } = AvaloniaProperty.UnsetValue;

        public BindingMode Mode { get; set; }

        [ConstructorArgument("path")]
        public string Path { get; set; } = "";

        public BindingPriority Priority { get; set; } = BindingPriority.LocalValue;

        public object? Source { get; set; } = AvaloniaProperty.UnsetValue;

        public string? StringFormat { get; set; }

        public RelativeSource? RelativeSource { get; set; }

        public object? TargetNullValue { get; set; } = AvaloniaProperty.UnsetValue;
    }
}
