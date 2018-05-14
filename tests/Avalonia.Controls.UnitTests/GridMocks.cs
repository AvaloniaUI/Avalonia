using System;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    internal static class GridMock
    {
        /// <summary>
        /// Create a mock grid to test its row layout.
        /// This method contains Arrange (`new Grid()`) and Action (`Measure()`/`Arrange()`).
        /// </summary>
        /// <param name="measure">The measure height of this grid. PositiveInfinity by default.</param>
        /// <param name="arrange">The arrange height of this grid. DesiredSize.Height by default.</param>
        /// <returns>The mock grid that its children bounds will be tested.</returns>
        internal static Grid New(Size measure = default, Size arrange = default)
        {
            var grid = new Grid();
            grid.Children.Add(new Border());
            grid.Measure(measure == default ? new Size(double.PositiveInfinity, double.PositiveInfinity) : measure);
            grid.Arrange(new Rect(default, arrange == default ? grid.DesiredSize : arrange));
            return grid;
        }

        /// <summary>
        /// Create a mock grid to test its row layout.
        /// This method contains Arrange (`new Grid()`) and Action (`Measure()`/`Arrange()`).
        /// </summary>
        /// <param name="rows">The row definitions of this mock grid.</param>
        /// <param name="measure">The measure height of this grid. PositiveInfinity by default.</param>
        /// <param name="arrange">The arrange height of this grid. DesiredSize.Height by default.</param>
        /// <returns>The mock grid that its children bounds will be tested.</returns>
        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        internal static Grid New(RowDefinitions rows,
            double measure = default, double arrange = default)
        {
            var grid = new Grid { RowDefinitions = rows };
            for (var i = 0; i < rows.Count; i++)
            {
                grid.Children.Add(new Border { [Grid.RowProperty] = i });
            }

            grid.Measure(new Size(double.PositiveInfinity, measure == default ? double.PositiveInfinity : measure));
            if (arrange == default)
            {
                arrange = measure == default ? grid.DesiredSize.Width : measure;
            }

            grid.Arrange(new Rect(0, 0, 0, arrange));

            return grid;
        }

        /// <summary>
        /// Create a mock grid to test its column layout.
        /// This method contains Arrange (`new Grid()`) and Action (`Measure()`/`Arrange()`).
        /// </summary>
        /// <param name="columns">The column definitions of this mock grid.</param>
        /// <param name="measure">The measure width of this grid. PositiveInfinity by default.</param>
        /// <param name="arrange">The arrange width of this grid. DesiredSize.Width by default.</param>
        /// <returns>The mock grid that its children bounds will be tested.</returns>
        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        internal static Grid New(ColumnDefinitions columns,
            double measure = default, double arrange = default)
        {
            var grid = new Grid { ColumnDefinitions = columns };
            for (var i = 0; i < columns.Count; i++)
            {
                grid.Children.Add(new Border { [Grid.ColumnProperty] = i });
            }

            grid.Measure(new Size(measure == default ? double.PositiveInfinity : measure, double.PositiveInfinity));
            if (arrange == default)
            {
                arrange = measure == default ? grid.DesiredSize.Width : measure;
            }

            grid.Arrange(new Rect(0, 0, arrange, 0));

            return grid;
        }
    }

    internal static class GridAssert
    {
        /// <summary>
        /// Assert all the children heights.
        /// This method will assume that the grid children count equals row count.
        /// </summary>
        /// <param name="grid">The children will be fetched through it.</param>
        /// <param name="rows">Expected row values of every children.</param>
        internal static void ChildrenHeight(Grid grid, params double[] rows)
        {
            if (grid.Children.Count != rows.Length)
            {
                throw new NotSupportedException();
            }

            for (var i = 0; i < rows.Length; i++)
            {
                Assert.Equal(rows[i], grid.Children[i].Bounds.Height);
            }
        }

        /// <summary>
        /// Assert all the children widths.
        /// This method will assume that the grid children count equals row count.
        /// </summary>
        /// <param name="grid">The children will be fetched through it.</param>
        /// <param name="columns">Expected column values of every children.</param>
        internal static void ChildrenWidth(Grid grid, params double[] columns)
        {
            if (grid.Children.Count != columns.Length)
            {
                throw new NotSupportedException();
            }

            for (var i = 0; i < columns.Length; i++)
            {
                Assert.Equal(columns[i], grid.Children[i].Bounds.Width);
            }
        }
    }
}
