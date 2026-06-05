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

        Assert.False(TableViewLayoutHelper.UpdateActualWidths(columns, 100));
    }

    [Fact]
    public void UpdateActualWidths_Single_Star_Column_Gets_Full_Width()
    {
        var columns = new AvaloniaList<TableViewColumn>
        {
            new() { Width = new GridLength(1, GridUnitType.Star) },
        };

        Assert.True(TableViewLayoutHelper.UpdateActualWidths(columns, 200));
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

        TableViewLayoutHelper.UpdateActualWidths(columns, 400);

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

        TableViewLayoutHelper.UpdateActualWidths(columns, 500);

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

        TableViewLayoutHelper.UpdateActualWidths(columns, 200);

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

        TableViewLayoutHelper.UpdateActualWidths(columns, 200);

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

        TableViewLayoutHelper.UpdateActualWidths(columns, 200);

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

        TableViewLayoutHelper.UpdateActualWidths(columns, double.PositiveInfinity);

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

        Assert.True(TableViewLayoutHelper.UpdateActualWidths(columns, 200));
        Assert.False(TableViewLayoutHelper.UpdateActualWidths(columns, 200));
    }

    [Fact]
    public void UpdateActualWidths_Returns_True_When_Width_Changes()
    {
        var columns = new AvaloniaList<TableViewColumn>
        {
            new() { Width = new GridLength(1, GridUnitType.Star) },
        };

        Assert.True(TableViewLayoutHelper.UpdateActualWidths(columns, 200));
        Assert.True(TableViewLayoutHelper.UpdateActualWidths(columns, 300));
        Assert.Equal(300, columns[0].ActualWidth);
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

        TableViewLayoutHelper.UpdateActualWidths(columns, 200);

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

        TableViewLayoutHelper.UpdateActualWidths(columns, 200);
        TableViewLayoutHelper.ResetActualWidths(columns);

        Assert.True(double.IsNaN(columns[0].ActualWidth));
        Assert.True(double.IsNaN(columns[1].ActualWidth));
    }
}
