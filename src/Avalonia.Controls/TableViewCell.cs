using Avalonia.Data;

namespace Avalonia.Controls;

/// <summary>
/// Represents a single cell in a <see cref="TableViewRow"/>.
/// </summary>
public class TableViewCell : ContentControl
{
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

    private static CompiledBinding RowBinding
        => field ??= new();

    private void ClearProperties()
    {
        ClearValue(ThemeProperty);
        ClearValue(ContentTemplateProperty);
        ClearValue(ContentProperty);
    }

    private void SetProperties(TableViewColumn column)
    {
        if (column.CellTheme is { } cellTheme)
            SetCurrentValue(ThemeProperty, cellTheme);

        if (column.CellTemplate is { } cellTemplate)
        {
            SetCurrentValue(ContentTemplateProperty, cellTemplate);
            Bind(ContentProperty, RowBinding);
        }
        else if (column.Binding is { } binding)
            Bind(ContentProperty, binding);
        else
            Bind(ContentProperty, RowBinding);
    }
}
