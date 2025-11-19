using System;
using System.Text;
using Avalonia.LogicalTree;
using Avalonia.Reactive;

namespace Avalonia.Data.Core.ExpressionNodes;

internal sealed class LogicalAncestorElementNode : SourceNode
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

    public override ExpressionNode Clone() => new LogicalAncestorElementNode(_ancestorType, _ancestorLevel);

    public override object? SelectSource(object? source, object target, object? anchor)
    {
        if (source != AvaloniaProperty.UnsetValue)
            throw new NotSupportedException(
                "LogicalAncestorNode is invalid in conjunction with a binding source.");
        if (target is ILogical)
            return target;
        if (anchor is ILogical)
            return anchor;
        throw new InvalidOperationException("Cannot find an ILogical to get a logical ancestor.");
    }

    public override bool ShouldLogErrors(object target)
    {
        return target is ILogical logical && logical.IsAttachedToLogicalTree;
    }

    protected override void OnSourceChanged(object? source, Exception? dataValidationError)
    {
        if (!ValidateNonNullSource(source))
            return;

        if (source is ILogical logical)
        {
            var locator = ControlLocator.Track(logical, _ancestorLevel, _ancestorType);
            _subscription = locator.Subscribe(TrackedControlChanged);
        }
    }

    protected override void Unsubscribe(object oldSource)
    {
        _subscription?.Dispose();
        _subscription = null;
    }

    private void TrackedControlChanged(ILogical? control)
    {
        if (control is not null)
        {
            SetValue(control);
        }
        else
        {
            SetError("Ancestor not found.");
        }
    }
}
