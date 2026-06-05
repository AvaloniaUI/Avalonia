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
        // We don't bind the various properties here.
        // First, it's pretty rare for a column's properties to change after the initial setup.
        // Second, we have additional logic depending on whether a cell template is specified.
        // Instead, values are updated manually via Refresh().

        SetValue(ThemeProperty, column.CellTheme);
        SetValue(HorizontalContentAlignmentProperty, column.HorizontalContentAlignment);

        if (column.CellTemplate is { } cellTemplate)
        {
            SetValue(ContentTemplateProperty, cellTemplate);
            Bind(ContentProperty, RowBinding);
        }
        else
        {
            SetValue(ContentTemplateProperty, null);
            Bind(ContentProperty, column.Binding ?? RowBinding);
        }
    }

    internal void Refresh()
    {
        if (Column is null)
            ClearProperties();
        else
            SetProperties(Column);
    }
}
