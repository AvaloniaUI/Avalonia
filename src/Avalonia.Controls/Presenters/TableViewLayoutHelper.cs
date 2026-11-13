using System;
using Avalonia.Collections;
using Avalonia.Layout;

namespace Avalonia.Controls.Presenters;

internal static class TableViewLayoutHelper
{
    /// <summary>
    /// Distributes <paramref name="availableWidth"/> among the columns.
    /// Pixel columns take their clamped fixed size; remaining space is split proportionally among star columns,
    /// redistributing space when a star column reaches a minimum or maximum.
    /// Auto is treated as 1*.
    /// </summary>
    public static bool UpdateActualWidths(
        AvaloniaList<TableViewColumn> columns,
        double availableWidth,
        bool useLayoutRounding,
        double layoutScale)
    {
        if (columns.Count == 0)
            return false;

        var fixedTotal = 0.0;
        var starTotal = 0.0;
        var modified = false;
        var hasStarLimits = false;

        for (var i = 0; i < columns.Count; i++)
        {
            if (!columns[i].IsVisible)
            {
                if (columns[i].ActualWidth != 0)
                {
                    columns[i].ActualWidth = 0;
                    modified = true;
                }
                continue;
            }

            var width = columns[i].Width;
            if (width.IsAbsolute)
            {
                var actualWidth = columns[i].ClampWidth(width.Value);

                if (useLayoutRounding)
                    actualWidth = LayoutHelper.RoundLayoutValue(actualWidth, layoutScale);

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
                hasStarLimits |= columns[i].MinWidth > 0 || !double.IsPositiveInfinity(columns[i].MaxWidth);
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

        Span<double> starWidths = hasStarLimits
            ? columns.Count <= 64 ? stackalloc double[columns.Count] : new double[columns.Count]
            : Span<double>.Empty;
        if (hasStarLimits)
            ResolveStarWidths(columns, starBudget, starWidths);

        // Distribute the star budget by rounding the cumulative right edge of each star column rather than each width in
        // isolation. This spreads the sub-pixel remainders across the columns instead of letting them accumulate, so the
        // sum of the rounded star widths equals the rounded allocation, which fills the budget when limits permit.
        var unroundedEdge = 0.0;
        var roundedEdge = 0.0;

        for (var i = 0; i < columns.Count; i++)
        {
            if (!columns[i].IsVisible)
                continue;

            var width = columns[i].Width;
            if (!width.IsAbsolute)
            {
                var share = width.IsStar ? width.Value : 1.0;
                unroundedEdge += hasStarLimits
                    ? starWidths[i]
                    : starTotal > 0 ? share / starTotal * starBudget : 0;

                var actualWidth = unroundedEdge - roundedEdge;

                if (useLayoutRounding)
                {
                    var nextRoundedEdge = LayoutHelper.RoundLayoutValue(unroundedEdge, layoutScale);
                    actualWidth = nextRoundedEdge - roundedEdge;
                    roundedEdge = nextRoundedEdge;
                }
                else
                    roundedEdge = unroundedEdge;

                if (columns[i].ActualWidth != actualWidth)
                {
                    columns[i].ActualWidth = actualWidth;
                    modified = true;
                }
            }
        }

        return modified;
    }

    private static void ResolveStarWidths(
        AvaloniaList<TableViewColumn> columns,
        double budget,
        Span<double> widths)
    {
        widths.Fill(double.NaN);
        var originalBudget = budget;

        while (true)
        {
            var maxWeight = 0.0;
            for (var i = 0; i < columns.Count; ++i)
            {
                if (columns[i].IsVisible && !columns[i].Width.IsAbsolute && double.IsNaN(widths[i]))
                    maxWeight = Math.Max(maxWeight, columns[i].Width.IsStar ? columns[i].Width.Value : 1);
            }

            var totalWeight = 0.0;
            for (var i = 0; i < columns.Count; ++i)
            {
                if (columns[i].IsVisible && !columns[i].Width.IsAbsolute && double.IsNaN(widths[i]))
                    totalWeight += Weight(columns[i]);
            }

            var clampedTotal = 0.0;
            for (var i = 0; i < columns.Count; ++i)
            {
                if (columns[i].IsVisible && !columns[i].Width.IsAbsolute && double.IsNaN(widths[i]))
                    clampedTotal += columns[i].ClampWidth(Share(columns[i]));
            }

            // If floors overfill the budget, resolve only floors; otherwise resolve only ceilings.
            // Freezing both at once can leave a feasible distribution with unused space.
            var resolved = false;
            for (var i = 0; i < columns.Count; ++i)
            {
                var column = columns[i];
                if (!column.IsVisible || column.Width.IsAbsolute || !double.IsNaN(widths[i]))
                    continue;

                var share = Share(column);
                if ((clampedTotal > budget && share < column.MinWidth) ||
                    (clampedTotal < budget && share > Math.Max(column.MinWidth, column.MaxWidth)))
                {
                    widths[i] = column.ClampWidth(share);
                    resolved = true;
                }
            }

            if (!resolved)
            {
                for (var i = 0; i < columns.Count; ++i)
                {
                    if (columns[i].IsVisible && !columns[i].Width.IsAbsolute && double.IsNaN(widths[i]))
                        widths[i] = columns[i].ClampWidth(Share(columns[i]));
                }
                return;
            }

            // Recompute instead of subtracting weights to avoid cancellation with very unequal weights.
            budget = 0;
            for (var i = 0; i < columns.Count; ++i)
            {
                if (!double.IsNaN(widths[i]))
                    budget += widths[i];
            }
            budget = Math.Max(0, originalBudget - budget);

            double Share(TableViewColumn column)
                => totalWeight > 0 ? Weight(column) / totalWeight * budget : 0;

            double Weight(TableViewColumn column)
                => maxWeight > 0 ? (column.Width.IsStar ? column.Width.Value : 1) / maxWeight : 0;
        }
    }

    /// <remarks>
    /// Contract between <see cref="UpdateActualWidths"/>, <see cref="NeedsActualWidths"/> and <see cref="ResetActualWidths"/>: <br/>
    /// If <see cref="TableViewColumn.ActualWidth"/> is NaN, a recalculation of the ActualWidths is needed.
    /// All column widths are reset and recalculated together, so checking the first one is sufficient.
    /// </remarks>
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
            if (!columns[i].IsVisible)
                continue;

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
            if (!columns[i].IsVisible)
                continue;

            var width = columns[i].ActualWidth;
            cells[i].Arrange(new Rect(x, 0, width, finalSize.Height));
            x += width;
        }

        return finalSize;
    }
}
