using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Text;
using Avalonia.Data.Converters;
using Avalonia.Data.Core.ExpressionNodes;
using Avalonia.Data.Core.Parsers;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avalonia.Utilities;

namespace Avalonia.Data.Core;

/// <summary>
/// A binding expression which accepts and produces (possibly boxed) object values.
/// </summary>
/// <remarks>
/// A <see cref="BindingExpression"/> represents a untyped binding which has been
/// instantiated on an object.
/// </remarks>
internal partial class BindingExpression : UntypedBindingExpressionBase, IDescription, IDisposable
{
    private static readonly List<ExpressionNode> s_emptyExpressionNodes = new();
    private readonly WeakReference<object?>? _source;
    private readonly BindingMode _mode;
    private readonly List<ExpressionNode> _nodes;
    private readonly TargetTypeConverter? _targetTypeConverter;
    private readonly UncommonFields? _uncommon;

    /// <summary>
    /// Initializes a new instance of the <see cref="BindingExpression"/> class.
    /// </summary>
    /// <param name="source">The source from which the value will be read.</param>
    /// <param name="nodes">The nodes representing the binding path.</param>
    /// <param name="fallbackValue">
    /// The fallback value. Pass <see cref="AvaloniaProperty.UnsetValue"/> for no fallback.
    /// </param>
    /// <param name="converter">The converter to use.</param>
    /// <param name="converterCulture">The converter culture to use.</param>
    /// <param name="converterParameter">The converter parameter.</param>
    /// <param name="enableDataValidation">
    /// Whether data validation should be enabled for the binding.
    /// </param>
    /// <param name="mode">The binding mode.</param>
    /// <param name="priority">The binding priority.</param>
    /// <param name="stringFormat">The format string to use.</param>
    /// <param name="targetNullValue">The null target value.</param>
    /// <param name="targetTypeConverter">
    /// A final type converter to be run on the produced value.
    /// </param>
    /// <param name="updateSourceTrigger">The trigger for updating the source value.</param>
    public BindingExpression(
        object? source,
        List<ExpressionNode>? nodes,
        object? fallbackValue,
        IValueConverter? converter = null,
        CultureInfo? converterCulture = null,
        object? converterParameter = null,
        bool enableDataValidation = false,
        BindingMode mode = BindingMode.OneWay,
        BindingPriority priority = BindingPriority.LocalValue,
        string? stringFormat = null,
        object? targetNullValue = null,
        TargetTypeConverter? targetTypeConverter = null,
        UpdateSourceTrigger updateSourceTrigger = UpdateSourceTrigger.PropertyChanged)
            : base(priority, enableDataValidation)
    {
        if (mode == BindingMode.Default)
            throw new ArgumentException("Binding mode cannot be Default.", nameof(mode));
        if (updateSourceTrigger == UpdateSourceTrigger.Default)
            throw new ArgumentException("UpdateSourceTrigger cannot be Default.", nameof(updateSourceTrigger));

        if (source == AvaloniaProperty.UnsetValue)
            source = null;

        _source = new(source);
        _mode = mode;
        _nodes = nodes ?? s_emptyExpressionNodes;
        _targetTypeConverter = targetTypeConverter;

        if (converter is not null ||
            converterCulture is not null ||
            converterParameter is not null ||
            fallbackValue != AvaloniaProperty.UnsetValue ||
            !string.IsNullOrWhiteSpace(stringFormat) ||
            (targetNullValue is not null && targetNullValue != AvaloniaProperty.UnsetValue) ||
            updateSourceTrigger is not UpdateSourceTrigger.PropertyChanged)
        {
            _uncommon = new()
            {
                _converter = converter,
                _converterCulture = converterCulture,
                _converterParameter = converterParameter,
                _fallbackValue = fallbackValue,
                _stringFormat = stringFormat switch
                {
                    string s when string.IsNullOrWhiteSpace(s) => null,
                    string s when !s.Contains('{') => $"{{0:{stringFormat}}}",
                    _ => stringFormat,
                },
                _targetNullValue = targetNullValue ?? AvaloniaProperty.UnsetValue,
                _updateSourceTrigger = updateSourceTrigger,
            };
        }

        IPropertyAccessorNode? leafAccessor = null;

        if (nodes is not null)
        {
            for (var i = 0; i < nodes.Count; ++i)
            {
                var node = nodes[i];
                node.SetOwner(this, i);
                if (node is IPropertyAccessorNode n)
                    leafAccessor = n;
            }
        }

        if (enableDataValidation)
            leafAccessor?.EnableDataValidation();
    }

