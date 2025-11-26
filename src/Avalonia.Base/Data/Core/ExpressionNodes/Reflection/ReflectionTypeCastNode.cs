using System;
using System.Text;

namespace Avalonia.Data.Core.ExpressionNodes.Reflection;

/// <summary>
/// A node in an <see cref="BindingExpression"/> which casts a value using reflection.
/// </summary>
internal sealed class ReflectionTypeCastNode : ExpressionNode
{
    private readonly Type _targetType;

    public ReflectionTypeCastNode(Type targetType) => _targetType = targetType;

    public override void BuildString(StringBuilder builder)
    {
        builder.Append('(');
        builder.Append(_targetType.Name);
        builder.Append(')');
    }

    public override ExpressionNode Clone() => new ReflectionTypeCastNode(_targetType);

    protected override void OnSourceChanged(object? source, Exception? dataValidationError)
    {
        if (!ValidateNonNullSource(source))
            return;

        if (_targetType.IsInstanceOfType(source))
            SetValue(source);
        else
            ClearValue();
    }
}
