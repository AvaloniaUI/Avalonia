namespace Avalonia.Controls;

/// <summary>
/// Represents the header of a <see cref="TableViewColumn"/>.
/// </summary>
public class TableViewColumnHeader : ContentControl
{
    /// <summary>
    /// Identifies the <see cref="Column"/> property.
    /// </summary>
    public static readonly DirectProperty<TableViewColumnHeader, TableViewColumn?> ColumnProperty =
        AvaloniaProperty.RegisterDirect<TableViewColumnHeader, TableViewColumn?>(nameof(Column), o => o.Column);

    /// <summary>
    /// Gets the column associated with this header.
    /// </summary>
    public TableViewColumn? Column
    {
        get;
        internal set => SetAndRaise(ColumnProperty, ref field, value);
    }
}
