using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace Avalonia.Data;

/// <summary>
/// Defines properties common to both <see cref="Binding"/> and <c>CompiledBindingExtension</c>.
/// </summary>
public abstract class StandardBindingBase : BindingBase
{
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
    /// Gets or sets the binding priority.
    /// </summary>
    public BindingPriority Priority { get; set; }

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

    private protected (BindingMode, UpdateSourceTrigger) ResolveDefaultsFromMetadata(
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
