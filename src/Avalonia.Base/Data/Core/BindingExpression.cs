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
using Avalonia.Logging;
using Avalonia.Threading;

namespace Avalonia.Data.Core;

/// <summary>
/// A binding expression which accepts and produces (possibly boxed) object values.
/// </summary>
/// <remarks>
/// A <see cref="BindingExpression"/> represents a untyped binding which has been
/// instantiated on an object.
/// </remarks>
internal partial class BindingExpression : IDescription, IDisposable
{
    internal static readonly WeakReference<object?> NullReference = new(null);
    private readonly WeakReference<object?>? _source;
    private readonly WeakReference<AvaloniaObject?> _target;
    private readonly BindingMode _mode;
    private readonly IReadOnlyList<ExpressionNode> _nodes;
    private readonly TargetTypeConverter? _targetTypeConverter;
    private bool _isRunning;
    private BindingPriority _priority;
    private bool _produceValue;
    private IBindingExpressionSink? _sink;
    private AvaloniaProperty? _targetProperty;
    private WeakReference<object?>? _value;
    private BindingError? _error;
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
    /// <param name="converterCulture">The converter culture to use.</param>
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
        CultureInfo? converterCulture = null,
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
        IsDataValidationEnabled = enableDataValidation;

        if (converter is not null ||
            converterCulture is not null ||
            converterParameter is not null ||
            fallbackValue != AvaloniaProperty.UnsetValue ||
            (targetNullValue is not null && targetNullValue != AvaloniaProperty.UnsetValue) ||
            !string.IsNullOrWhiteSpace(stringFormat))
        {
            _uncommon = new()
            {
                _converter = converter,
                _converterCulture = converterCulture,
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

    public BindingPriority Priority => _priority;
    public Type? SourceType => (LeafNode as ISettableNode)?.ValueType;
    public AvaloniaProperty? TargetProperty => _targetProperty;
    public Type TargetType => _targetProperty?.PropertyType ?? typeof(object);
    public IValueConverter? Converter => _uncommon?._converter;
    public CultureInfo ConverterCulture => _uncommon?._converterCulture ?? CultureInfo.CurrentCulture;
    public object? ConverterParameter => _uncommon?._converterParameter;
    public object? FallbackValue => _uncommon is not null ? _uncommon._fallbackValue : AvaloniaProperty.UnsetValue;
    public bool IsDataValidationEnabled { get; }
    public bool HasDataValidationError => _error?.ErrorType == BindingValueType.DataValidationError;
    public object? TargetNullValue => _uncommon?._targetNullValue ?? AvaloniaProperty.UnsetValue;
    public ExpressionNode LeafNode => _nodes[_nodes.Count - 1];
    public string? StringFormat => _uncommon?._stringFormat;

    /// <summary>
    /// Gets the current value of the binding expression.
    /// </summary>
    /// <returns>
    /// The current value or <see cref="AvaloniaProperty.UnsetValue"/> if the binding was unable
    /// to read a value.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// The binding expression has not been started.
    /// </exception>
    public object? GetValue()
    {
        if (!_isRunning)
            throw new InvalidOperationException("BindingExpression has not been started.");
        if (_value is null)
            return AvaloniaProperty.UnsetValue;
        else if (_value == NullReference)
            return null;
        else if (_value.TryGetTarget(out var value))
            return value;
        else
            return AvaloniaProperty.UnsetValue;
    }

    /// <summary>
    /// Gets the current value of the binding expression or the default value for the target property.
    /// </summary>
    /// <returns>
    /// The current value or the target property default.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// The binding expression has not been started.
    /// </exception>
    public object? GetValueOrDefault()
    {
        var result = GetValue();
        if (result == AvaloniaProperty.UnsetValue)
            result = GetCachedDefaultValue();
        return result;
    }

    /// <summary>
    /// Gets the data validation state, if supported.
    /// </summary>
    /// <param name="state">The binding error state.</param>
    /// <param name="error">The current binding error, if any.</param>
    /// <returns>
    /// True if the expression supports data validation, otherwise false.
    /// </returns>
    public void GetDataValidationState(out BindingValueType state, out Exception? error)
    {
        if (_error is not null)
        {
            state = _error.ErrorType;
            error = _error.Exception;
        }
        else
        {
            state = BindingValueType.Value;
            error = null;
        }
    }

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
            value = Converter.ConvertBack(value, type, ConverterParameter, ConverterCulture);

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
        if (LeafNode.IsValueAlive && IdentityEquals(LeafNode.Value, value, type))
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

    /// <summary>
    /// Initializes the binding expression with the specified subscriber and target property but
    /// does not start it.
    /// </summary>
    /// <param name="subscriber">The subscriber.</param>
    /// <param name="targetProperty">The target property.</param>
    /// <param name="priority">The priority of the binding.</param>
    /// <exception cref="AvaloniaInternalException">
    /// <paramref name="targetProperty"/> is different to that passed in the constructor, if one was
    /// passed there.
    /// </exception>
    public void Initialize(
        IBindingExpressionSink subscriber,
        AvaloniaProperty targetProperty,
        BindingPriority priority)
    {
        if (_targetProperty is not null && _targetProperty != targetProperty)
            throw new AvaloniaInternalException(
                "StartAsLocalValueBinding was called with a property different to that passed in constructor.");

        _sink = subscriber;
        _targetProperty = targetProperty;
        _priority = priority;
    }

    /// <summary>
    /// Starts the binding expression with the specified subscriber and target property..
    /// </summary>
    /// <param name="subscriber">The subscriber.</param>
    /// <param name="targetProperty">The target property.</param>
    /// <param name="priority">The priority of the binding.</param>
    public void Start(
        IBindingExpressionSink subscriber,
        AvaloniaProperty targetProperty,
        BindingPriority priority)
    {
        Initialize(subscriber, targetProperty, priority);
        Start(produceValue: true);
    }

    /// <summary>
    /// Terminates the binding.
    /// </summary>
    public void Dispose()
    {
        if (_sink is null)
            return;

        Stop();

        var sink = _sink;
        _sink = null;
        sink.OnCompleted(this);
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
    /// <param name="target">The target to which the value will be written.</param>
    /// <param name="targetNullValue">The null target value.</param>
    /// <param name="targetProperty">The target property.</param>
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
        AvaloniaObject? target = null,
        object? targetNullValue = null,
        AvaloniaProperty? targetProperty = null,
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
            target: target,
            targetNullValue: targetNullValue,
            targetProperty: targetProperty,
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

        if (nodeIndex == _nodes.Count - 1)
        {
            // The leaf node has changed. If the binding mode is not OneWayToSource, publish the
            // value to the target.
            if (_mode != BindingMode.OneWayToSource)
                UpdateAndPublishValue(value, dataValidationError);

            // If the binding mode is OneTime, then stop the binding.
            if (_mode == BindingMode.OneTime)
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

        // Clear the current value.
        UpdateValue(AvaloniaProperty.UnsetValue, null, out var hasValueChanged, out _);

        // And store the error.
        _error = new(
            new BindingChainException(error, Description, errorPoint.ToString()),
            BindingValueType.BindingError);

        PublishValue(hasValueChanged, hasErrorChanged: true);
    }

    internal void OnDataValidationError(Exception error)
    {
        _error = new(error, BindingValueType.DataValidationError);
        PublishValue(hasValueChanged: false, hasErrorChanged: true);
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

    private object? GetCachedDefaultValue()
    {
        Debug.Assert(_targetProperty is not null);

        if (_uncommon?._isDefaultValueInitialized == true)
            return _uncommon._defaultValue;

        if (_target.TryGetTarget(out var target))
        {
            _uncommon ??= new();
            _uncommon._isDefaultValueInitialized = true;

            if (_targetProperty.IsDirect)
                _uncommon._defaultValue = ((IDirectPropertyAccessor)_targetProperty).GetUnsetValue(target.GetType());
            else
                _uncommon._defaultValue = ((IStyledPropertyAccessor)_targetProperty).GetDefaultValue(target.GetType());
        }

        return _uncommon?._defaultValue ?? AvaloniaProperty.UnsetValue;
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

    private bool ShouldLogError([NotNullWhen(true)] out AvaloniaObject? target)
    {
        if (!_target.TryGetTarget(out target))
            return false;
        if (_nodes.Count > 0 && _nodes[0] is SourceNode sourceNode)
            return sourceNode.ShouldLogErrors(target);
        return true;
    }

    private void Start(bool produceValue)
    {
        Debug.Assert(_sink is not null);

        if (_isRunning)
            return;

        _isRunning = true;
        _produceValue = produceValue;

        if (_source?.TryGetTarget(out var source) == true)
        {
            if (_nodes.Count > 0)
            {
                _nodes[0].SetSource(source, null);
            }
            else
            {
                _value = new(source);
                _error = null;
                PublishValue(hasValueChanged: true, hasErrorChanged: false);
            }

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

        _produceValue = true;
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

        _isRunning = false;
        _value = null;
    }

    private void UpdateValue(
        object? value,
        Exception? dataValidationError,
        out bool hasValueChanged,
        out bool hasErrorChanged)
    {
        var isTargetNullValue = false;
        var hadError = _error is not null;

        // All values other than DoNothing should be passed to the converter.
        if (value != BindingOperations.DoNothing && Converter is { } converter)
            value = Convert(converter, ConverterParameter, value, TargetType);

        // Check this here as the converter may return DoNothing.
        if (value == BindingOperations.DoNothing)
        {
            hasValueChanged = hasErrorChanged = false;
            return;
        }

        // Set the data validation error.
        _error = dataValidationError is not null ?
            new(dataValidationError, BindingValueType.DataValidationError) :
            null;

        // If we have a data validation error and the value is Unset then we keep the
        // current value.
        if (dataValidationError is not null && value == AvaloniaProperty.UnsetValue)
        {
            hasValueChanged = false;
            hasErrorChanged = true;
            return;
        }

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
                value = ConvertFrom(_targetTypeConverter, value);
            }
        }

        // FallbackValue applies if the result from the binding, converter or target type converter
        // is UnsetValue.
        if (value == AvaloniaProperty.UnsetValue && FallbackValue != AvaloniaProperty.UnsetValue)
            value = ConvertFallback(FallbackValue, nameof(FallbackValue));

        // Update the stored value.
        var oldValue = _value;

        if (value is null)
            _value = NullReference;
        else
            _value = new(value);

        hasValueChanged = !Equals(oldValue, _value);
        hasErrorChanged = _error is not null || (_error is null && hadError);
    }

    private void PublishValue(bool hasValueChanged, bool hasErrorChanged)
    {
        if (!hasValueChanged && !hasErrorChanged)
            return;

        if (_sink is not null && _produceValue)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                _sink.OnChanged(this, hasValueChanged, hasErrorChanged);
            }
            else
            {
                // To avoid allocating closure in the outer scope we need to capture variables
                // locally. This allows us to skip most of the allocations when on UI thread.
                var sink = _sink;
                var v = hasValueChanged;
                var e = hasErrorChanged;
                Dispatcher.UIThread.Post(() => sink.OnChanged(this, v, e));
            }
        }
    }

    private void UpdateAndPublishValue(object? value, Exception? dataValidationError)
    {
        UpdateValue(
            value,
            dataValidationError,
            out var hasValueChanged,
            out var hasErrorChanged);

        if (hasValueChanged || hasErrorChanged)
            PublishValue(hasValueChanged, hasErrorChanged);
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
            _nodes[0].SetSource(source, null);
    }

    private void OnTargetPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == _targetProperty)
        {
            SetValue(e.NewValue);
        }
    }

