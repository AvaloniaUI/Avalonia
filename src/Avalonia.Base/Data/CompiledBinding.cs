using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;
using Avalonia.Data.Core.ExpressionNodes;

namespace Avalonia.Data;

/// <summary>
/// A binding which does not use reflection to access members.
/// </summary>
public class CompiledBinding : BindingBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompiledBinding"/> class.
    /// </summary>
    public CompiledBinding() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompiledBinding"/> class.
    /// </summary>
    /// <param name="path">The binding path.</param>
    public CompiledBinding(BindingPath path) => Path = path;
    
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
    public BindingPath? Path { get; set; }

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
    internal WeakReference<INameScope?>? NameScope { get; set; }

    internal override BindingExpressionBase CreateInstance(
        AvaloniaObject target,
        AvaloniaProperty? targetProperty,
        object? anchor)
    {
        var enableDataValidation = targetProperty?.GetMetadata(target).EnableDataValidation ?? false;
        var nodes = BindingPath.BuildExpressionNodes(Path, Source, targetProperty);

        // If the first node is an ISourceNode then allow it to select the source; otherwise
        // use the binding source if specified, falling back to the target.
        var source = nodes?.Count > 0 && nodes[0] is SourceNode sn
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
