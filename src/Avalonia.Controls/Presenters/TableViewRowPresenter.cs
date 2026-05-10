using Avalonia.Collections;
using Avalonia.Data;
using static Avalonia.Controls.Presenters.TableViewLayoutHelper;

namespace Avalonia.Controls.Presenters;

/// <summary>
/// Lays out the cells of a <see cref="TableViewRow"/> according to the column definitions
/// of the parent <see cref="TableView"/>.
/// </summary>
public class TableViewRowPresenter : Panel
{
    private static CompiledBinding RowBinding
        => field ??= new();

    internal AvaloniaList<TableViewColumn>? Columns { get; set; }

    internal void RebuildCells()
    {
        Children.Clear();

        if (Columns is not { } columns)
            return;

        foreach (var column in columns)
        {
            var cell = CreateCell(column);
            Children.Add(cell);
        }

        InvalidateMeasure();
    }

    private static TableViewCell CreateCell(TableViewColumn column)
    {
        var cell = new TableViewCell();

        if (column.CellTheme is { } cellTheme)
            cell.Theme = cellTheme;

        if (column.CellTemplate is { } cellTemplate)
        {
            cell.ContentTemplate = cellTemplate;
            cell.Bind(ContentControl.ContentProperty, RowBinding);
        }
        else if (column.Binding is { } binding)
            cell.Bind(ContentControl.ContentProperty, binding);
        else
            cell.Bind(ContentControl.ContentProperty, RowBinding);

        return cell;
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Columns is not { } columns)
            return default;

        // In a standard template, the column widths should have been computed by the headers' presenter.
        // If for some reason they weren't, do it now.
        if (NeedsActualWidths(columns))
            UpdateActualWidths(columns, availableSize.Width);

        return MeasureRow(Columns, Children, availableSize);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Columns is not { } columns)
            return finalSize;

        if (NeedsActualWidths(columns))
            UpdateActualWidths(columns, finalSize.Width);

        return ArrangeRow(Columns, Children, finalSize, 0);
    }
}
