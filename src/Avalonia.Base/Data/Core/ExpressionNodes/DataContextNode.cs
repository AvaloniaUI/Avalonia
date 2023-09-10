using System;
using System.Text;

namespace Avalonia.Data.Core.ExpressionNodes;

internal class DataContextNode : ExpressionNode, ISourceNode
{
    public override void BuildString(StringBuilder builder)
    {
        // Nothing to add.
    }

    public object SelectSource(object? source, object target, object? anchor)
    {
        if (source is not null)
            throw new NotSupportedException(
                "DataContextNode is invalid in conjunction with a binding source.");
        if (target is IDataContextProvider and AvaloniaObject)
            return target;
        if (anchor is IDataContextProvider and AvaloniaObject)
            return anchor;
        throw new InvalidOperationException("Cannot find a DataContext to bind to.");
    }

    protected override void OnSourceChanged(object source)
    {
        if (source is IDataContextProvider && source is AvaloniaObject ao)
        {
            ao.PropertyChanged += OnPropertyChanged;
            SetValue(ao.GetValue(StyledElement.DataContextProperty));
        }
        else
        {
            SetError(new InvalidCastException($"Unable to read DataContext from '{source.GetType()}'."));
        }
    }

    protected override void Unsubscribe(object oldSource)
    {
        if (oldSource is StyledElement oldElement)
            oldElement.PropertyChanged -= OnPropertyChanged;
    }

    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender == Source && e.Property == StyledElement.DataContextProperty)
            SetValue(e.NewValue);
    }
}
