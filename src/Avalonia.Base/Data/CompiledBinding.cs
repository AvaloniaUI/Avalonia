using System;
using Avalonia.Data.Core;
using Avalonia.Data.Core.ExpressionNodes;

namespace Avalonia.Data;

/// <summary>
/// A binding which does not use reflection to access members.
/// </summary>
public class CompiledBinding : StandardBindingBase
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
    /// Gets or sets the binding path.
    /// </summary>
    public BindingPath? Path { get; set; }

    internal override BindingExpressionBase Instance(
        AvaloniaObject target,
        AvaloniaProperty? targetProperty,
        object? anchor)
    {
        var enableDataValidation = targetProperty?.GetMetadata(target).EnableDataValidation ?? false;
        var nodes = Path?.CreateExpressionNodes(Source);

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
}
