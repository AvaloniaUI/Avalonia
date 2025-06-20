using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;
using Avalonia.Logging;

namespace Avalonia.Data;

internal class TemplateBindingExpression : UntypedBindingExpressionBase
{
    private IValueConverter? _converter;
    private CultureInfo? _converterCulture;
    private object? _converterParameter;
    private BindingMode _mode;
    private readonly AvaloniaProperty? _property;
    private bool _hasPublishedValue;

    public TemplateBindingExpression(
        AvaloniaProperty? property,
        IValueConverter? converter,
        CultureInfo? converterCulture,
        object? converterParameter,
        BindingMode mode)
        : base(BindingPriority.Template)
    {
        _property = property;
        _converter = converter;
        _converterCulture = converterCulture;
        _converterParameter = converterParameter;
        _mode = mode;
    }

    public override string Description => $"{{TemplateBinding {_property}}}";

    protected override void StartCore()
    {
        _hasPublishedValue = false;
        OnTemplatedParentChanged();
        if (TryGetTarget(out var target))
            target.PropertyChanged += OnTargetPropertyChanged;
    }

    protected override void StopCore()
    {
        if (TryGetTarget(out var target))
        {
            if (target is StyledElement targetElement &&
                targetElement?.TemplatedParent is { } templatedParent)
            {
                templatedParent.PropertyChanged -= OnTemplatedParentPropertyChanged;
            }

            if (target is not null)
            {
                target.PropertyChanged -= OnTargetPropertyChanged;
            }
        }
    }

    internal override bool WriteValueToSource(object? value)
    {
        if (_property is not null && TryGetTemplatedParent(out var templatedParent))
        {
            if (_converter is not null)
                value = ConvertBack(_converter, _converterCulture, _converterParameter, value, TargetType);

            if (value != BindingOperations.DoNothing)
                templatedParent.SetCurrentValue(_property, value);

            return true;
        }

        return false;
    }

    private object? ConvertToTargetType(object? value)
    {
        var converter = TargetTypeConverter.GetDefaultConverter();

        if (converter.TryConvert(value, TargetType, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }
        else
        {
            if (TryGetTarget(out var target))
            {
                var valueString = value?.ToString() ?? "(null)";
                var valueTypeName = value?.GetType().FullName ?? "null";
                var message = $"Could not convert '{valueString}' ({valueTypeName}) to '{TargetType}'.";
                Log(target, message, LogEventLevel.Warning);
            }

            return AvaloniaProperty.UnsetValue;
        }
    }

    private void PublishValue()
    {
        if (_mode == BindingMode.OneWayToSource)
            return;

        if (TryGetTemplatedParent(out var templatedParent))
        {
            var value = _property is not null ?
                templatedParent.GetValue(_property) :
                templatedParent;
            BindingError? error = null;

            if (_converter is not null)
                value = Convert(_converter, _converterCulture, _converterParameter, value, TargetType, ref error);

            value = ConvertToTargetType(value);
            PublishValue(value, error);
            _hasPublishedValue = true;

            if (_mode == BindingMode.OneTime)
                Stop();
        }
        else if (_hasPublishedValue)
        {
            PublishValue(AvaloniaProperty.UnsetValue);
        }
    }

    private void OnTemplatedParentChanged()
    {
        if (TryGetTemplatedParent(out var templatedParent))
            templatedParent.PropertyChanged += OnTemplatedParentPropertyChanged;

        PublishValue();
    }

    private void OnTemplatedParentPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == _property)
            PublishValue();
    }

    private void OnTargetPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == StyledElement.TemplatedParentProperty)
        {
            if (e.OldValue is AvaloniaObject oldValue)
                oldValue.PropertyChanged -= OnTemplatedParentPropertyChanged;

            OnTemplatedParentChanged();
        }
        else if (_mode is BindingMode.TwoWay or BindingMode.OneWayToSource && e.Property == TargetProperty)
        {
            WriteValueToSource(e.NewValue);
        }
    }

    private bool TryGetTemplatedParent([NotNullWhen(true)] out AvaloniaObject? result)
    {
        if (TryGetTarget(out var target) &&
            target is StyledElement targetElement &&
            targetElement.TemplatedParent is { } templatedParent)
        {
            result = templatedParent;
            return true;
        }

        result = null;
        return false;
    }
}
