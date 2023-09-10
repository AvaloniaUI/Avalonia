using System;
using System.Text;
using Avalonia.Reactive;
using Avalonia.VisualTree;

namespace Avalonia.Data.Core.ExpressionNodes;

internal class VisualAncestorElementNode : ExpressionNode
{
    private readonly Type? _ancestorType;
    private readonly int _ancestorLevel;
    private IDisposable? _subscription;

    public VisualAncestorElementNode(Type? ancestorType, int ancestorLevel)
    {
        _ancestorType = ancestorType;
        _ancestorLevel = ancestorLevel;
    }

    public override void BuildString(StringBuilder builder)
    {
        builder.Append("$visualParent");

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
        if (source is Visual visual)
        {
            var locator = VisualLocator.Track(visual, _ancestorLevel, _ancestorType);
            _subscription = locator.Subscribe(x => SetValue(x));
        }
    }

    protected override void Unsubscribe(object oldSource)
    {
        _subscription?.Dispose();
        _subscription = null;
    }
}