    private object? Convert(
        IValueConverter converter,
        object? converterParameter,
        object? value,
        Type targetType)
    {
        try
        {
            return converter.Convert(value, targetType, converterParameter, ConverterCulture);
        }
        catch (Exception e)
        {
            var valueString = value?.ToString() ?? "(null)";
            var valueTypeName = value?.GetType().FullName ?? "null";
            var message = $"Could not convert '{valueString}' ({valueTypeName}) to '{targetType}' using '{converter}'";

            if (ShouldLogError(out var target))
                Log(target, $"{message}: {e.Message}", LogEventLevel.Warning);

            _error = new(new InvalidCastException(message + '.', e), BindingValueType.BindingError);
            return AvaloniaProperty.UnsetValue;
        }
    }

    private object? ConvertFallback(object? fallback, string fallbackName)
    {
        if (_targetTypeConverter is null || TargetType == typeof(object) || fallback == AvaloniaProperty.UnsetValue)
            return fallback;

        if (_targetTypeConverter.TryConvert(fallback, TargetType, ConverterCulture, out var result))
            return result;

        if (_target.TryGetTarget(out var target))
            Log(target, $"Could not convert {fallbackName} '{fallback}' to '{TargetType}'.", LogEventLevel.Error);

        return AvaloniaProperty.UnsetValue;
    }