    public override string Description
    {
        get
        {
            var b = new StringBuilder();
            LeafNode.BuildString(b, _nodes);
            return b.ToString();
        }
    }

    public Type? SourceType => (LeafNode as ISettableNode)?.ValueType;
    public IValueConverter? Converter => _uncommon?._converter;
    public CultureInfo ConverterCulture => _uncommon?._converterCulture ?? CultureInfo.CurrentCulture;
    public object? ConverterParameter => _uncommon?._converterParameter;
    public object? FallbackValue => _uncommon is not null ? _uncommon._fallbackValue : AvaloniaProperty.UnsetValue;
    public ExpressionNode LeafNode => _nodes[_nodes.Count - 1];
    public string? StringFormat => _uncommon?._stringFormat;
    public object? TargetNullValue => _uncommon?._targetNullValue ?? AvaloniaProperty.UnsetValue;
    public UpdateSourceTrigger UpdateSourceTrigger => _uncommon?._updateSourceTrigger ?? UpdateSourceTrigger.PropertyChanged;

    public override void UpdateSource()
    {
        if (_mode is BindingMode.TwoWay or BindingMode.OneWayToSource)
            WriteTargetValueToSource();
    }

    public override void UpdateTarget()
    {
        if (_nodes.Count == 0)
            return;

        var source = _nodes[0].Source;

        for (var i = 0; i < _nodes.Count; ++i)
            _nodes[i].SetSource(null, null);

        _nodes[0].SetSource(source, null);
    }

    /// <summary>
    /// Creates an <see cref="BindingExpression"/> from an expression tree.
    /// </summary>
    /// <typeparam name="TIn">The input type of the binding expression.</typeparam>
    /// <typeparam name="TOut">The output type of the binding expression.</typeparam>
    /// <param name="source">The source from which the binding value will be read.</param>
    /// <param name="expression">The expression representing the binding path.</param>
    /// <param name="converter">The converter to use.</param>
    /// <param name="converterCulture">The converter culture to use.</param>
    /// <param name="converterParameter">The converter parameter.</param>
    /// <param name="enableDataValidation">Whether data validation should be enabled for the binding.</param>
    /// <param name="fallbackValue">The fallback value.</param>
    /// <param name="mode">The binding mode.</param>
    /// <param name="priority">The binding priority.</param>
    /// <param name="targetNullValue">The null target value.</param>
    /// <param name="allowReflection">Whether to allow reflection for target type conversion.</param>
    [RequiresUnreferencedCode(TrimmingMessages.ExpressionNodeRequiresUnreferencedCodeMessage)]
    internal static BindingExpression Create<TIn, TOut>(
        TIn source,
        Expression<Func<TIn, TOut>> expression,
        IValueConverter? converter = null,
        CultureInfo? converterCulture = null,
        object? converterParameter = null,
        bool enableDataValidation = false,
        Optional<object?> fallbackValue = default,
        BindingMode mode = BindingMode.OneWay,
        BindingPriority priority = BindingPriority.LocalValue,
        object? targetNullValue = null,
        bool allowReflection = true)
            where TIn : class?
    {
        var nodes = BindingExpressionVisitor<TIn>.BuildNodes(expression, enableDataValidation);
        var fallback = fallbackValue.HasValue ? fallbackValue.Value : AvaloniaProperty.UnsetValue;

        return new BindingExpression(
            source,
            nodes,
            fallback,
            converter: converter,
            converterCulture: converterCulture,
            converterParameter: converterParameter,
            enableDataValidation: enableDataValidation,
            mode: mode,
            priority: priority,
            targetNullValue: targetNullValue,
            targetTypeConverter: allowReflection ?
                TargetTypeConverter.GetReflectionConverter() :
                TargetTypeConverter.GetDefaultConverter());
    }

