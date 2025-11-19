using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Data.Core.ExpressionNodes;

/// <summary>
/// A node in an <see cref="BindingExpression"/> which accesses an array with integer
/// indexers.
/// </summary>
internal sealed class ArrayIndexerNode : ExpressionNode, ISettableNode
{
    private readonly int[] _indexes;

    public ArrayIndexerNode(int[] indexes)
    {
        _indexes = indexes;
    }

    public Type? ValueType => Source?.GetType().GetElementType();

    public override void BuildString(StringBuilder builder)
    {
        builder.Append('[');

        for (var i = 0; i < _indexes.Length; i++)
        {
            builder.Append(_indexes[i]);
            if (i != _indexes.Length - 1)
                builder.Append(',');
        }

        builder.Append(']');
    }

    public override ExpressionNode Clone() => new ArrayIndexerNode(_indexes);

    public bool WriteValueToSource(object? value, IReadOnlyList<ExpressionNode> nodes)
    {
        if (Source is Array array)
        {
            array.SetValue(value, _indexes);
            return true;
        }

        return false;
    }

    protected override void OnSourceChanged(object? source, Exception? dataValidationError)
    {
        if (!ValidateNonNullSource(source))
            return;

        if (source is Array array)
            SetValue(array.GetValue(_indexes));
        else
            ClearValue();
    }
}
