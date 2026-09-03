using Avalonia.Layout;
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
    internal TableViewRow? Row { get; set; }

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
        Row?.LogicalChildren.Clear();
    }

    internal void RebuildCells()
    {
        if (Row?.Columns is not { } columns)
        {
            RemoveCells();
            return;
        }

        if (columns.Count != Children.Count)
        {
            RemoveCells();

            foreach (var column in columns)
            {
                var cell = new TableViewCell { Column = column };
                Children.Add(cell);
            }

            Row.LogicalChildren.AddRange(Children);
        }
        else
        {
            for (var i = 0; i < columns.Count; i++)
            {
                ((TableViewCell)Children[i]).Column = columns[i];
            }
        }

        InvalidateMeasure();
    }

    internal void RefreshCell(int columnIndex)
        => ((TableViewCell)Children[columnIndex]).Refresh();

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Row?.Columns is not { } columns)
            return default;

        // In a standard template, the column widths should have been computed by the headers' presenter.
        // If for some reason they weren't, do it now.
        if (NeedsActualWidths(columns))
            UpdateActualWidths(columns, availableSize.Width, UseLayoutRounding, LayoutHelper.GetLayoutScale(this));

        return MeasureRow(columns, Children, availableSize);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Row?.Columns is not { } columns)
            return finalSize;

        if (NeedsActualWidths(columns))
            UpdateActualWidths(columns, finalSize.Width, UseLayoutRounding, LayoutHelper.GetLayoutScale(this));

        return ArrangeRow(columns, Children, finalSize, 0);
    }
}