    private object? ConvertFrom(TargetTypeConverter? converter, object? value)
    {
        if (converter is null || _targetProperty is null)
            return value;

        var targetType = _targetProperty.PropertyType;

        if (converter.TryConvert(value, targetType, ConverterCulture, out var result))
            return result;

        var valueString = value?.ToString() ?? "(null)";
        var valueTypeName = value?.GetType().FullName ?? "null";
        var message = $"Could not convert '{valueString}' ({valueTypeName}) to '{targetType}'.";

        if (ShouldLogError(out var target))
            Log(target, message, LogEventLevel.Warning);

        _error = new(new InvalidCastException(message), BindingValueType.BindingError);
        return AvaloniaProperty.UnsetValue;
    }

    private static bool IdentityEquals(object? a, object? b, Type type)
    {
        if (type.IsValueType || type == typeof(string))
            return Equals(a, b);
        else
            return ReferenceEquals(a, b);
    }

    private class BindingError
    {
        public BindingError(Exception exception, BindingValueType errorType)
        {
            Exception = exception;
            ErrorType = errorType;
        }

        public Exception Exception { get; }
        public BindingValueType ErrorType { get; }
    }

    private class UncommonFields
    {
        public IValueConverter? _converter;
        public object? _converterParameter;
        public CultureInfo? _converterCulture;
        public object? _fallbackValue;
        public string? _stringFormat;
        public object? _targetNullValue;
        public object? _defaultValue;
        public bool _isDefaultValueInitialized;
    }
}
