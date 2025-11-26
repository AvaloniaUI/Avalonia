using System;
using System.Text;

namespace Avalonia.Data.Core.ExpressionNodes;

internal sealed class TemplatedParentNode : SourceNode
{
    public override void BuildString(StringBuilder builder)
    {         
        builder.Append("$templatedParent");
    }

    public override ExpressionNode Clone() => new TemplatedParentNode();

    public override object? SelectSource(object? source, object target, object? anchor)
    {
        if (source != AvaloniaProperty.UnsetValue)
            throw new NotSupportedException(
                "TemplatedParentNode is invalid in conjunction with a binding source.");
        if (target is StyledElement)
            return target;
        if (anchor is StyledElement)
            return anchor;
        throw new InvalidOperationException("Cannot find a StyledElement to get a TemplatedParent.");
    }

    protected override void OnSourceChanged(object? source, Exception? dataValidationError)
    {
        if (!ValidateNonNullSource(source))
            return;

        if (source is StyledElement newElement)
        {
            newElement.PropertyChanged += OnPropertyChanged;
            SetValue(newElement.TemplatedParent);
        }
        else
        {
            SetError($"Unable to read TemplatedParent from '{source.GetType()}'.");
        }
    }

    protected override void Unsubscribe(object oldSource)
    {
        if (oldSource is StyledElement oldElement)
            oldElement.PropertyChanged -= OnPropertyChanged;
    }

    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender == Source && e.Property == StyledElement.TemplatedParentProperty)
            SetValue(e.NewValue);
    }
}