    /// <summary>
    /// Called by an <see cref="ExpressionNode"/> belonging to this binding when its
    /// <see cref="ExpressionNode.Value"/> changes.
    /// </summary>
    /// <param name="nodeIndex">The <see cref="ExpressionNode.Index"/>.</param>
    /// <param name="value">The <see cref="ExpressionNode.Value"/>.</param>
    /// <param name="dataValidationError">
    /// The data validation error associated with the current value, if any.
    /// </param>
    internal void OnNodeValueChanged(int nodeIndex, object? value, Exception? dataValidationError)
    {
        Debug.Assert(value is not BindingNotification);
        Debug.Assert(nodeIndex >= 0 && nodeIndex < _nodes.Count);

        if (nodeIndex == _nodes.Count - 1)
        {
            // The leaf node has changed. If the binding mode is not OneWayToSource, publish the
            // value to the target.
            if (_mode != BindingMode.OneWayToSource)
            {
                var error = dataValidationError is not null ?
                    new BindingError(dataValidationError, BindingErrorType.DataValidationError) :
                    null;
                ConvertAndPublishValue(value, error);
            }

            // If the binding mode is OneTime, then stop the binding if a valid value was published.
            if (_mode == BindingMode.OneTime && GetValue() != AvaloniaProperty.UnsetValue)
                Stop();
        }
        else if (_mode == BindingMode.OneWayToSource && nodeIndex == _nodes.Count - 2 && value is not null)
        {
            // When the binding mode is OneWayToSource, we need to write the value to the source
            // when the object holding the source property changes; this is node before the leaf
            // node. First update the leaf node's source, then write the value to its property.
            _nodes[nodeIndex + 1].SetSource(value, dataValidationError);
            WriteTargetValueToSource();
        }
        else if (value is null)
        {
            OnNodeError(nodeIndex, "Value is null.");
        }
        else
        {
            _nodes[nodeIndex + 1].SetSource(value, dataValidationError);
        }
    }

    /// <summary>
    /// Called by an <see cref="ExpressionNode"/> belonging to this binding when an error occurs
    /// reading its value.
    /// </summary>
    /// <param name="nodeIndex">
    /// The <see cref="ExpressionNode.Index"/> or -1 if the source is null.
    /// </param>
    /// <param name="error">The error message.</param>
    internal void OnNodeError(int nodeIndex, string error)
    {
        // Set the source of all nodes after the one that errored to null. This needs to be done
        // for each node individually because setting the source to null will not result in
        // OnNodeValueChanged or OnNodeError being called.
        for (var i = nodeIndex + 1; i < _nodes.Count; ++i)
            _nodes[i].SetSource(null, null);

        if (_mode == BindingMode.OneWayToSource)
            return;

        var errorPoint = CalculateErrorPoint(nodeIndex);

        if (ShouldLogError(out var target))
            Log(target, error, errorPoint);

        // Clear the current value and publish the error.
        var bindingError = new BindingError(
            new BindingChainException(error, Description, errorPoint.ToString()),
            BindingErrorType.Error);
        ConvertAndPublishValue(AvaloniaProperty.UnsetValue, bindingError);
    }

    internal void OnDataValidationError(Exception error)
    {
        var bindingError = new BindingError(error, BindingErrorType.DataValidationError);
        PublishValue(UnchangedValue, bindingError);
    }

