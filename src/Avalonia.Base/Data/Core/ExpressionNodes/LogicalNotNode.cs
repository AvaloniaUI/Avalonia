using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Avalonia.Data.Core.ExpressionNodes;

internal sealed class LogicalNotNode : ExpressionNode, ISettableNode
{
    public override void BuildString(StringBuilder builder)
    {
        builder.Append("!");
    }

    public override void BuildString(StringBuilder builder, IReadOnlyList<ExpressionNode> nodes)
    {
        builder.Append("!");
        if (Index > 0)
            nodes[Index - 1].BuildString(builder, nodes);
    }

    public Type ValueType => typeof(bool);

    public bool WriteValueToSource(object? value, IReadOnlyList<ExpressionNode> nodes)
    {
        if (Index > 0 && nodes[Index - 1] is ISettableNode previousNode && TryConvert(value, out var boolValue))
            return previousNode.WriteValueToSource(!boolValue, nodes);
        return false;
    }

    public override ExpressionNode Clone() => new LogicalNotNode();

    protected override void OnSourceChanged(object? source, Exception? dataValidationError)
    {
        var v = BindingNotification.ExtractValue(source);

        if (TryConvert(v, out var value))
            SetValue(BindingNotification.UpdateValue(source, !value), dataValidationError);
        else
            SetError($"Unable to convert '{source}' to bool.");
    }

    private static bool TryConvert(object? value, out bool result)
    {
        if (value is bool b)
        {
            result = b;
            return true;
        }
        if (value is string s)
        {
            // Special case string for performance.
            if (bool.TryParse(s, out result))
                return true;
        }
        else
        {
            try
            {
                result = Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                return true;
            }
            catch { }
        }

        result = false;
        return false;
    }
}
