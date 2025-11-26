using System;

namespace Avalonia.Data.Core.ExpressionNodes;

/// <summary>
/// A node in an <see cref="BindingExpression"/> which selects the value of the visual
/// parent's DataContext.
/// </summary>
internal sealed class ParentDataContextNode : DataContextNodeBase
{
    private static readonly AvaloniaObject s_unset = new();
    private AvaloniaObject? _parent = s_unset;

    public override ExpressionNode Clone() => new ParentDataContextNode();

    protected override void OnSourceChanged(object? source, Exception? dataValidationError)
    {
        if (!ValidateNonNullSource(source))
            return;

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
