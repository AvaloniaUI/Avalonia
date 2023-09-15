using System;
using System.Text;

namespace Avalonia.Data.Core.ExpressionNodes;

/// <summary>
/// A node in an <see cref="BindingExpression"/> which selects the value of the visual
/// parent's DataContext.
/// </summary>
internal class ParentDataContextNode : ExpressionNode, ISourceNode
{
    private static AvaloniaObject s_Unset = new();
    private AvaloniaObject? _parent = s_Unset;

    public override void BuildString(StringBuilder builder)
    {
        // Nothing to add.
    }

    public object SelectSource(object? source, object target, object? anchor)
    {
        if (source != AvaloniaProperty.UnsetValue)
            throw new NotSupportedException(
                "ParentDataContextNode is invalid in conjunction with a binding source.");
        if (target is IDataContextProvider and AvaloniaObject)
            return target;
        if (anchor is IDataContextProvider and AvaloniaObject)
            return anchor;
        throw new InvalidOperationException("Cannot find a DataContext to bind to.");
    }

    protected override void OnSourceChanged(object source)
    {
        if (source is AvaloniaObject newElement)
            newElement.PropertyChanged += OnPropertyChanged;

        if (source is Visual v)
            SetParent(v.GetValue(Visual.VisualParentProperty));
        else
            SetParent(null);
    }

    protected override void Unsubscribe(object oldSource)
    {
        if (oldSource is AvaloniaObject oldElement)
            oldElement.PropertyChanged -= OnPropertyChanged;
    }

    private void SetParent(AvaloniaObject? parent)
    {
        if (parent == _parent)
            return;

        Unsubscribe();
        _parent = parent;

        if (_parent is IDataContextProvider)
        {
            _parent.PropertyChanged += OnParentPropertyChanged;
            SetValue(_parent.GetValue(StyledElement.DataContextProperty));
        }
        else
        {
            SetValue(null);
        }
    }

    private void Unsubscribe()
    {
        if (_parent is not null)
            _parent.PropertyChanged -= OnParentPropertyChanged;
    }

    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Visual.VisualParentProperty)
            SetParent(e.NewValue as AvaloniaObject);
    }

    private void OnParentPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == StyledElement.DataContextProperty)
            SetValue(e.NewValue);
    }
}
