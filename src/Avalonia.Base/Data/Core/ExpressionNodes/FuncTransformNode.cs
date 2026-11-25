using System;
using System.Text;

namespace Avalonia.Data.Core.ExpressionNodes;

/// <summary>
/// A node in an <see cref="BindingExpression"/> which uses a function to transform its
/// value.
/// </summary>
internal sealed class FuncTransformNode : ExpressionNode
{
    private readonly Func<object?, object?> _transform;

    public FuncTransformNode(Func<object?, object?> transform)
    {
        _transform = transform;
    }

    public override void BuildString(StringBuilder builder)
    {
        // We don't have enough information to add anything here.
    }

    protected override void OnSourceChanged(object? source, Exception? dataValidationError)
    {
        // Only used for type casts, which produce null from null as in C#. Any error belongs to
        // the member access which follows, where a null-conditional operator can short-circuit it.
        SetValue(_transform(source));
    }
}
