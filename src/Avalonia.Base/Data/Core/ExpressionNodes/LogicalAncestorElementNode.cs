using System;
using System.Text;
using Avalonia.LogicalTree;
using Avalonia.Reactive;

namespace Avalonia.Data.Core.ExpressionNodes;

internal class LogicalAncestorElementNode : ExpressionNode
{
    private readonly Type? _ancestorType;
    private readonly int _ancestorLevel;
    private IDisposable? _subscription;

    public LogicalAncestorElementNode(Type? ancestorType, int ancestorLevel)
    {
        _ancestorType = ancestorType;
        _ancestorLevel = ancestorLevel;
    }

    public override void BuildString(StringBuilder builder)
    {
        builder.Append("$parent");

        if (_ancestorLevel > 0 || _ancestorType is not null)
        {
            builder.Append('[');

            if (_ancestorType is not null)
            {
                builder.Append(_ancestorType.Name);
                if (_ancestorLevel > 0)
                    builder.Append(',');
            }

            if (_ancestorLevel > 0)
                builder.Append(_ancestorLevel);

            builder.Append(']');
        }
    }

    protected override void OnSourceChanged(object source)
    {
        if (source is ILogical logical)
        {
            var locator = ControlLocator.Track(logical, _ancestorLevel, _ancestorType);
            _subscription = locator.Subscribe(SetValue);
        }
    }

    protected override void Unsubscribe(object oldSource)
    {
        _subscription?.Dispose();
        _subscription = null;
    }
}
