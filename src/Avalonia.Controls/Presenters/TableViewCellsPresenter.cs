using Avalonia.Collections;
using static Avalonia.Controls.Presenters.TableViewLayoutHelper;

namespace Avalonia.Controls.Presenters;

/// <summary>
/// Lays out the cells of a <see cref="TableViewRow"/> according to the column definitions
/// of the parent <see cref="TableView"/>.
/// </summary>
/// <remarks>
/// The cells are recycled alongside their owning row in a <see cref="VirtualizingStackPanel"/>,
/// but are not virtualized in a single row (i.e. there is no column virtualization).
/// </remarks>
public class TableViewCellsPresenter : Panel
{
    internal AvaloniaList<TableViewColumn>? Columns { get; set; }

    internal void ClearCells()
    {
        foreach (var child in Children)
        {
            if (child is TableViewCell cell)
                cell.Column = null;
        }
    }

    internal void RemoveCells()
    {
        ClearCells();
        Children.Clear();
    }

    internal void RebuildCells()
    {
        if (Columns is null)
        {
            RemoveCells();
            return;
        }

        if (Columns.Count != Children.Count)
        {
            RemoveCells();

            foreach (var column in Columns)
            {
                var cell = new TableViewCell { Column = column };
                Children.Add(cell);
            }
        }
        else
        {
            for (var i = 0; i < Columns.Count; i++)
            {
                ((TableViewCell)Children[i]).Column = Columns[i];
            }
        }

        InvalidateMeasure();
    }

    internal void RefreshCell(int columnIndex)
        => ((TableViewCell)Children[columnIndex]).Refresh();

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
