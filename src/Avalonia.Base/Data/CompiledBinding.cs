using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;
using Avalonia.Data.Core.ExpressionNodes;
using Avalonia.Data.Core.Parsers;
using Avalonia.Utilities;

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
    public CompiledBinding(CompiledBindingPath path) => Path = path;

    /// <summary>
    /// Creates a <see cref="CompiledBinding"/> from a lambda expression.
    /// The binding will use the target's DataContext as the source.
    /// </summary>
    /// <typeparam name="TIn">The input type of the binding expression.</typeparam>
    /// <typeparam name="TOut">The output type of the binding expression.</typeparam>
    /// <param name="expression">
    /// The lambda expression representing the binding path
    /// (e.g., <c>vm => vm.PropertyName</c>).
    /// </param>
    /// <param name="converter">Optional value converter to transform values between source and target.</param>
    /// <param name="mode">
    /// The binding mode. Default is <see cref="BindingMode.Default"/> which resolves to the
    /// property's default binding mode.
    /// </param>
    /// <returns>A configured <see cref="CompiledBinding"/> instance ready to be applied to a property.</returns>
    /// <exception cref="ExpressionParseException">
    /// Thrown when the expression contains unsupported operations or invalid syntax for binding expressions.
    /// </exception>
    /// <remarks>
    /// This method uses <see cref="BindingExpressionVisitor{TIn}"/> to convert the lambda expression
    /// into a strongly-typed <see cref="CompiledBindingPath"/>. The resulting binding avoids reflection
    /// for property access, providing better performance than reflection-based bindings.
    ///
    /// Supported expressions include:
    /// <list type="bullet">
    /// <item>Property access: <c>x => x.Property</c></item>
    /// <item>Nested properties: <c>x => x.Property.Nested</c></item>
    /// <item>Indexers: <c>x => x.Items[0]</c></item>
    /// <item>Type casts: <c>x => ((DerivedType)x).Property</c></item>
    /// <item>Logical NOT: <c>x => !x.BoolProperty</c></item>
    /// <item>Stream bindings: <c>x => x.TaskProperty</c> (Task/Observable)</item>
    /// <item>AvaloniaProperty access: <c>x => x[MyProperty]</c></item>
    /// </list>
    /// </remarks>
    [RequiresDynamicCode(TrimmingMessages.ExpressionNodeRequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(TrimmingMessages.ExpressionNodeRequiresUnreferencedCodeMessage)]
    public static CompiledBinding Create<TIn, TOut>(
        Expression<Func<TIn, TOut>> expression,
        IValueConverter? converter = null,
        BindingMode mode = BindingMode.Default)
    {
        var path = BindingExpressionVisitor<TIn>.BuildPath(expression);
        return new CompiledBinding(path)
        {
            Converter = converter,
            Mode = mode
        };
    }

    /// <summary>
    /// Creates a <see cref="CompiledBinding"/> from a lambda expression with an explicit source object.
    /// </summary>
    /// <typeparam name="TIn">The input type of the binding expression.</typeparam>
    /// <typeparam name="TOut">The output type of the binding expression.</typeparam>
    /// <param name="source">The source object for the binding.</param>
    /// <param name="expression">
    /// The lambda expression representing the binding path
    /// (e.g., <c>vm => vm.PropertyName</c>).
    /// </param>
    /// <param name="converter">Optional value converter to transform values between source and target.</param>
    /// <param name="mode">
    /// The binding mode. Default is <see cref="BindingMode.Default"/> which resolves to the
    /// property's default binding mode.
    /// </param>
    /// <returns>A configured <see cref="CompiledBinding"/> instance ready to be applied to a property.</returns>
    /// <exception cref="ExpressionParseException">
    /// Thrown when the expression contains unsupported operations or invalid syntax for binding expressions.
    /// </exception>
    /// <remarks>
    /// This overload allows specifying an explicit binding source instead of using the target's DataContext.
    /// See <see cref="Create{TIn, TOut}(Expression{Func{TIn, TOut}}, IValueConverter?, BindingMode)"/>
    /// for more details on supported expression syntax.
    /// </remarks>
    [RequiresDynamicCode(TrimmingMessages.ExpressionNodeRequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(TrimmingMessages.ExpressionNodeRequiresUnreferencedCodeMessage)]
    public static CompiledBinding Create<TIn, TOut>(
        TIn source,
        Expression<Func<TIn, TOut>> expression,
        IValueConverter? converter = null,
        BindingMode mode = BindingMode.Default)
        where TIn : class
    {
        var path = BindingExpressionVisitor<TIn>.BuildPath(expression);
        return new CompiledBinding(path)
        {
            Source = source,
            Converter = converter,
            Mode = mode
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
    internal WeakReference<INameScope?>? NameScope { get; set; }

    internal override BindingExpressionBase CreateInstance(
        AvaloniaObject target,
        AvaloniaProperty? targetProperty,
        object? anchor)
    {
        var enableDataValidation = targetProperty?.GetMetadata(target).EnableDataValidation ?? false;
        var nodes = new List<ExpressionNode>();
        var isRooted = false;

        Path?.BuildExpression(nodes, out isRooted);

        // If the binding isn't rooted (i.e. doesn't have a Source or start with $parent, $self,
        // #elementName etc.) then we need to add a data context source node.
        if (Source == AvaloniaProperty.UnsetValue && !isRooted)
            nodes.Insert(0, ExpressionNodeFactory.CreateDataContext(targetProperty));

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
