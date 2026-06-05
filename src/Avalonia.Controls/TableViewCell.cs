using Avalonia.Data;

namespace Avalonia.Controls;

/// <summary>
/// Represents a single cell in a <see cref="TableViewRow"/>.
/// </summary>
public class TableViewCell : ContentControl
{
    private static CompiledBinding RowBinding
        => field ??= new();

    public static readonly DirectProperty<TableViewCell, TableViewColumn?> ColumnProperty =
        AvaloniaProperty.RegisterDirect<TableViewCell, TableViewColumn?>(nameof(Column), o => o.Column);

    public TableViewColumn? Column
    {
        get;
        internal set
        {
            var oldValue = field;
            if (!SetAndRaise(ColumnProperty, ref field, value))
                return;

            if (oldValue is not null)
                ClearProperties();

            if (value is not null)
                SetProperties(value);
        }
    }

    private void ClearProperties()
    {
        ClearValue(ThemeProperty);
        ClearValue(HorizontalContentAlignmentProperty);
        ClearValue(ContentTemplateProperty);
        ClearValue(ContentProperty);
    }

    private void SetProperties(TableViewColumn column)
    {
        SetValue(ThemeProperty, column.CellTheme);
        SetValue(HorizontalContentAlignmentProperty, column.HorizontalContentAlignment);

        if (column.CellTemplate is { } cellTemplate)
        {
            SetValue(ContentTemplateProperty, cellTemplate);
            Bind(ContentProperty, RowBinding);
        }
        else if (column.Binding is { } binding)
        {
            SetValue(ContentTemplateProperty, null);
            Bind(ContentProperty, binding);
        }
        else
        {
            SetValue(ContentTemplateProperty, null);
            Bind(ContentProperty, RowBinding);
        }
    }
}