    internal override bool WriteValueToSource(object? value)
    {
        if (_nodes.Count == 0 || LeafNode is not ISettableNode setter || setter.ValueType is not { } type)
            return false;

        if (Converter is { } converter &&
            value != AvaloniaProperty.UnsetValue &&
            value != BindingOperations.DoNothing)
        {
            value = ConvertBack(converter, ConverterCulture, ConverterParameter, value, type);
        }

        if (value == BindingOperations.DoNothing)
            return true;

        // Use the target type converter to convert the value to the target type if necessary.
        if (_targetTypeConverter is not null)
        {
            if (_targetTypeConverter.TryConvert(value, type, ConverterCulture, out var converted))
            {
                value = converted;
            }
            else if (FallbackValue != AvaloniaProperty.UnsetValue)
            {
                value = FallbackValue;
            }
            else if (IsDataValidationEnabled)
            {
                var valueString = value?.ToString() ?? "(null)";
                var valueTypeName = value?.GetType().FullName ?? "null";
                var ex = new InvalidCastException(
                    $"Could not convert '{valueString}' ({valueTypeName}) to {type}.");
                OnDataValidationError(ex);
                return false;
            }
            else
            {
                return false;
            }
        }

        // Don't set the value if it's unchanged.
        if (TypeUtilities.IdentityEquals(LeafNode.Value, value, type))
            return true;

        try
        {
            return setter.WriteValueToSource(value, _nodes);
        }
        catch
        {
            return false;
        }
    }

    protected override bool ShouldLogError([NotNullWhen(true)] out AvaloniaObject? target)
    {
        if (!TryGetTarget(out target))
            return false;
        if (_nodes.Count > 0 && _nodes[0] is SourceNode sourceNode)
            return sourceNode.ShouldLogErrors(target);
        return true;
    }

    protected override void StartCore()
    {
        if (_source?.TryGetTarget(out var source) == true)
        {
            if (_nodes.Count > 0)
                _nodes[0].SetSource(source, null);
            else
                ConvertAndPublishValue(source, null);

            if (_mode is BindingMode.TwoWay or BindingMode.OneWayToSource &&
                TryGetTarget(out var target) &&
                TargetProperty is not null)
            {
                var trigger = UpdateSourceTrigger;

                if (trigger is UpdateSourceTrigger.PropertyChanged)
                    target.PropertyChanged += OnTargetPropertyChanged;
                else if (trigger is UpdateSourceTrigger.LostFocus && target is IInputElement ie)
                    ie.LostFocus += OnTargetLostFocus;
            }
        }
        else
        {
            OnNodeError(-1, "Binding Source is null.");
        }
    }

    protected override void StopCore()
    {
        foreach (var node in _nodes)
            node.Reset();

        if (_mode is BindingMode.TwoWay or BindingMode.OneWayToSource &&
            TryGetTarget(out var target))
        {
            var trigger = UpdateSourceTrigger;

            if (trigger is UpdateSourceTrigger.PropertyChanged)
                target.PropertyChanged -= OnTargetPropertyChanged;
            else if (trigger is UpdateSourceTrigger.LostFocus && target is IInputElement ie)
                ie.LostFocus -= OnTargetLostFocus;
        }
    }

    private string CalculateErrorPoint(int nodeIndex)
    {
        // Build a string describing the binding chain up to the node that errored.
        var result = new StringBuilder();

        if (nodeIndex >= 0)
            _nodes[nodeIndex].BuildString(result);
        else
            result.Append("(source)");

        return result.ToString();
    }

    private void Log(AvaloniaObject target, string error, string errorPoint, LogEventLevel level = LogEventLevel.Warning)
    {
        if (!Logger.TryGet(level, LogArea.Binding, out var log))
            return;

        log.Log(
            target,
            "An error occurred binding {Property} to {Expression} at {ExpressionErrorPoint}: {Message}",
            (object?)TargetProperty ?? "(unknown)",
            Description,
            errorPoint,
            error);
    }

