using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Avalonia.Data.Core.Plugins;

namespace Avalonia.Data.Core.ExpressionNodes;

/// <summary>
/// A node in the binding path of an <see cref="BindingExpression"/> that reads a property
/// via a predefined <see cref="IPropertyAccessorPlugin"/>.
/// </summary>
internal sealed class PropertyAccessorNode : ExpressionNode, IPropertyAccessorNode, ISettableNode
{
    private readonly Action<object?> _onValueChanged;
    private readonly IPropertyAccessorPlugin _plugin;
    private readonly bool _acceptsNull;
    private IPropertyAccessor? _accessor;
    private bool _enableDataValidation;
    private MemberDataValidator? _dataValidator;

    public PropertyAccessorNode(string propertyName, IPropertyAccessorPlugin plugin, bool acceptsNull)
    {
        _plugin = plugin;
        _acceptsNull = acceptsNull;
        _onValueChanged = OnValueChanged;
        PropertyName = propertyName;
    }

    public IPropertyAccessor? Accessor => _accessor;
    public string PropertyName { get; }
    public Type? ValueType => _accessor?.PropertyType;

    public override void BuildString(StringBuilder builder)
    {
        if (builder.Length > 0 && builder[builder.Length - 1] != '!')
            builder.Append('.');
        builder.Append(PropertyName);
    }

    public void EnableDataValidation() => _enableDataValidation = true;

    public bool WriteValueToSource(object? value, IReadOnlyList<ExpressionNode> nodes)
    {
        if (_accessor?.PropertyType is not null)
        {
            return _accessor.SetValue(value, BindingPriority.LocalValue);
        }

        return false;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    protected override void OnSourceChanged(object? source, Exception? dataValidationError)
    {
        if (source is null)
        {
            if (_acceptsNull)
                SetValue(null);
            else
                ValidateNonNullSource(source);
            return;
        }

        var reference = new WeakReference<object?>(source);

        if (_plugin.Start(reference, PropertyName) is { } accessor)
        {
            UpdateDataValidator(source);
            _accessor = accessor;
            _accessor.Subscribe(_onValueChanged);
        }
        else
        {
            ClearValue();
        }
    }

    protected override void Unsubscribe(object oldSource)
    {
        _accessor?.Dispose();
        _accessor = null;
        UpdateDataValidator(null);
    }

    private void UpdateDataValidator(object? source)
    {
        if (!_enableDataValidation)
            return;

        if (_dataValidator?.RaisesEvents == true)
            _dataValidator.DataValidationChanged -= OnDataValidationChanged;

        _dataValidator = source is not null ?
            DataValidationPlugin.GetDataValidator(source, PropertyName) :
            null;

        if (_dataValidator?.RaisesEvents == true)
            _dataValidator.DataValidationChanged += OnDataValidationChanged;
    }

    private void OnDataValidationChanged(object? sender, EventArgs e)
    {
        SetDataValidationError(_dataValidator?.GetDataValidationError());
    }

    private void OnValueChanged(object? newValue)
    {
        var dataValidationError = _dataValidator?.GetDataValidationError();
        SetValue(BindingNotification.ExtractValue(newValue), dataValidationError);
    }
}
