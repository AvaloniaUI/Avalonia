using System;
using Avalonia.Collections;

namespace Avalonia.Controls.Presenters;

internal static class TableViewLayoutHelper
{
    /// <summary>
    /// Distributes <paramref name="availableWidth"/> among the columns.
    /// Pixel columns take their fixed size; remaining space is split proportionally among star columns.
    /// Auto is treated as 1*.
    /// </summary>
    public static bool UpdateActualWidths(AvaloniaList<TableViewColumn> columns, double availableWidth)
    {
        if (columns.Count == 0)
            return false;

        var fixedTotal = 0.0;
        var starTotal = 0.0;
        var modified = false;

        for (var i = 0; i < columns.Count; i++)
        {
            var width = columns[i].Width;
            if (width.IsAbsolute)
            {
                var actualWidth = width.Value;

                if (columns[i].ActualWidth != actualWidth)
                {
                    columns[i].ActualWidth = actualWidth;
                    modified = true;
                }

                fixedTotal += actualWidth;
            }
            else
            {
                // Star or Auto — treat both as star
                starTotal += width.IsStar ? width.Value : 1.0;
            }
        }

        double starBudget;

        if (double.IsPositiveInfinity(availableWidth))
        {
            // The headers aren't supposed to be measured with infinity, as they're normally outside the main ScrollViewer.
            // If they've been relocated, or are missing, use 1000 pixels arbitrarily so star columns are still displayed.
            starBudget = 1000;
        }
        else
            starBudget = Math.Max(0.0, availableWidth - fixedTotal);

        for (var i = 0; i < columns.Count; i++)
        {
            var width = columns[i].Width;
            if (!width.IsAbsolute)
            {
                var share = width.IsStar ? width.Value : 1.0;
                var actualWidth = starTotal > 0 ? share / starTotal * starBudget : 0;

                if (columns[i].ActualWidth != actualWidth)
                {
                    columns[i].ActualWidth = actualWidth;
                    modified = true;
                }
            }
        }

        return modified;
    }

    public static bool NeedsActualWidths(AvaloniaList<TableViewColumn> columns)
        => columns.Count > 0 && double.IsNaN(columns[0].ActualWidth);

    public static void ResetActualWidths(AvaloniaList<TableViewColumn> columns)
    {
        foreach (var column in columns)
            column.ActualWidth = double.NaN;
    }

    public static Size MeasureRow(AvaloniaList<TableViewColumn> columns, Controls cells, Size availableSize)
    {
        if (cells.Count != columns.Count)
            return default;

        var totalWidth = 0.0;
        var totalHeight = 0.0;

        for (var i = 0; i < cells.Count; i++)
        {
            var child = cells[i];
            var columnWidth = columns[i].ActualWidth;
            child.Measure(new Size(columnWidth, availableSize.Height));
            totalWidth += columnWidth;
            totalHeight = Math.Max(totalHeight, child.DesiredSize.Height);
        }

        return new Size(totalWidth, totalHeight);
    }

    public static Size ArrangeRow(AvaloniaList<TableViewColumn> columns, Controls cells, Size finalSize, double offset)
    {
        if (cells.Count != columns.Count)
            return finalSize;

        var x = offset;
        for (var i = 0; i < cells.Count; i++)
        {
            var width = columns[i].ActualWidth;
            columns[i].ActualWidth = width;
            cells[i].Arrange(new Rect(x, 0, width, finalSize.Height));
            x += width;
        }

        return finalSize;
    }
}
