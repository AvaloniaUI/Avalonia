using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Avalonia.Data.Core;

internal class MultiBindingExpression : UntypedBindingExpressionBase, IBindingExpressionSink
{
    private static readonly object s_uninitialized = new object();
    private readonly IBinding[] _bindings;
    private readonly IMultiValueConverter? _converter;
    private readonly CultureInfo? _converterCulture;
    private readonly object? _converterParameter;
    private readonly UntypedBindingExpressionBase?[] _expressions;
    private readonly object? _fallbackValue;
    private readonly object? _targetNullValue;
    private readonly object?[] _values;
    private readonly ReadOnlyCollection<object?> _valuesView;

    public MultiBindingExpression(
        BindingPriority priority,
        IList<IBinding> bindings,
        IMultiValueConverter? converter,
        CultureInfo? converterCulture,
        object? converterParameter,
        object? fallbackValue,
        object? targetNullValue)
            : base(priority)
    {
        _bindings = [.. bindings];
        _converter = converter;
        _converterCulture = converterCulture;
        _converterParameter = converterParameter;
        _expressions = new UntypedBindingExpressionBase[_bindings.Length];
        _fallbackValue = fallbackValue;
        _targetNullValue = targetNullValue;
        _values = new object?[_bindings.Length];
        _valuesView = new(_values);

#if NETSTANDARD2_0
        for (var i = 0; i < _bindings.Length; ++i)
            _values[i] = s_uninitialized;
#else
        Array.Fill(_values, s_uninitialized);
#endif
    }

    public override string Description => "MultiBinding";
    internal UntypedBindingExpressionBase?[] Expressions => _expressions;
    internal IMultiValueConverter? Converter => _converter;
    internal CultureInfo? ConverterCulture => _converterCulture;
    internal object? ConverterParameter => _converterParameter;
    internal object? FallbackValue => _fallbackValue;
    internal object? TargetNullValue => _targetNullValue;

    protected override void StartCore()
    {
        if (!TryGetTarget(out var target))
            throw new AvaloniaInternalException("MultiBindingExpression has no target.");

        for (var i = 0; i < _bindings.Length; ++i)
        {
            var binding = _bindings[i]; 

            if (binding is not IBinding2 b)
                throw new NotSupportedException($"Unsupported IBinding implementation '{binding}'.");

            var expression = b.Instance(target, null, null);

            if (expression is not UntypedBindingExpressionBase e)
                throw new NotSupportedException($"Unsupported BindingExpressionBase implementation '{expression}'.");

            _expressions[i] = e;
            e.AttachAndStart(this, target, null, Priority);
        }
    }

    protected override void StopCore()
    {
        for (var i = 0; i < _expressions.Length; ++i)
        {
            _expressions[i]?.Dispose();
            _expressions[i] = null;
            _values[i] = s_uninitialized;
        }
    }

    void IBindingExpressionSink.OnChanged(
        UntypedBindingExpressionBase instance,
        bool hasValueChanged,
        bool hasErrorChanged,
        object? value,
        BindingError? error)
    {
        var i = Array.IndexOf(_expressions, instance);
        Debug.Assert(i != -1);

        _values[i] = BindingNotification.ExtractValue(value);
        PublishValue();
    }

    void IBindingExpressionSink.OnCompleted(UntypedBindingExpressionBase instance)
    {
        // Nothing to do here.
    }

    private void PublishValue()
    {
        foreach (var v in _values)
        {
            if (v == s_uninitialized)
                return;
        }

        if (_converter is not null)
        {
            var culture = _converterCulture ?? CultureInfo.CurrentCulture;
            var converted = _converter.Convert(_valuesView, TargetType, _converterParameter, culture);

            converted = BindingNotification.ExtractValue(converted);

            if (converted != BindingOperations.DoNothing)
            {
                if (converted == null)
                    converted = _targetNullValue;
                if (converted == AvaloniaProperty.UnsetValue)
                    converted = _fallbackValue;
                PublishValue(converted);
            }
        }
        else
        {
            PublishValue(_valuesView);
        }
    }
}
