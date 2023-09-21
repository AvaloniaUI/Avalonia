using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Text;
using Avalonia.Data.Converters;
using Avalonia.Data.Core.ExpressionNodes;
using Avalonia.Data.Core.Parsers;
using Avalonia.Logging;

namespace Avalonia.Data.Core;

/// <summary>
/// A binding expression which accepts and produces (possibly boxed) object values.
/// </summary>
/// <remarks>
/// A <see cref="BindingExpression"/> represents a untyped binding which has been
/// instantiated on an object.
/// </remarks>
internal class BindingExpression : IObservable<object?>,
    IObserver<object?>,
    IDescription,
    IDisposable
{
    private static readonly WeakReference<object?> NullReference = new(null);
    private readonly WeakReference<object?>? _source;
    private readonly WeakReference<AvaloniaObject?> _target;
    private readonly BindingMode _mode;
    private readonly IReadOnlyList<ExpressionNode> _nodes;
    private readonly AvaloniaProperty? _targetProperty;
    private readonly TargetTypeConverter? _targetTypeConverter;
    private readonly bool _enableDataValidation;
    private IObserver<object?>? _observer;
    private WeakReference<object?>? _value;
    private UncommonFields? _uncommon;

    /// <summary>
    /// Initializes a new instance of the <see cref="BindingExpression"/> class.
    /// </summary>
    /// <param name="source">The source from which the value will be read.</param>
    /// <param name="nodes">The nodes representing the binding path.</param>
    /// <param name="fallbackValue">
    /// The fallback value. Pass <see cref="AvaloniaProperty.UnsetValue"/> for no fallback.
    /// </param>
    /// <param name="converter">The converter to use.</param>
    /// <param name="converterParameter">The converter parameter.</param>
    /// <param name="enableDataValidation">
    /// Whether data validation should be enabled for the binding.
    /// </param>
    /// <param name="mode">The binding mode.</param>
    /// <param name="stringFormat">The format string to use.</param>
    /// <param name="target">The target to which the value will be written.</param>
    /// <param name="targetNullValue">The null target value.</param>
    /// <param name="targetProperty">The target property.</param>
    /// <param name="targetTypeConverter">
    /// A final type converter to be run on the produced value.
    /// </param>
    public BindingExpression(
        object? source,
        IReadOnlyList<ExpressionNode> nodes,
        object? fallbackValue,
        IValueConverter? converter = null,
        object? converterParameter = null,
        bool enableDataValidation = false,
        BindingMode mode = BindingMode.OneWay,
        string? stringFormat = null,
        AvaloniaObject? target = null,
        object? targetNullValue = null,
        AvaloniaProperty? targetProperty = null,
        TargetTypeConverter? targetTypeConverter = null)
    {
        if (mode == BindingMode.Default)
            throw new ArgumentException("Binding mode cannot be Default.", nameof(mode));
        if (target is null && mode is BindingMode.TwoWay or BindingMode.OneWayToSource)
            throw new ArgumentException("Target cannot be null for TwoWay or OneWayToSource bindings.", nameof(target));
        if (targetProperty is null && mode is BindingMode.TwoWay or BindingMode.OneWayToSource)
            throw new ArgumentException("Target property cannot be null for TwoWay or OneWayToSource bindings.", nameof(target));

        if (source == AvaloniaProperty.UnsetValue)
            source = null;

        _source = new(source);
        _target = new(target);
        _targetProperty = targetProperty;
        _mode = mode;
        _nodes = nodes;
        _targetTypeConverter = targetTypeConverter;
        _enableDataValidation = enableDataValidation;

        if (converter is not null ||
            converterParameter is not null ||
            fallbackValue != AvaloniaProperty.UnsetValue ||
            (targetNullValue is not null && targetNullValue != AvaloniaProperty.UnsetValue) ||
            !string.IsNullOrWhiteSpace(stringFormat))
        {
            _uncommon = new()
            {
                _converter = converter,
                _converterParameter = converterParameter,
                _fallbackValue = fallbackValue,
                _targetNullValue = targetNullValue ?? AvaloniaProperty.UnsetValue,
                _stringFormat = stringFormat switch
                {
                    string s when string.IsNullOrWhiteSpace(s) => null,
                    string s when !s.Contains('{') => $"{{0:{stringFormat}}}",
                    _ => stringFormat,
                },
            };
        }

        IPropertyAccessorNode? leafAccessor = null;

        for (var i = 0; i < nodes.Count; ++i)
        {
            var node = nodes[i];
            node.SetOwner(this, i);
            if (node is IPropertyAccessorNode n)
                leafAccessor = n;
        }

        if (enableDataValidation)
            leafAccessor?.EnableDataValidation();
    }

    public string Description
    {
        get
        {
            var b = new StringBuilder();
            LeafNode.BuildString(b, _nodes);
            return b.ToString();
        }
    }

    public Type? SourceType => (LeafNode as ISettableNode)?.ValueType;
    public Type TargetType => _targetProperty?.PropertyType ?? typeof(object);
    public IValueConverter? Converter => _uncommon?._converter;
    public object? ConverterParameter => _uncommon?._converterParameter;
    public object? FallbackValue => _uncommon is not null ? _uncommon._fallbackValue : AvaloniaProperty.UnsetValue;
    public object? TargetNullValue => _uncommon?._targetNullValue ?? AvaloniaProperty.UnsetValue;
    public ExpressionNode LeafNode => _nodes[_nodes.Count - 1];
    public string? StringFormat => _uncommon?._stringFormat;

    /// <summary>
    /// Writes the specified value to the binding source if possible.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <returns>
    /// True if the value could be written to the binding source; otherwise false.
    /// </returns>
    public bool SetValue(object? value)
    {
        if (_nodes.Count == 0 || LeafNode is not ISettableNode setter || setter.ValueType is not { } type)
            return false;

        if (Converter is not null)
            value = Converter.ConvertBack(value, type, ConverterParameter, CultureInfo.CurrentCulture);

        if (value == BindingOperations.DoNothing)
            return true;

        // If the value is the same as the last value, then don't set the value.
        if (_value is not null && _value.TryGetTarget(out var lastValue) && Equals(value, lastValue))
            return true;

        // If the value is null and the last value was null, then don't set the value. This is a
        // separate step from the above because WeakReference<T>.TryGetTarget() returns false for
        // null references.
        if (value is null && _value == NullReference)
            return true;

        // Use the target type converter to convert the value to the target type if necessary.
        if (_targetTypeConverter is not null)
        {
            if (_targetTypeConverter.TryConvert(value, type, CultureInfo.CurrentCulture, out var converted))
            {
                value = converted;
            }
            else if (FallbackValue != AvaloniaProperty.UnsetValue)
            {
                value = FallbackValue;
            }
            else if (_enableDataValidation)
            {
                var valueString = value?.ToString() ?? "(null)";
                var valueTypeName = value?.GetType().FullName ?? "null";
                var ex = new InvalidCastException(
                    $"Cannot convert '{valueString}' ({valueTypeName}) to {type}.");
                _observer?.OnNext(new BindingNotification(ex, BindingErrorType.DataValidationError));
                return false;
            }
            else
            {
                return false;
            }
        }

        try
        {
            return setter.WriteValueToSource(value, _nodes);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Creates an <see cref="BindingExpression"/> from an expression tree.
    /// </summary>
    /// <typeparam name="TIn">The input type of the binding expression.</typeparam>
    /// <typeparam name="TOut">The output type of the binding expression.</typeparam>
    /// <param name="source">The source from which the binding value will be read.</param>
    /// <param name="expression">The expression representing the binding path.</param>
    /// <param name="converter">The converter to use.</param>
    /// <param name="converterParameter">The converter parameter.</param>
    /// <param name="enableDataValidation">Whether data validation should be enabled for the binding.</param>
    /// <param name="fallbackValue">The fallback value.</param>
    /// <param name="mode">The binding mode.</param>
    /// <param name="target">The target to which the value will be written.</param>
    /// <param name="targetNullValue">The null target value.</param>
    /// <param name="targetProperty">The target property.</param>
    [RequiresUnreferencedCode(TrimmingMessages.ExpressionNodeRequiresUnreferencedCodeMessage)]
    public static BindingExpression Create<TIn, TOut>(
        TIn source,
        Expression<Func<TIn, TOut>> expression,
        IValueConverter? converter = null,
        object? converterParameter = null,
        bool enableDataValidation = false,
        Optional<object?> fallbackValue = default,
        BindingMode mode = BindingMode.OneWay,
        AvaloniaObject? target = null,
        object? targetNullValue = null,
        AvaloniaProperty? targetProperty = null)
            where TIn : class?
    {
        var nodes = BindingExpressionVisitor<TIn>.BuildNodes(expression, enableDataValidation);
        var fallback = fallbackValue.HasValue ? fallbackValue.Value : AvaloniaProperty.UnsetValue;

        return new BindingExpression(
            source,
            nodes,
            fallback,
            converter: converter,
            converterParameter: converterParameter,
            enableDataValidation: enableDataValidation,
            mode: mode,
            target: target,
            targetNullValue: targetNullValue,
            targetProperty: targetProperty,
            targetTypeConverter: TargetTypeConverter.GetReflectionConverter());
    }

    /// <summary>
    /// Implements the disposable returned by <see cref="IObservable{T}.Subscribe(IObserver{T})"/>.
    /// </summary>
    void IDisposable.Dispose()
    {
        if (_observer is null)
            return;
        _observer = null;
        Stop();
    }

    IDisposable IObservable<object?>.Subscribe(IObserver<object?> observer)
    {
        if (_observer is not null)
            throw new InvalidOperationException(
                $"An {nameof(BindingExpression)} may only have a single subscriber.");

        _observer = observer ?? throw new ArgumentNullException(nameof(observer));
        Start();
        return this;
    }

    void IObserver<object?>.OnCompleted() { }
    void IObserver<object?>.OnError(Exception error) { }
    void IObserver<object?>.OnNext(object? value) => SetValue(value);

    /// <summary>
    /// Called by an <see cref="ExpressionNode"/> belonging to this binding when its
    /// <see cref="ExpressionNode.Value"/> changes.
    /// </summary>
    /// <param name="nodeIndex">The <see cref="ExpressionNode.Index"/>.</param>
    /// <param name="value">The <see cref="ExpressionNode.Value"/>.</param>
    internal void OnNodeValueChanged(int nodeIndex, object? value)
    {
        if (nodeIndex == _nodes.Count - 1)
        {
            // The leaf node has changed. If the binding mode is not OneWayToSource, publish the
            // value to the target.
            if (_mode != BindingMode.OneWayToSource)
                PublishValue();

            // If the binding mode is OneTime, then stop the binding.
            if (_mode == BindingMode.OneTime)
                Stop();
        }
        else if (_mode == BindingMode.OneWayToSource && nodeIndex == _nodes.Count - 2 && value is not null)
        {
            // When the binding mode is OneWayToSource, we need to write the value to the source
            // when the object holding the source property changes; this is node before the leaf
            // node. First update the leaf node's source, then write the value to its property.
            _nodes[nodeIndex + 1].SetSource(value);
            WriteTargetValueToSource();
        }
        else if (value is null)
        {
            OnNodeError(nodeIndex, "Value is null.");
        }
        else
        {
            _nodes[nodeIndex + 1].SetSource(value);
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
        _value = null;

        // Set the source of all nodes after the one that errored to null. This needs to be done
        // for each node individually because setting the source to null will not result in
        // OnNodeValueChanged or OnNodeError being called.
        for (var i = nodeIndex + 1; i < _nodes.Count; ++i)
            _nodes[i].SetSource(null);

        if (_observer is null || _mode == BindingMode.OneWayToSource)
            return;

        // Build a string describing the binding chain up to the node that errored.
        var errorPoint = new StringBuilder();

        if (nodeIndex >= 0)
            _nodes[nodeIndex].BuildString(errorPoint);
        else
            errorPoint.Append("(source)");

        LogWarningIfNecessary(error, errorPoint.ToString());

        var e = new BindingChainException(error, Description, errorPoint.ToString());
        _observer.OnNext(new BindingNotification(
            e, 
            BindingErrorType.Error, 
            ConvertFallback(FallbackValue, nameof(FallbackValue))));
    }

    private void LogWarningIfNecessary(string error, string errorPoint)
    {
        if (!_target.TryGetTarget(out var target))
            return;

        if (_nodes.Count > 0 &&
            _nodes[0] is SourceNode sourceNode &&
            !sourceNode.ShouldLogErrors(target))
            return;

        Log(target, error, errorPoint);
    }

    private void Log(AvaloniaObject target, string error, LogEventLevel level = LogEventLevel.Warning)
    {
        if (!Logger.TryGet(level, LogArea.Binding, out var log))
            return;

        log.Log(
            target,
            "An error occurred binding {Property} to {Expression}: {Message}",
            (object?)_targetProperty ?? "(unknown)",
            Description,
            error);
    }

    private void Log(AvaloniaObject target, string error, string errorPoint, LogEventLevel level = LogEventLevel.Warning)
    {
        if (!Logger.TryGet(level, LogArea.Binding, out var log))
            return;

        log.Log(
            target,
            "An error occurred binding {Property} to {Expression} at {ExpressionErrorPoint}: {Message}",
            (object?)_targetProperty ?? "(unknown)",
            Description,
            errorPoint,
            error);
    }

    private void Start()
    {
        if (_observer is null)
            return;

        if (_source?.TryGetTarget(out var source) == true)
        {
            if (_nodes.Count > 0)
                _nodes[0].SetSource(source);
            else
                _observer.OnNext(source);

            if (_mode is BindingMode.TwoWay or BindingMode.OneWayToSource &&
                _target.TryGetTarget(out var target) &&
                _targetProperty is not null)
            {
                if (_mode is BindingMode.OneWayToSource)
                    SetValue(target.GetValue(_targetProperty));

                target.PropertyChanged += OnTargetPropertyChanged;
            }
        }
        else
        {
            OnNodeError(-1, "Binding Source is null.");
        }
    }

    private void Stop()
    {
        foreach (var node in _nodes)
            node.Reset();

        if (_mode is BindingMode.TwoWay or BindingMode.OneWayToSource &&
            _target.TryGetTarget(out var target))
        {
            target.PropertyChanged -= OnTargetPropertyChanged;
        }
    }

    private void PublishValue()
    {
        if (_observer is null)
            return;

        // The value can be a simple value or a BindingNotification. As we move through this method
        // we'll keep `notification` updated with the value and current error state by calling
        // `UpdateAndUnwrap`.
        var valueOrNotification = _nodes.Count > 0 ? _nodes[_nodes.Count - 1].Value : null;
        var value = BindingNotification.ExtractValue(valueOrNotification);
        var notification = valueOrNotification as BindingNotification;
        var isTargetNullValue = false;

        // All values other than DoNothing should be passed to the converter.
        if (value != BindingOperations.DoNothing && Converter is { } converter)
        {
            value = UpdateAndUnwrap(
                Convert(
                    converter,
                    ConverterParameter,
                    value,
                    _targetProperty?.PropertyType ?? typeof(object)),
                ref notification);
        }

        // Check this here as the converter may return DoNothing.
        if (value == BindingOperations.DoNothing)
            return;

        // TargetNullValue only applies when the value is null: UnsetValue indicates that there
        // was a binding error so we don't want to use TargetNullValue in that case.
        if (value is null && TargetNullValue != AvaloniaProperty.UnsetValue)
        {
            value = UpdateAndUnwrap(ConvertFallback(TargetNullValue, nameof(TargetNullValue)), ref notification);
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
                value = string.Format(CultureInfo.CurrentCulture, stringFormat, value);
            }
            else if (_targetTypeConverter is not null && value is not null)
            {
                // Otherwise, if we have a target type converter, convert the value to the target type.
                value = UpdateAndUnwrap(ConvertFrom(_targetTypeConverter, value, false), ref notification);
            }
        }

        // FallbackValue applies if the result from the binding, converter or target type converter
        // is UnsetValue.
        if (value == AvaloniaProperty.UnsetValue && FallbackValue != AvaloniaProperty.UnsetValue)
            value = UpdateAndUnwrap(ConvertFallback(FallbackValue, nameof(FallbackValue)), ref notification);

        // Store the value and publish the notification/value to the observer.
        _value = value is null ? NullReference : new(value);
        _observer.OnNext(notification ?? value);
    }

    private void WriteTargetValueToSource()
    {
        if (_mode != BindingMode.OneWayToSource)
            return;

        if (_target.TryGetTarget(out var target) &&
            _targetProperty is not null &&
            target.GetValue(_targetProperty) is var value &&
            !Equals(value, LeafNode.Value))
        {
            SetValue(value);
        }
    }

    private void OnSourceChanged(object? source)
    {
        if (_nodes.Count > 0)
            _nodes[0].SetSource(source);
    }

    private void OnTargetPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == _targetProperty)
        {
            SetValue(e.NewValue);
        }
    }

    private static object? Convert(
        IValueConverter converter,
        object? converterParameter,
        object? value,
        Type targetType)
    {
        try
        {
            return converter.Convert(value, targetType, converterParameter, CultureInfo.CurrentCulture);
        }
        catch (Exception e)
        {
            var valueString = value?.ToString() ?? "(null)";
            var valueTypeName = value?.GetType().FullName ?? "null";
            var ex = new InvalidCastException(
                $"Cannot convert '{valueString}' ({valueTypeName}) to {targetType} using '{converter}'.", e);
            return new BindingNotification(ex, BindingErrorType.Error);
        }
    }

    private object? ConvertFallback(object? fallback, string fallbackName)
    {
        if (_targetTypeConverter is null || TargetType == typeof(object) || fallback == AvaloniaProperty.UnsetValue)
            return fallback;

        if (_targetTypeConverter.TryConvert(fallback, TargetType, CultureInfo.CurrentCulture, out var result))
            return result;

        if (_target.TryGetTarget(out var target))
            Log(target, $"Could not convert {fallbackName} '{fallback}' to '{TargetType}'.", LogEventLevel.Error);

        return AvaloniaProperty.UnsetValue;
   }

    private object? ConvertFrom(TargetTypeConverter? converter, object value, bool isFallback)
    {
        if (converter is null || _targetProperty is null)
            return value;

        var targetType = _targetProperty.PropertyType;

        if (converter.TryConvert(value, targetType, CultureInfo.CurrentCulture, out var result))
            return result;

        var valueString = value?.ToString() ?? "(null)";
        var valueTypeName = value?.GetType().FullName ?? "null";
        var fallbackMessage = isFallback ? " fallback value " : " ";
        var ex = new InvalidCastException(
            $"Cannot convert{fallbackMessage}'{valueString}' ({valueTypeName}) to '{targetType}'.");
        return new BindingNotification(ex, BindingErrorType.Error);
    }

    private static object? UpdateAndUnwrap(object? value, ref BindingNotification? notification)
    {
        if (value is BindingNotification n)
        {
            value = n.Value;

            if (n.Error is not null)
            {
                if (notification is null)
                    notification = n;
                else
                    notification.AddError(n.Error, n.ErrorType);
            }
        }

        notification?.SetValue(value);
        return value;
    }

    private class UncommonFields
    {
        public IValueConverter? _converter;
        public object? _converterParameter;
        public object? _fallbackValue;
        public string? _stringFormat;
        public object? _targetNullValue;
    }
}
