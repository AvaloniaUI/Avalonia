using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Data.Core.Plugins;

namespace Avalonia.Data.Core.ExpressionNodes;

/// <summary>
/// A node in the binding path of an <see cref="BindingExpression"/> that reads a property
/// via a predefined <see cref="IPropertyAccessorPlugin"/>.
/// </summary>
internal class PropertyAccessorNode : ExpressionNode, IPropertyAccessorNode, ISettableNode
{
    private readonly Action<object?> _onValueChanged;
    private readonly IPropertyAccessorPlugin _plugin;
    private IPropertyAccessor? _accessor;

    public PropertyAccessorNode(string propertyName, IPropertyAccessorPlugin plugin)
    {
        _plugin = plugin;
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

    public void EnableDataValidation() { }

    public bool WriteValueToSource(object? value, IReadOnlyList<ExpressionNode> nodes)
    {
        if (_accessor?.PropertyType is not null)
        {
            return _accessor.SetValue(value, BindingPriority.LocalValue);
        }

        return false;
    }

    protected override void OnSourceChanged(object source, Exception? dataValidationError)
    {
        if (_plugin.Start(new(source), PropertyName) is { } accessor)
        {
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
    }

    private void OnValueChanged(object? newValue)
    {
        SetValue(newValue);
    }
}
