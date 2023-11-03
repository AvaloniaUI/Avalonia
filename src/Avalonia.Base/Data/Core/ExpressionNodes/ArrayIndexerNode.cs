using System;
using System.Text;

namespace Avalonia.Data.Core.ExpressionNodes;

/// <summary>
/// A node in an <see cref="BindingExpression"/> which accesses an array with integer
/// indexers.
/// </summary>
internal class ArrayIndexerNode : ExpressionNode
{
    private readonly int[] _indexes;

    public ArrayIndexerNode(int[] indexes)
    {
        _indexes = indexes;
    }

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

    protected override void OnSourceChanged(object source, Exception? dataValidationError)
    {
        if (source is Array array)
            SetValue(array.GetValue(_indexes));
        else
            ClearValue();
    }
}
