using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Presenters;

public sealed class TableViewLayoutHelperTests : ScopedTestBase
{
    [Fact]
    public void UpdateActualWidths_With_No_Columns_Returns_False()
    {
        var columns = new AvaloniaList<TableViewColumn>();

        Assert.False(TableViewLayoutHelper.UpdateActualWidths(columns, 100, false, 1.0));
    }

    [Fact]
    public void UpdateActualWidths_Single_Star_Column_Gets_Full_Width()
    {
        var columns = new AvaloniaList<TableViewColumn>
        {
            new() { Width = new GridLength(1, GridUnitType.Star) },
        };

        Assert.True(TableViewLayoutHelper.UpdateActualWidths(columns, 200, false, 1.0));
        Assert.Equal(200, columns[0].ActualWidth);
    }

    [Fact]
    public void UpdateActualWidths_Distributes_Star_Columns_Proportionally()
    {
        var columns = new AvaloniaList<TableViewColumn>
        {
            new() { Width = new GridLength(1, GridUnitType.Star) },
            new() { Width = new GridLength(3, GridUnitType.Star) },
        };

        TableViewLayoutHelper.UpdateActualWidths(columns, 400, false, 1.0);

        Assert.Equal(100, columns[0].ActualWidth);
        Assert.Equal(300, columns[1].ActualWidth);
    }

    [Fact]
    public void UpdateActualWidths_Pixel_Column_Uses_Its_Fixed_Width()
    {
        var columns = new AvaloniaList<TableViewColumn>
        {
            new() { Width = new GridLength(80) },
        };

        TableViewLayoutHelper.UpdateActualWidths(columns, 500, false, 1.0);

        Assert.Equal(80, columns[0].ActualWidth);
    }

    [Fact]
    public void UpdateActualWidths_Pixel_Plus_Star_Subtracts_Fixed_From_Star_Budget()
    {
        var columns = new AvaloniaList<TableViewColumn>
        {
            new() { Width = new GridLength(50) },
            new() { Width = new GridLength(1, GridUnitType.Star) },
        };

        TableViewLayoutHelper.UpdateActualWidths(columns, 200, false, 1.0);

        Assert.Equal(50, columns[0].ActualWidth);
        Assert.Equal(150, columns[1].ActualWidth);
    }

    [Fact]
    public void UpdateActualWidths_Star_Clamped_To_Zero_When_Pixel_Exceeds_Available()
    {
        var columns = new AvaloniaList<TableViewColumn>
        {
            new() { Width = new GridLength(300) },
            new() { Width = new GridLength(1, GridUnitType.Star) },
        };

        TableViewLayoutHelper.UpdateActualWidths(columns, 200, false, 1.0);

        Assert.Equal(300, columns[0].ActualWidth);
        Assert.Equal(0, columns[1].ActualWidth);
    }

    [Fact]
    public void UpdateActualWidths_Treats_Auto_As_One_Star()
    {
        var columns = new AvaloniaList<TableViewColumn>
        {
            new() { Width = GridLength.Auto },
            new() { Width = new GridLength(1, GridUnitType.Star) },
        };

        TableViewLayoutHelper.UpdateActualWidths(columns, 200, false, 1.0);

        Assert.Equal(100, columns[0].ActualWidth);
        Assert.Equal(100, columns[1].ActualWidth);
    }

    [Fact]
    public void UpdateActualWidths_Falls_Back_To_1000_For_Infinite_Width()
    {
        var columns = new AvaloniaList<TableViewColumn>
        {
            new() { Width = new GridLength(1, GridUnitType.Star) },
            new() { Width = new GridLength(1, GridUnitType.Star) },
        };

        TableViewLayoutHelper.UpdateActualWidths(columns, double.PositiveInfinity, false, 1.0);

        Assert.Equal(500, columns[0].ActualWidth);
        Assert.Equal(500, columns[1].ActualWidth);
    }

    [Fact]
    public void UpdateActualWidths_Returns_False_When_No_Change()
    {
        var columns = new AvaloniaList<TableViewColumn>
        {
            new() { Width = new GridLength(1, GridUnitType.Star) },
            new() { Width = new GridLength(50) },
        };

        Assert.True(TableViewLayoutHelper.UpdateActualWidths(columns, 200, false, 1.0));
        Assert.False(TableViewLayoutHelper.UpdateActualWidths(columns, 200, false, 1.0));
    }

    [Fact]
    public void UpdateActualWidths_Returns_True_When_Width_Changes()
    {
        var columns = new AvaloniaList<TableViewColumn>
        {
            new() { Width = new GridLength(1, GridUnitType.Star) },
        };

        Assert.True(TableViewLayoutHelper.UpdateActualWidths(columns, 200, false, 1.0));
        Assert.True(TableViewLayoutHelper.UpdateActualWidths(columns, 300, false, 1.0));
        Assert.Equal(300, columns[0].ActualWidth);
    }

    [Fact]
    public void UpdateActualWidths_Spreads_Rounding_Across_Star_Columns()
    {
        var columns = new AvaloniaList<TableViewColumn>
        {
            new() { Width = new GridLength(2, GridUnitType.Star) },
            new() { Width = new GridLength(3, GridUnitType.Star) },
            new() { Width = new GridLength(2, GridUnitType.Star) },
        };

        TableViewLayoutHelper.UpdateActualWidths(columns, 100, useLayoutRounding: true, layoutScale: 1);

        // Weights 2:3:2 over 100 give 28.57 / 42.86 / 28.57. Rounding each column independently
        // would produce 29 / 43 / 29 = 101; spreading the remainder keeps the total at 100.
        Assert.Equal(29, columns[0].ActualWidth);
        Assert.Equal(42, columns[1].ActualWidth);
        Assert.Equal(29, columns[2].ActualWidth);
        Assert.Equal(100, columns[0].ActualWidth + columns[1].ActualWidth + columns[2].ActualWidth);
    }

