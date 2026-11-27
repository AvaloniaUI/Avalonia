using System;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Presenters;

public sealed class TableViewColumnWidthLimitTests : ScopedTestBase
{
    [Theory]
    [InlineData(40, 100, 200, 100)]
    [InlineData(100, 100, 200, 100)]
    [InlineData(150, 100, 200, 150)]
    [InlineData(200, 100, 200, 200)]
    [InlineData(400, 100, 200, 200)]
    [InlineData(40, 200, 100, 200)]
    public void Pixel_Widths_Are_Clamped_And_Subtracted_From_Star_Budget(
        double width, double min, double max, double expected)
    {
        var columns = new AvaloniaList<TableViewColumn>
        {
            new() { Width = new GridLength(width), MinWidth = min, MaxWidth = max },
            new(),
        };

        TableViewLayoutHelper.UpdateActualWidths(columns, 500, false, 1);

        Assert.Equal(expected, columns[0].ActualWidth);
        Assert.Equal(500 - expected, columns[1].ActualWidth);
        Assert.Equal(new GridLength(width), columns[0].Width);
        Assert.False(TableViewLayoutHelper.UpdateActualWidths(columns, 500, false, 1));
    }

    [Theory]
    [InlineData(GridUnitType.Star, 0, 50, 50, 150)]
    [InlineData(GridUnitType.Auto, 0, 50, 50, 150)]
    [InlineData(GridUnitType.Star, 150, 200, 150, 50)]
    [InlineData(GridUnitType.Auto, 150, 200, 150, 50)]
    [InlineData(GridUnitType.Star, 260, 300, 260, 0)]
    [InlineData(GridUnitType.Star, 150, 50, 150, 50)]
    public void Star_And_Auto_Widths_Redistribute_At_Limits(
        GridUnitType unit, double min, double max, double first, double second)
    {
        var columns = new AvaloniaList<TableViewColumn>
        {
            new() { Width = new GridLength(1, unit), MinWidth = min, MaxWidth = max },
            new(),
        };

        TableViewLayoutHelper.UpdateActualWidths(columns, 200, false, 1);

        Assert.Equal(first, columns[0].ActualWidth);
        Assert.Equal(second, columns[1].ActualWidth);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Simultaneous_Floor_And_Ceiling_Do_Not_Leave_Feasible_Space_Unused(bool reverse)
    {
        var floor = new TableViewColumn { MinWidth = 60 };
        var ceiling = new TableViewColumn { MaxWidth = 10 };
        var columns = reverse
            ? new AvaloniaList<TableViewColumn> { ceiling, floor }
            : new AvaloniaList<TableViewColumn> { floor, ceiling };

        TableViewLayoutHelper.UpdateActualWidths(columns, 100, false, 1);

        Assert.Equal(90, floor.ActualWidth);
        Assert.Equal(10, ceiling.ActualWidth);
    }

    [Fact]
    public void Remaining_Stars_Keep_Their_Weight_Ratio()
    {
        var columns = new AvaloniaList<TableViewColumn>
        {
            new() { MaxWidth = 20 },
            new(),
            new() { Width = new GridLength(3, GridUnitType.Star) },
        };

        TableViewLayoutHelper.UpdateActualWidths(columns, 200, false, 1);

        Assert.Equal(20, columns[0].ActualWidth);
        Assert.Equal(45, columns[1].ActualWidth);
        Assert.Equal(135, columns[2].ActualWidth);
    }

    [Fact]
    public void Exhausted_Limits_Allow_Overflow_Or_Unused_Space()
    {
        var columns = new AvaloniaList<TableViewColumn>
        {
            new() { MinWidth = 80, MaxWidth = 100 },
            new() { MinWidth = 60, MaxWidth = 100 },
        };

        TableViewLayoutHelper.UpdateActualWidths(columns, 100, false, 1);
        Assert.Equal(80, columns[0].ActualWidth);
        Assert.Equal(60, columns[1].ActualWidth);

        TableViewLayoutHelper.UpdateActualWidths(columns, 300, false, 1);
        Assert.Equal(100, columns[0].ActualWidth);
        Assert.Equal(100, columns[1].ActualWidth);
    }

    [Fact]
    public void Zero_Weight_Star_Reserves_Only_Its_Minimum()
    {
        var columns = new AvaloniaList<TableViewColumn>
        {
            new() { Width = new GridLength(0, GridUnitType.Star), MinWidth = 30 },
            new(),
        };

        TableViewLayoutHelper.UpdateActualWidths(columns, 200, false, 1);

        Assert.Equal(30, columns[0].ActualWidth);
        Assert.Equal(170, columns[1].ActualWidth);
    }

    [Theory]
    [InlineData(GridUnitType.Pixel)]
    [InlineData(GridUnitType.Star)]
    [InlineData(GridUnitType.Auto)]
    public void Hidden_Columns_Ignore_Limits(GridUnitType unit)
    {
        var columns = new AvaloniaList<TableViewColumn>
        {
            new() { Width = new GridLength(1, unit), IsVisible = false, MinWidth = 100, MaxWidth = 50 },
            new(),
        };

        TableViewLayoutHelper.UpdateActualWidths(columns, 200, false, 1);

        Assert.Equal(0, columns[0].ActualWidth);
        Assert.Equal(200, columns[1].ActualWidth);
    }

    [Theory]
    [InlineData(1, 20, 27, 26)]
    [InlineData(2, 20.5, 26, 26.5)]
    public void Constrained_Stars_Spread_Rounding_Across_Remaining_Columns(
        double scale, double first, double second, double third)
    {
        var columns = new AvaloniaList<TableViewColumn>
        {
            new() { MaxWidth = 20.4 },
            new(),
            new(),
        };

        TableViewLayoutHelper.UpdateActualWidths(columns, 73, true, scale);

        Assert.Equal(first, columns[0].ActualWidth);
        Assert.Equal(second, columns[1].ActualWidth);
        Assert.Equal(third, columns[2].ActualWidth);
        Assert.Equal(73, columns[0].ActualWidth + columns[1].ActualWidth + columns[2].ActualWidth);
    }

    [Theory]
    [InlineData(1, 20, 53)]
    [InlineData(2, 20.5, 52.5)]
    public void Pixel_Limits_Are_Rounded_Before_Allocating_Star_Space(
        double scale, double first, double second)
    {
        var columns = new AvaloniaList<TableViewColumn>
        {
            new() { Width = new GridLength(10), MinWidth = 20.4 },
            new(),
        };

        TableViewLayoutHelper.UpdateActualWidths(columns, 73, true, scale);

        Assert.Equal(first, columns[0].ActualWidth);
        Assert.Equal(second, columns[1].ActualWidth);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    public void Width_Limits_Reject_Invalid_Values(double value)
    {
        var column = new TableViewColumn();

        Assert.Throws<ArgumentException>(() => column.MinWidth = value);
        Assert.Throws<ArgumentException>(() => column.MaxWidth = value);
        Assert.Equal(0, column.MinWidth);
        Assert.Equal(double.PositiveInfinity, column.MaxWidth);
    }

    [Fact]
    public void Minimum_Rejects_Infinity_But_Maximum_Allows_It()
    {
        var column = new TableViewColumn();

        Assert.Throws<ArgumentException>(() => column.MinWidth = double.PositiveInfinity);
        column.MaxWidth = 100;
        column.MaxWidth = double.PositiveInfinity;
        Assert.Equal(0, column.MinWidth);
        Assert.Equal(double.PositiveInfinity, column.MaxWidth);
    }

    [Fact]
    public void Constrained_Large_Weights_Do_Not_Erase_Remaining_Small_Weights()
    {
        var columns = new AvaloniaList<TableViewColumn>
        {
            new() { Width = new GridLength(1e308, GridUnitType.Star), MaxWidth = 20 },
            new() { Width = new GridLength(1e-308, GridUnitType.Star) },
            new() { Width = new GridLength(3e-308, GridUnitType.Star) },
        };

        TableViewLayoutHelper.UpdateActualWidths(columns, 200, false, 1);

        Assert.Equal(20, columns[0].ActualWidth);
        Assert.Equal(45, columns[1].ActualWidth, 8);
        Assert.Equal(135, columns[2].ActualWidth, 8);
    }

    [Theory]
    [InlineData(64)]
    [InlineData(65)]
    public void Many_Constrained_Columns_Keep_The_Total_Width(int count)
    {
        var columns = new AvaloniaList<TableViewColumn> { new() { MaxWidth = 1 } };
        for (var i = 1; i < count; ++i)
            columns.Add(new TableViewColumn());

        TableViewLayoutHelper.UpdateActualWidths(columns, 1 + 2 * (count - 1), false, 1);

        Assert.Equal(1, columns[0].ActualWidth);
        for (var i = 1; i < count; ++i)
            Assert.Equal(2, columns[i].ActualWidth, 8);
        Assert.False(TableViewLayoutHelper.UpdateActualWidths(columns, 1 + 2 * (count - 1), false, 1));
    }

    [Fact]
    public void Redistributing_Space_Can_Reach_Another_Columns_Limit()
    {
        var columns = new AvaloniaList<TableViewColumn>
        {
            new() { MaxWidth = 10 },
            new() { MaxWidth = 40 },
            new(),
        };

        TableViewLayoutHelper.UpdateActualWidths(columns, 100, false, 1);

        Assert.Equal(10, columns[0].ActualWidth);
        Assert.Equal(40, columns[1].ActualWidth);
        Assert.Equal(50, columns[2].ActualWidth);
    }
}
