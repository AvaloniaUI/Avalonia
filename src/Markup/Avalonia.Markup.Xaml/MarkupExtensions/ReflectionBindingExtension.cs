using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
    [RequiresUnreferencedCode(TrimmingMessages.ReflectionBindingRequiresUnreferencedCodeMessage)]
#if NET8_0_OR_GREATER
    [RequiresDynamicCode(TrimmingMessages.ReflectionBindingRequiresDynamicCodeMessage)]
#endif
    public sealed class ReflectionBindingExtension
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReflectionBinding"/> class.
        /// </summary>
        public ReflectionBindingExtension() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReflectionBinding"/> class.
        /// </summary>
        /// <param name="path">The binding path.</param>
        public ReflectionBindingExtension(string path)
        {
            Path = path;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReflectionBinding"/> class.
        /// </summary>
        /// <param name="path">The binding path.</param>
        /// <param name="mode">The binding mode.</param>
        public ReflectionBindingExtension(string path, BindingMode mode)
        {
            Path = path;
            Mode = mode;
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

        /// <summary>
        /// Gets or sets the name of the element to use as the binding source.
        /// </summary>
        public string? ElementName { get; set; }

        /// <summary>
        /// Gets or sets the value to use when the binding is unable to produce a value.
        /// </summary>
        public object? FallbackValue { get; set; } = AvaloniaProperty.UnsetValue;

        /// <summary>
        /// Gets or sets the binding mode.
        /// </summary>
        public BindingMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the binding path.
        /// </summary>
        public string Path { get; set; } = "";

        /// <summary>
        /// Gets or sets the binding priority.
        /// </summary>
        public BindingPriority Priority { get; set; }

        /// <summary>
        /// Gets or sets the relative source for the binding.
        /// </summary>
        public RelativeSource? RelativeSource { get; set; }

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

        public ReflectionBinding ProvideValue(IServiceProvider serviceProvider)
        {
            return new ReflectionBinding
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
                Delay = Delay,
                Source = Source,
                StringFormat = StringFormat,
                RelativeSource = RelativeSource,
                DefaultAnchor = new WeakReference(serviceProvider.GetDefaultAnchor()),
                TargetNullValue = TargetNullValue,
                NameScope = new WeakReference<INameScope?>(serviceProvider.GetService<INameScope>()),
                UpdateSourceTrigger = UpdateSourceTrigger,
            };
        }
    }
}