    [Fact]
    public void UpdateActualWidths_Spreads_Rounding_With_Intertwined_Fixed_Columns()
    {
        var columns = new AvaloniaList<TableViewColumn>
        {
            new() { Width = new GridLength(50) },
            new() { Width = new GridLength(1, GridUnitType.Star) },
            new() { Width = new GridLength(30) },
            new() { Width = new GridLength(3, GridUnitType.Star) },
            new() { Width = new GridLength(2, GridUnitType.Star) },
        };

        TableViewLayoutHelper.UpdateActualWidths(columns, 201, useLayoutRounding: true, layoutScale: 1);

        // Fixed columns keep their exact size (80 total). The remaining 121px star budget is split
        // 1:3:2 (20.17 / 60.5 / 40.33). Rounding each independently would drop to 200; spreading the
        // remainder gives the extra pixel to the 3* column so everything fills exactly 201.
        Assert.Equal(50, columns[0].ActualWidth);
        Assert.Equal(20, columns[1].ActualWidth);
        Assert.Equal(30, columns[2].ActualWidth);
        Assert.Equal(61, columns[3].ActualWidth);
        Assert.Equal(40, columns[4].ActualWidth);
        Assert.Equal(
            201,
            columns[0].ActualWidth + columns[1].ActualWidth + columns[2].ActualWidth +
            columns[3].ActualWidth + columns[4].ActualWidth);
    }

    [Fact]
    public void UpdateActualWidths_Spreads_Rounding_At_Fractional_Layout_Scale()
    {
        var columns = new AvaloniaList<TableViewColumn>
        {
            new() { Width = new GridLength(3, GridUnitType.Star) },
            new() { Width = new GridLength(1, GridUnitType.Star) },
            new() { Width = new GridLength(3, GridUnitType.Star) },
        };

        TableViewLayoutHelper.UpdateActualWidths(columns, 100, useLayoutRounding: true, layoutScale: 2);

        // At 2x scale widths snap to half-pixels (whole device pixels). Weights 3:1:3 give
        // 42.86 / 14.29 / 42.86; rounding each independently would drift the total to 100.5,
        // while spreading the remainder keeps it at 100.
        Assert.Equal(43, columns[0].ActualWidth);
        Assert.Equal(14, columns[1].ActualWidth);
        Assert.Equal(43, columns[2].ActualWidth);
        Assert.Equal(100, columns[0].ActualWidth + columns[1].ActualWidth + columns[2].ActualWidth);
    }

    [Fact]
    public void UpdateActualWidths_Rounds_Fixed_Column_Width_And_Star_Absorbs_Remainder()
    {
        var columns = new AvaloniaList<TableViewColumn>
        {
            new() { Width = new GridLength(50.4) },
            new() { Width = new GridLength(1, GridUnitType.Star) },
            new() { Width = new GridLength(2, GridUnitType.Star) },
        };

        TableViewLayoutHelper.UpdateActualWidths(columns, 200, useLayoutRounding: true, layoutScale: 1);

        // The fixed column rounds to a whole pixel (50); the remaining 150px star budget is split
        // 1:2 between the star columns.
        Assert.Equal(50, columns[0].ActualWidth);
        Assert.Equal(50, columns[1].ActualWidth);
        Assert.Equal(100, columns[2].ActualWidth);
    }

    [Fact]
    public void NeedsActualWidths_Returns_False_For_Empty_Columns()
    {
        var columns = new AvaloniaList<TableViewColumn>();

        Assert.False(TableViewLayoutHelper.NeedsActualWidths(columns));
    }

    [Fact]
    public void NeedsActualWidths_Returns_True_When_First_Column_Has_NaN_Width()
    {
        var columns = new AvaloniaList<TableViewColumn> { new(), new() };

        Assert.True(TableViewLayoutHelper.NeedsActualWidths(columns));
    }

    [Fact]
    public void NeedsActualWidths_Returns_False_After_Widths_Are_Computed()
    {
        var columns = new AvaloniaList<TableViewColumn>
        {
            new() { Width = new GridLength(1, GridUnitType.Star) },
            new() { Width = new GridLength(1, GridUnitType.Star) },
        };

        TableViewLayoutHelper.UpdateActualWidths(columns, 200, false, 1.0);

        Assert.False(TableViewLayoutHelper.NeedsActualWidths(columns));
    }

    [Fact]
    public void ResetActualWidths_Sets_All_Widths_To_NaN()
    {
        var columns = new AvaloniaList<TableViewColumn>
        {
            new() { Width = new GridLength(50) },
            new() { Width = new GridLength(1, GridUnitType.Star) },
        };

        TableViewLayoutHelper.UpdateActualWidths(columns, 200, false, 1.0);
        TableViewLayoutHelper.ResetActualWidths(columns);

        Assert.True(double.IsNaN(columns[0].ActualWidth));
        Assert.True(double.IsNaN(columns[1].ActualWidth));
    }
}
