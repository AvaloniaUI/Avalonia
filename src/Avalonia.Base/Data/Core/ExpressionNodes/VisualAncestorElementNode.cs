using System;
using System.Text;
using Avalonia.Reactive;
using Avalonia.VisualTree;

namespace Avalonia.Data.Core.ExpressionNodes;

internal sealed class VisualAncestorElementNode : SourceNode
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

    public override object? SelectSource(object? source, object target, object? anchor)
    {
        if (source != AvaloniaProperty.UnsetValue)
            throw new NotSupportedException(
                "VisualAncestorNode is invalid in conjunction with a binding source.");
        if (target is Visual)
            return target;
        if (anchor is Visual)
            return anchor;
        throw new InvalidOperationException("Cannot find an ILogical to get a visual ancestor.");
    }

    public override bool ShouldLogErrors(object target)
    {
        return target is Visual visual && visual.IsAttachedToVisualTree;
    }

    protected override void OnSourceChanged(object? source, Exception? dataValidationError)
    {
        if (!ValidateNonNullSource(source))
            return;

        if (source is Visual visual)
        {
            var locator = VisualLocator.Track(visual, _ancestorLevel, _ancestorType);
            _subscription = locator.Subscribe(TrackedControlChanged);
        }
    }

    protected override void Unsubscribe(object oldSource)
    {
        _subscription?.Dispose();
        _subscription = null;
    }


    private void TrackedControlChanged(Visual? control)
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
