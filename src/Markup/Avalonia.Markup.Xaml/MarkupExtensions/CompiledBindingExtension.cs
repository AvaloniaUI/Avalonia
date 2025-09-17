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
    public sealed class CompiledBindingExtension : BindingBase
    {
        public CompiledBindingExtension()
        {
            Path = new CompiledBindingPath();
        }

        public CompiledBindingExtension(CompiledBindingPath path)
        {
            Path = path;
        }

        public CompiledBindingExtension ProvideValue(IServiceProvider provider)
        {
            return new CompiledBindingExtension
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
                DefaultAnchor = new WeakReference(provider.GetDefaultAnchor()),
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
        public CompiledBindingPath Path { get; set; }

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

        internal override BindingExpressionBase CreateInstance(
            AvaloniaObject target,
            AvaloniaProperty? targetProperty,
            object? anchor)
        {
            var enableDataValidation = targetProperty?.GetMetadata(target).EnableDataValidation ?? false;
            return InstanceCore(target, targetProperty, anchor, enableDataValidation);
        }

        /// <summary>
        /// Hack for TreeDataTemplate to create a binding expression for an item.
        /// </summary>
        /// <param name="source">The item.</param>
        /// <remarks>
        /// Ideally we'd do this in a more generic way but didn't have time to refactor
        /// ITreeDataTemplate in time for 11.0. We should revisit this in 12.0.
        /// </remarks>
        // TODO12: Refactor
        internal BindingExpression CreateObservableForTreeDataTemplate(object source)
        {
            if (Source != AvaloniaProperty.UnsetValue)
                throw new NotSupportedException("Source bindings are not supported in this context.");

            var nodes = new List<ExpressionNode>();

            Path.BuildExpression(nodes, out var isRooted);

            if (isRooted)
                throw new NotSupportedException("Rooted binding paths are not supported in this context.");

            return new BindingExpression(
                source,
                nodes,
                FallbackValue,
                delay: TimeSpan.FromMilliseconds(Delay),
                converter: Converter,
                converterParameter: ConverterParameter,
                targetNullValue: TargetNullValue);
        }

        private BindingExpression InstanceCore(
            AvaloniaObject target,
            AvaloniaProperty? targetProperty,
            object? anchor,
            bool enableDataValidation)
        {
            var nodes = new List<ExpressionNode>();

            // Build the expression nodes from the binding path.
            Path.BuildExpression(nodes, out var isRooted);

            // If the binding isn't rooted (i.e. doesn't have a Source or start with $parent, $self,
            // #elementName etc.) then we need to add a data context source node.
            if (Source == AvaloniaProperty.UnsetValue && !isRooted)
                nodes.Insert(0, ExpressionNodeFactory.CreateDataContext(targetProperty));

            // If the first node is an ISourceNode then allow it to select the source; otherwise
            // use the binding source if specified, falling back to the target.
            var source = nodes.Count > 0 && nodes[0] is SourceNode sn
                ? sn.SelectSource(Source, target, anchor ?? DefaultAnchor?.Target)
                : Source != AvaloniaProperty.UnsetValue ? Source : target;

            var (mode, trigger) = ResolveDefaultsFromMetadata(target, targetProperty);

            return new BindingExpression(
                source,
                nodes,
                FallbackValue,
                delay: TimeSpan.FromMilliseconds(Delay),
                converter: Converter,
                converterCulture: ConverterCulture,
                converterParameter: ConverterParameter,
                enableDataValidation: enableDataValidation,
                mode: mode,
                priority: Priority,
                stringFormat: StringFormat,
                targetNullValue: TargetNullValue,
                targetProperty: targetProperty,
                targetTypeConverter: TargetTypeConverter.GetDefaultConverter(),
                updateSourceTrigger: trigger);
        }

        private (BindingMode, UpdateSourceTrigger) ResolveDefaultsFromMetadata(
            AvaloniaObject target,
            AvaloniaProperty? targetProperty)
        {
            var mode = Mode;
            var trigger = UpdateSourceTrigger == UpdateSourceTrigger.Default ?
                UpdateSourceTrigger.PropertyChanged : UpdateSourceTrigger;

            if (mode == BindingMode.Default)
            {
                if (targetProperty?.GetMetadata(target) is { } metadata)
                    mode = metadata.DefaultBindingMode;
                else
                    mode = BindingMode.OneWay;
            }

            return (mode, trigger);
        }
    }
}