    private void ConvertAndPublishValue(object? value, BindingError? error)
    {
        var isTargetNullValue = false;

        // All values other than UnsetValue and DoNothing should be passed to the converter.
        if (Converter is { } converter &&
            value != AvaloniaProperty.UnsetValue &&
            value != BindingOperations.DoNothing)
        {
            value = Convert(converter, ConverterCulture, ConverterParameter, value, TargetType, ref error);
        }

        // Check this here as the converter may return DoNothing.
        if (value == BindingOperations.DoNothing)
            return;

        // TargetNullValue only applies when the value is null: UnsetValue indicates that there
        // was a binding error so we don't want to use TargetNullValue in that case.
        if (value is null && TargetNullValue != AvaloniaProperty.UnsetValue)
        {
            value = ConvertFallback(TargetNullValue, nameof(TargetNullValue));
            isTargetNullValue = true;
        }

        // If we have a value, try to convert it to the target type.
        if (value != AvaloniaProperty.UnsetValue)
        {
            if (StringFormat is { } stringFormat &&
                (TargetType == typeof(object) || TargetType == typeof(string)) &&
                !isTargetNullValue)
            {
                // The string format applies if we're targeting a type that can accept a string
                // and the value isn't the TargetNullValue.
                value = string.Format(ConverterCulture, stringFormat, value);
            }
            else if (_targetTypeConverter is not null)
            {
                // Otherwise, if we have a target type converter, convert the value to the target type.
                value = ConvertFrom(_targetTypeConverter, value, ref error);
            }
        }

        // FallbackValue applies if the result from the binding, converter or target type converter
        // is UnsetValue.
        if (value == AvaloniaProperty.UnsetValue && FallbackValue != AvaloniaProperty.UnsetValue)
            value = ConvertFallback(FallbackValue, nameof(FallbackValue));

        // Publish the value.
        PublishValue(value, error);
    }

    private void WriteTargetValueToSource()
    {
        Debug.Assert(_mode is BindingMode.TwoWay or BindingMode.OneWayToSource);

        if (TryGetTarget(out var target) &&
            TargetProperty is not null &&
            target.GetValue(TargetProperty) is var value &&
            !TypeUtilities.IdentityEquals(value, LeafNode.Value, TargetType))
        {
            WriteValueToSource(value);
        }
    }

    private void OnTargetLostFocus(object? sender, RoutedEventArgs e)
    {
        Debug.Assert(UpdateSourceTrigger is UpdateSourceTrigger.LostFocus);

        WriteTargetValueToSource();
    }

    private void OnTargetPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        Debug.Assert(_mode is BindingMode.TwoWay or BindingMode.OneWayToSource);
        Debug.Assert(UpdateSourceTrigger is UpdateSourceTrigger.PropertyChanged);

        if (e.Property == TargetProperty)
            WriteValueToSource(e.NewValue);
    }

    private object? ConvertFallback(object? fallback, string fallbackName)
    {
        if (_targetTypeConverter is null || TargetType == typeof(object) || fallback == AvaloniaProperty.UnsetValue)
            return fallback;

        if (_targetTypeConverter.TryConvert(fallback, TargetType, ConverterCulture, out var result))
            return result;

        if (TryGetTarget(out var target))
            Log(target, $"Could not convert {fallbackName} '{fallback}' to '{TargetType}'.", LogEventLevel.Error);

        return AvaloniaProperty.UnsetValue;
    }

    private object? ConvertFrom(TargetTypeConverter? converter, object? value, ref BindingError? error)
    {
        if (converter is null)
            return value;

        if (converter.TryConvert(value, TargetType, ConverterCulture, out var result))
            return result;

        var valueString = value?.ToString() ?? "(null)";
        var valueTypeName = value?.GetType().FullName ?? "null";
        var message = $"Could not convert '{valueString}' ({valueTypeName}) to '{TargetType}'.";

        if (ShouldLogError(out var target))
            Log(target, message, LogEventLevel.Warning);

        error = new(new InvalidCastException(message), BindingErrorType.Error);
        return AvaloniaProperty.UnsetValue;
    }

    /// <summary>
    /// Uncommonly used fields are separated out to reduce memory usage.
    /// </summary>
    private class UncommonFields
    {
        public IValueConverter? _converter;
        public object? _converterParameter;
        public CultureInfo? _converterCulture;
        public object? _fallbackValue;
        public string? _stringFormat;
        public object? _targetNullValue;
        public UpdateSourceTrigger _updateSourceTrigger;
    }
}
