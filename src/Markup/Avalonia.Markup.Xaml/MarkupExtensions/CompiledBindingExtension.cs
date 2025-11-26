using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;
using Avalonia.Data.Core.ExpressionNodes;
using Avalonia.Data.Core.Parsers;
using Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
    public sealed class CompiledBindingExtension
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

        /// <summary>
        /// Gets or sets the amount of time, in milliseconds, to wait before updating the binding 
        /// source after the value on the target changes.
        /// </summary>
        /// <remarks>
        /// There is no delay when the source is updated via <see cref="UpdateSourceTrigger.LostFocus"/> 
        /// or <see cref="BindingExpressionBase.UpdateSource"/>. Nor is there a delay when 
        /// <see cref="BindingMode.OneWayToSource"/> is active and a new source object is provided.
        /// </remarks>
        public int Delay { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IValueConverter"/> to use.
        /// </summary>
        public IValueConverter? Converter { get; set; }

        /// <summary>
        /// Gets or sets the culture in which to evaluate the converter.
        /// </summary>
        /// <value>The default value is null.</value>
        /// <remarks>
        /// If this property is not set then <see cref="CultureInfo.CurrentCulture"/> will be used.
        /// </remarks>
        [TypeConverter(typeof(CultureInfoIetfLanguageTagConverter))]
        public CultureInfo? ConverterCulture { get; set; }

        /// <summary>
        /// Gets or sets a parameter to pass to <see cref="Converter"/>.
        /// </summary>
        public object? ConverterParameter { get; set; }

        public Type? DataType { get; set; }

        /// <summary>
        /// Gets or sets the value to use when the binding is unable to produce a value.
        /// </summary>
        public object? FallbackValue { get; set; } = AvaloniaProperty.UnsetValue;

        /// <summary>
        /// Gets or sets the binding mode.
        /// </summary>
        public BindingMode Mode { get; set; }

        [ConstructorArgument("path")]
        public CompiledBindingPath? Path { get; set; }

        /// <summary>
        /// Gets or sets the binding priority.
        /// </summary>
        public BindingPriority Priority { get; set; }

        /// <summary>
        /// Gets or sets the source for the binding.
        /// </summary>
        public object? Source { get; set; } = AvaloniaProperty.UnsetValue;

        /// <summary>
        /// Gets or sets the string format.
        /// </summary>
        public string? StringFormat { get; set; }

        /// <summary>
        /// Gets or sets the value to use when the binding result is null.
        /// </summary>
        public object? TargetNullValue { get; set; } = AvaloniaProperty.UnsetValue;

        /// <summary>
        /// Gets or sets a value that determines the timing of binding source updates for
        /// <see cref="BindingMode.TwoWay"/> and <see cref="BindingMode.OneWayToSource"/> bindings.
        /// </summary>
        public UpdateSourceTrigger UpdateSourceTrigger { get; set; }

        internal WeakReference? DefaultAnchor { get; set; }
    }
}
