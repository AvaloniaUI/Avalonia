using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.UnitTests;

using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Avalonia.Controls.UnitTests
{
    public class GridTests
    {
        private readonly ITestOutputHelper output;

        public GridTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        private Grid CreateGrid(params (string name, GridLength width)[] columns)
        {
            return CreateGrid(columns.Select(c =>
                (c.name, c.width, ColumnDefinition.MinWidthProperty.GetDefaultValue(typeof(ColumnDefinition)))).ToArray());
        }

        private Grid CreateGrid(params (string name, GridLength width, double minWidth)[] columns)
        {
            return CreateGrid(columns.Select(c =>
                (c.name, c.width, c.minWidth, ColumnDefinition.MaxWidthProperty.GetDefaultValue(typeof(ColumnDefinition)))).ToArray());
        }

        private Grid CreateGrid(params (string name, GridLength width, double minWidth, double maxWidth)[] columns)
        {

            var grid = new Grid();
            foreach (var k in columns.Select(c => new ColumnDefinition
            {
                SharedSizeGroup = c.name,
                Width = c.width,
                MinWidth = c.minWidth,
                MaxWidth = c.maxWidth
            }))
                grid.ColumnDefinitions.Add(k);

            return grid;
        }

        private Control AddSizer(Grid grid, int column, double size = 30)
        {
            var ctrl = new Control { MinWidth = size, MinHeight = size };
            ctrl.SetValue(Grid.ColumnProperty, column);
            grid.Children.Add(ctrl);
            output.WriteLine($"[AddSizer] Column: {column} MinWidth: {size} MinHeight: {size}");
            return ctrl;
        }

        private void PrintColumnDefinitions(Grid grid)
        {
            output.WriteLine($"[Grid] ActualWidth: {grid.Bounds.Width} ActualHeight: {grid.Bounds.Width}");
            output.WriteLine($"[ColumnDefinitions]");
            for (int i = 0; i < grid.ColumnDefinitions.Count; i++)
            {
                var cd = grid.ColumnDefinitions[i];
                output.WriteLine($"[{i}] ActualWidth: {cd.ActualWidth} SharedSizeGroup: {cd.SharedSizeGroup}");
            }
        }

        [Fact]
        public void Calculates_Colspan_Correctly()
        {
            var target = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(new GridLength(4, GridUnitType.Pixel)),
                    new ColumnDefinition(GridLength.Auto),
                },
                RowDefinitions = new RowDefinitions
                {
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Auto),
                },
                Children =
                {
                    new Border
                    {
                        Width = 100,
                        Height = 25,
                        [Grid.ColumnSpanProperty] = 3,
                    },
                    new Border
                    {
                        Width = 150,
                        Height = 25,
                        [Grid.RowProperty] = 1,
                    },
                    new Border
                    {
                        Width = 50,
                        Height = 25,
                        [Grid.RowProperty] = 1,
                        [Grid.ColumnProperty] = 2,
                    }
                },
            };

            target.Measure(Size.Infinity);

            // Issue #25 only appears after a second measure
            target.InvalidateMeasure();
            target.Measure(Size.Infinity);

            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(204, 50), target.Bounds.Size);
            Assert.Equal(150d, target.ColumnDefinitions[0].ActualWidth);
            Assert.Equal(4d, target.ColumnDefinitions[1].ActualWidth);
            Assert.Equal(50d, target.ColumnDefinitions[2].ActualWidth);
            Assert.Equal(new Rect(52, 0, 100, 25), target.Children[0].Bounds);
            Assert.Equal(new Rect(0, 25, 150, 25), target.Children[1].Bounds);
            Assert.Equal(new Rect(154, 25, 50, 25), target.Children[2].Bounds);
        }

        [Fact]
        public void Layout_EmptyColumnRow_LayoutLikeANormalPanel()
        {
            // Arrange & Action
            var grid = GridMock.New(arrange: new Size(600, 200));

            // Assert
            GridAssert.ChildrenWidth(grid, 600);
            GridAssert.ChildrenHeight(grid, 200);
        }

        [Fact]
        public void Layout_PixelRowColumn_BoundsCorrect()
        {
            // Arrange & Action
            var rowGrid = GridMock.New(new RowDefinitions("100,200,300"));
            var columnGrid = GridMock.New(new ColumnDefinitions("50,100,150"));

            // Assert
            GridAssert.ChildrenHeight(rowGrid, 100, 200, 300);
            GridAssert.ChildrenWidth(columnGrid, 50, 100, 150);
        }

        [Fact]
        public void Layout_StarRowColumn_BoundsCorrect()
        {
            // Arrange & Action
            var rowGrid = GridMock.New(new RowDefinitions("1*,2*,3*"), 600);
            var columnGrid = GridMock.New(new ColumnDefinitions("*,*,2*"), 600);

            // Assert
            GridAssert.ChildrenHeight(rowGrid, 100, 200, 300);
            GridAssert.ChildrenWidth(columnGrid, 150, 150, 300);
        }

        [Fact]
        public void Layout_MixPixelStarRowColumn_BoundsCorrect()
        {
            // Arrange & Action
            var rowGrid = GridMock.New(new RowDefinitions("1*,2*,150"), 600);
            var columnGrid = GridMock.New(new ColumnDefinitions("1*,2*,150"), 600);

            // Assert
            GridAssert.ChildrenHeight(rowGrid, 150, 300, 150);
            GridAssert.ChildrenWidth(columnGrid, 150, 300, 150);
        }

        [Fact]
        public void Layout_StarRowColumnWithMinLength_BoundsCorrect()
        {
            // Arrange & Action
            var rowGrid = GridMock.New(new RowDefinitions
            {
                new RowDefinition(1, GridUnitType.Star) { MinHeight = 200 },
                new RowDefinition(1, GridUnitType.Star),
                new RowDefinition(1, GridUnitType.Star),
            }, 300);
            var columnGrid = GridMock.New(new ColumnDefinitions
            {
                new ColumnDefinition(1, GridUnitType.Star) { MinWidth = 200 },
                new ColumnDefinition(1, GridUnitType.Star),
                new ColumnDefinition(1, GridUnitType.Star),
            }, 300);

            // Assert
            GridAssert.ChildrenHeight(rowGrid, 200, 50, 50);
            GridAssert.ChildrenWidth(columnGrid, 200, 50, 50);
        }

        [Fact]
        public void Layout_StarRowColumnWithMaxLength_BoundsCorrect()
        {
            // Arrange & Action
            var rowGrid = GridMock.New(new RowDefinitions
            {
                new RowDefinition(1, GridUnitType.Star) { MaxHeight = 200 },
                new RowDefinition(1, GridUnitType.Star),
                new RowDefinition(1, GridUnitType.Star),
            }, 800);
            var columnGrid = GridMock.New(new ColumnDefinitions
            {
                new ColumnDefinition(1, GridUnitType.Star) { MaxWidth = 200 },
                new ColumnDefinition(1, GridUnitType.Star),
                new ColumnDefinition(1, GridUnitType.Star),
            }, 800);

            // Assert
            GridAssert.ChildrenHeight(rowGrid, 200, 300, 300);
            GridAssert.ChildrenWidth(columnGrid, 200, 300, 300);
        }

        [Fact]
        public void Changing_Child_Column_Invalidates_Measure()
        {
            Border child;
            var target = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,*"),
                Children =
                {
                    (child = new Border
                    {
                        [Grid.ColumnProperty] = 0,
                    }),
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));
            Assert.True(target.IsMeasureValid);

            Grid.SetColumn(child, 1);

            Assert.False(target.IsMeasureValid);
        }

        [Fact]
        public void Grid_GridLength_Same_Size_Pixel_0()
        {
            var grid = CreateGrid(
                (null, new GridLength()),
                (null, new GridLength()),
                (null, new GridLength()),
                (null, new GridLength()));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, false);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == null), cd => Assert.Equal(0, cd.ActualWidth));
        }

        [Fact]
        public void Grid_GridLength_Same_Size_Pixel_50()
        {
            var grid = CreateGrid(
                (null, new GridLength(50)),
                (null, new GridLength(50)),
                (null, new GridLength(50)),
                (null, new GridLength(50)));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, false);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == null), cd => Assert.Equal(50, cd.ActualWidth));
        }

        [Fact]
        public void Grid_GridLength_Same_Size_Auto()
        {
            var grid = CreateGrid(
                (null, new GridLength(0, GridUnitType.Auto)),
                (null, new GridLength(0, GridUnitType.Auto)),
                (null, new GridLength(0, GridUnitType.Auto)),
                (null, new GridLength(0, GridUnitType.Auto)));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, false);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == null), cd => Assert.Equal(0, cd.ActualWidth));
        }

        [Fact]
        public void Grid_GridLength_Same_Size_Star()
        {
            var grid = CreateGrid(
                (null, new GridLength(1, GridUnitType.Star)),
                (null, new GridLength(1, GridUnitType.Star)),
                (null, new GridLength(1, GridUnitType.Star)),
                (null, new GridLength(1, GridUnitType.Star)));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, false);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == null), cd => Assert.Equal(50, cd.ActualWidth));
        }

        [Fact]
        public void SharedSize_Grid_GridLength_Same_Size_Pixel_0()
        {
            var grid = CreateGrid(
                ("A", new GridLength()),
                ("A", new GridLength()),
                ("A", new GridLength()),
                ("A", new GridLength()));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(0, cd.ActualWidth));
        }

        [Fact]
        public void SharedSize_Grid_GridLength_Same_Size_Pixel_50()
        {
            var grid = CreateGrid(
                ("A", new GridLength(50)),
                ("A", new GridLength(50)),
                ("A", new GridLength(50)),
                ("A", new GridLength(50)));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(50, cd.ActualWidth));
        }

        [Fact]
        public void SharedSize_Grid_GridLength_Same_Size_Auto()
        {
            var grid = CreateGrid(
                ("A", new GridLength(0, GridUnitType.Auto)),
                ("A", new GridLength(0, GridUnitType.Auto)),
                ("A", new GridLength(0, GridUnitType.Auto)),
                ("A", new GridLength(0, GridUnitType.Auto)));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(0, cd.ActualWidth));
        }

        [Fact]
        public void SharedSize_Grid_GridLength_Same_Size_Star()
        {
            var grid = CreateGrid(
                ("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
                ("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
                ("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
                ("A", new GridLength(1, GridUnitType.Star)));  // Star sizing is treated as Auto, 1 is ignored

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(0, cd.ActualWidth));
        }

        [Fact]
        public void SharedSize_Grid_GridLength_Same_Size_Pixel_0_First_Column_0()
        {
            var grid = CreateGrid(
                (null, new GridLength()),
                ("A", new GridLength()),
                ("A", new GridLength()),
                ("A", new GridLength()),
                ("A", new GridLength()));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(0, cd.ActualWidth));
        }

        [Fact]
        public void SharedSize_Grid_GridLength_Same_Size_Pixel_50_First_Column_0()
        {
            var grid = CreateGrid(
                (null, new GridLength()),
                ("A", new GridLength(50)),
                ("A", new GridLength(50)),
                ("A", new GridLength(50)),
                ("A", new GridLength(50)));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(50, cd.ActualWidth));
        }

        [Fact]
        public void SharedSize_Grid_GridLength_Same_Size_Auto_First_Column_0()
        {
            var grid = CreateGrid(
                (null, new GridLength()),
                ("A", new GridLength(0, GridUnitType.Auto)),
                ("A", new GridLength(0, GridUnitType.Auto)),
                ("A", new GridLength(0, GridUnitType.Auto)),
                ("A", new GridLength(0, GridUnitType.Auto)));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(0, cd.ActualWidth));
        }

        [Fact]
        public void SharedSize_Grid_GridLength_Same_Size_Star_First_Column_0()
        {
            var grid = CreateGrid(
                (null, new GridLength()),
                ("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
                ("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
                ("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
                ("A", new GridLength(1, GridUnitType.Star))); // Star sizing is treated as Auto, 1 is ignored

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(0, cd.ActualWidth));
        }

        [Fact]
        public void SharedSize_Grid_GridLength_Same_Size_Pixel_0_Last_Column_0()
        {
            var grid = CreateGrid(
                ("A", new GridLength()),
                ("A", new GridLength()),
                ("A", new GridLength()),
                ("A", new GridLength()),
                (null, new GridLength()));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(0, cd.ActualWidth));
        }

        [Fact]
        public void SharedSize_Grid_GridLength_Same_Size_Pixel_50_Last_Column_0()
        {
            var grid = CreateGrid(
                ("A", new GridLength(50)),
                ("A", new GridLength(50)),
                ("A", new GridLength(50)),
                ("A", new GridLength(50)),
                (null, new GridLength()));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(50, cd.ActualWidth));
        }

        [Fact]
        public void SharedSize_Grid_GridLength_Same_Size_Auto_Last_Column_0()
        {
            var grid = CreateGrid(
                ("A", new GridLength(0, GridUnitType.Auto)),
                ("A", new GridLength(0, GridUnitType.Auto)),
                ("A", new GridLength(0, GridUnitType.Auto)),
                ("A", new GridLength(0, GridUnitType.Auto)),
                (null, new GridLength()));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(0, cd.ActualWidth));
        }

        [Fact]
        public void SharedSize_Grid_GridLength_Same_Size_Star_Last_Column_0()
        {
            var grid = CreateGrid(
                ("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
                ("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
                ("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
                ("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
                (null, new GridLength()));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(0, cd.ActualWidth));
        }

        [Fact]
        public void SharedSize_Grid_GridLength_Same_Size_Pixel_0_First_And_Last_Column_0()
        {
            var grid = CreateGrid(
                (null, new GridLength()),
                ("A", new GridLength()),
                ("A", new GridLength()),
                ("A", new GridLength()),
                ("A", new GridLength()),
                (null, new GridLength()));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(0, cd.ActualWidth));
        }

        [Fact]
        public void SharedSize_Grid_GridLength_Same_Size_Pixel_50_First_And_Last_Column_0()
        {
            var grid = CreateGrid(
                (null, new GridLength()),
                ("A", new GridLength(50)),
                ("A", new GridLength(50)),
                ("A", new GridLength(50)),
                ("A", new GridLength(50)),
                (null, new GridLength()));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(50, cd.ActualWidth));
        }

        [Fact]
        public void SharedSize_Grid_GridLength_Same_Size_Auto_First_And_Last_Column_0()
        {
            var grid = CreateGrid(
                (null, new GridLength()),
                ("A", new GridLength(0, GridUnitType.Auto)),
                ("A", new GridLength(0, GridUnitType.Auto)),
                ("A", new GridLength(0, GridUnitType.Auto)),
                ("A", new GridLength(0, GridUnitType.Auto)),
                (null, new GridLength()));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(0, cd.ActualWidth));
        }

        [Fact]
        public void SharedSize_Grid_GridLength_Same_Size_Star_First_And_Last_Column_0()
        {
            var grid = CreateGrid(
                (null, new GridLength()),
                ("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
                ("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
                ("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
                ("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
                (null, new GridLength()));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(0, cd.ActualWidth));
        }

        [Fact]
        public void SharedSize_Grid_GridLength_Same_Size_Pixel_0_Two_Groups()
        {
            var grid = CreateGrid(
                ("A", new GridLength()),
                ("B", new GridLength()),
                ("B", new GridLength()),
                ("A", new GridLength()));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(0, cd.ActualWidth));
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => Assert.Equal(0, cd.ActualWidth));
        }

        [Fact]
        public void SharedSize_Grid_GridLength_Same_Size_Pixel_50_Two_Groups()
        {
            var grid = CreateGrid(
                ("A", new GridLength(25)),
                ("B", new GridLength(75)),
                ("B", new GridLength(75)),
                ("A", new GridLength(25)));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(25, cd.ActualWidth));
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => Assert.Equal(75, cd.ActualWidth));
        }

        [Fact]
        public void SharedSize_Grid_GridLength_Same_Size_Auto_Two_Groups()
        {
            var grid = CreateGrid(
                ("A", new GridLength(0, GridUnitType.Auto)),
                ("B", new GridLength(0, GridUnitType.Auto)),
                ("B", new GridLength(0, GridUnitType.Auto)),
                ("A", new GridLength(0, GridUnitType.Auto)));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(0, cd.ActualWidth));
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => Assert.Equal(0, cd.ActualWidth));
        }

        [Fact]
        public void SharedSize_Grid_GridLength_Same_Size_Star_Two_Groups()
        {
            var grid = CreateGrid(
                ("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
                ("B", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
                ("B", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
                ("A", new GridLength(1, GridUnitType.Star)));  // Star sizing is treated as Auto, 1 is ignored

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(0, cd.ActualWidth));
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => Assert.Equal(0, cd.ActualWidth));
        }

        [Fact]
        public void SharedSize_Grid_GridLength_Same_Size_Pixel_0_First_Column_0_Two_Groups()
        {
            var grid = CreateGrid(
                (null, new GridLength()),
                ("A", new GridLength()),
                ("B", new GridLength()),
                ("B", new GridLength()),
                ("A", new GridLength()));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(0, cd.ActualWidth));
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => Assert.Equal(0, cd.ActualWidth));
        }

        [Fact]
        public void SharedSize_Grid_GridLength_Same_Size_Pixel_50_First_Column_0_Two_Groups()
        {
            var grid = CreateGrid(
                (null, new GridLength()),
                ("A", new GridLength(25)),
                ("B", new GridLength(75)),
                ("B", new GridLength(75)),
                ("A", new GridLength(25)));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(25, cd.ActualWidth));
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => Assert.Equal(75, cd.ActualWidth));
        }

        [Fact]
        public void SharedSize_Grid_GridLength_Same_Size_Auto_First_Column_0_Two_Groups()
        {
            var grid = CreateGrid(
                (null, new GridLength()),
                ("A", new GridLength(0, GridUnitType.Auto)),
                ("B", new GridLength(0, GridUnitType.Auto)),
                ("B", new GridLength(0, GridUnitType.Auto)),
                ("A", new GridLength(0, GridUnitType.Auto)));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(0, cd.ActualWidth));
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => Assert.Equal(0, cd.ActualWidth));
        }

        [Fact]
        public void SharedSize_Grid_GridLength_Same_Size_Star_First_Column_0_Two_Groups()
        {
            var grid = CreateGrid(
                (null, new GridLength()),
                ("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
                ("B", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
                ("B", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
                ("A", new GridLength(1, GridUnitType.Star))); // Star sizing is treated as Auto, 1 is ignored

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(0, cd.ActualWidth));
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => Assert.Equal(0, cd.ActualWidth));
        }

        [Fact]
        public void SharedSize_Grid_GridLength_Same_Size_Pixel_0_Last_Column_0_Two_Groups()
        {
            var grid = CreateGrid(
                ("A", new GridLength()),
                ("B", new GridLength()),
                ("B", new GridLength()),
                ("A", new GridLength()),
                (null, new GridLength()));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(0, cd.ActualWidth));
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => Assert.Equal(0, cd.ActualWidth));
        }

        [Fact]
        public void SharedSize_Grid_GridLength_Same_Size_Pixel_50_Last_Column_0_Two_Groups()
        {
            var grid = CreateGrid(
                ("A", new GridLength(25)),
                ("B", new GridLength(75)),
                ("B", new GridLength(75)),
                ("A", new GridLength(25)),
                (null, new GridLength()));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(25, cd.ActualWidth));
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => Assert.Equal(75, cd.ActualWidth));
        }

        [Fact]
        public void SharedSize_Grid_GridLength_Same_Size_Auto_Last_Column_0_Two_Groups()
        {
            var grid = CreateGrid(
                ("A", new GridLength(0, GridUnitType.Auto)),
                ("B", new GridLength(0, GridUnitType.Auto)),
                ("B", new GridLength(0, GridUnitType.Auto)),
                ("A", new GridLength(0, GridUnitType.Auto)),
                (null, new GridLength()));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(0, cd.ActualWidth));
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => Assert.Equal(0, cd.ActualWidth));
        }

        [Fact]
        public void SharedSize_Grid_GridLength_Same_Size_Star_Last_Column_0_Two_Groups()
        {
            var grid = CreateGrid(
                ("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
                ("B", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
                ("B", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
                ("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
                (null, new GridLength()));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(0, cd.ActualWidth));
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => Assert.Equal(0, cd.ActualWidth));
        }

        [Fact]
        public void SharedSize_Grid_GridLength_Same_Size_Pixel_0_First_And_Last_Column_0_Two_Groups()
        {
            var grid = CreateGrid(
                (null, new GridLength()),
                ("A", new GridLength()),
                ("B", new GridLength()),
                ("B", new GridLength()),
                ("A", new GridLength()),
                (null, new GridLength()));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(0, cd.ActualWidth));
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => Assert.Equal(0, cd.ActualWidth));
        }

        [Fact]
        public void SharedSize_Grid_GridLength_Same_Size_Pixel_50_First_And_Last_Column_0_Two_Groups()
        {
            var grid = CreateGrid(
                (null, new GridLength()),
                ("A", new GridLength(25)),
                ("B", new GridLength(75)),
                ("B", new GridLength(75)),
                ("A", new GridLength(25)),
                (null, new GridLength()));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(25, cd.ActualWidth));
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => Assert.Equal(75, cd.ActualWidth));
        }

        [Fact]
        public void SharedSize_Grid_GridLength_Same_Size_Auto_First_And_Last_Column_0_Two_Groups()
        {
            var grid = CreateGrid(
                (null, new GridLength()),
                ("A", new GridLength(0, GridUnitType.Auto)),
                ("B", new GridLength(0, GridUnitType.Auto)),
                ("B", new GridLength(0, GridUnitType.Auto)),
                ("A", new GridLength(0, GridUnitType.Auto)),
                (null, new GridLength()));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(0, cd.ActualWidth));
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => Assert.Equal(0, cd.ActualWidth));
        }

        [Fact]
        public void SharedSize_Grid_GridLength_Same_Size_Star_First_And_Last_Column_0_Two_Groups()
        {
            var grid = CreateGrid(
                (null, new GridLength()),
                ("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
                ("B", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
                ("B", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
                ("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
                (null, new GridLength()));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(0, cd.ActualWidth));
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => Assert.Equal(0, cd.ActualWidth));
        }

        [Fact]
        public void Size_Propagation_Is_Constrained_To_Innermost_Scope()
        {
            var grids = new[] { CreateGrid(("A", new GridLength())), CreateGrid(("A", new GridLength(30)), (null, new GridLength())) };
            var innerScope = new Grid();

            foreach (var grid in grids)
                innerScope.Children.Add(grid);

            innerScope.SetValue(Grid.IsSharedSizeScopeProperty, true);

            var outerGrid = CreateGrid(("A", new GridLength(0)));
            var outerScope = new Grid();
            outerScope.Children.Add(outerGrid);
            outerScope.Children.Add(innerScope);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(outerScope);

            root.Measure(new Size(50, 50));
            root.Arrange(new Rect(new Point(), new Point(50, 50)));
            Assert.Equal(0, outerGrid.ColumnDefinitions[0].ActualWidth);
        }

        [Fact]
        public void Size_Group_Changes_Are_Tracked()
        {
            var grids = new[] {
                CreateGrid((null, new GridLength(0, GridUnitType.Auto)), (null, new GridLength())),
                CreateGrid(("A", new GridLength(30)), (null, new GridLength())) };
            var scope = new Grid();
            foreach (var xgrids in grids)
                scope.Children.Add(xgrids);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            root.Measure(new Size(50, 50));
            root.Arrange(new Rect(new Point(), new Point(50, 50)));
            PrintColumnDefinitions(grids[0]);
            Assert.Equal(0, grids[0].ColumnDefinitions[0].ActualWidth);

            grids[0].ColumnDefinitions[0].SharedSizeGroup = "A";

            root.Measure(new Size(51, 51));
            root.Arrange(new Rect(new Point(), new Point(51, 51)));
            PrintColumnDefinitions(grids[0]);
            Assert.Equal(30, grids[0].ColumnDefinitions[0].ActualWidth);

            grids[0].ColumnDefinitions[0].SharedSizeGroup = null;

            root.Measure(new Size(52, 52));
            root.Arrange(new Rect(new Point(), new Point(52, 52)));
            PrintColumnDefinitions(grids[0]);
            Assert.Equal(0, grids[0].ColumnDefinitions[0].ActualWidth);
        }

        [Fact]
        public void Collection_Changes_Are_Tracked()
        {
            var grid = CreateGrid(
                ("A", new GridLength(20)),
                ("A", new GridLength(30)),
                ("A", new GridLength(40)),
                (null, new GridLength()));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(40, cd.ActualWidth));

            grid.ColumnDefinitions.RemoveAt(2);

            // NOTE: THIS IS BROKEN IN WPF
            // grid.Measure(new Size(200, 200));
            // grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            // PrintColumnDefinitions(grid);
            // Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(30, cd.ActualWidth));

            grid.ColumnDefinitions.Insert(1, new ColumnDefinition { Width = new GridLength(30), SharedSizeGroup = "A" });

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(30, cd.ActualWidth));

            grid.ColumnDefinitions[1] = new ColumnDefinition { Width = new GridLength(10), SharedSizeGroup = "A" };

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(30, cd.ActualWidth));

            grid.ColumnDefinitions[1] = new ColumnDefinition { Width = new GridLength(50), SharedSizeGroup = "A" };

            // NOTE: THIS IS BROKEN IN WPF
            // grid.Measure(new Size(200, 200));
            // grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            // PrintColumnDefinitions(grid);
            // Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(0, cd.ActualWidth));
        }

        [Fact]
        public void Size_Priorities_Are_Maintained()
        {
            var sizers = new List<Control>();
            var grid = CreateGrid(
                ("A", new GridLength(20)),
                ("A", new GridLength(20, GridUnitType.Auto)),
                ("A", new GridLength(1, GridUnitType.Star)),
                ("A", new GridLength(1, GridUnitType.Star)),
                (null, new GridLength()));
            for (int i = 0; i < 3; i++)
                sizers.Add(AddSizer(grid, i, 6 + i * 6));
            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(100, 100));
            grid.Arrange(new Rect(new Point(), new Point(100, 100)));
            PrintColumnDefinitions(grid);
            // all in group are equal to the first fixed column
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(20, cd.ActualWidth));

            grid.ColumnDefinitions[0].SharedSizeGroup = null;

            grid.Measure(new Size(100, 100));
            grid.Arrange(new Rect(new Point(), new Point(100, 100)));
            PrintColumnDefinitions(grid);

            // NOTE: THIS IS BROKEN IN WPF
            // Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(6 + 2 * 6, cd.ActualWidth));
            // grid.ColumnDefinitions[1].SharedSizeGroup = null;

            // grid.Measure(new Size(100, 100));
            // grid.Arrange(new Rect(new Point(), new Point(100, 100)));
            // PrintColumnDefinitions(grid);
            
            // NOTE: THIS IS BROKEN IN WPF
            // all in group are equal to width (MinWidth) of the sizer in the second column
            // Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(6 + 1 * 6, cd.ActualWidth));
            
            // NOTE: THIS IS BROKEN IN WPF
            // grid.ColumnDefinitions[2].SharedSizeGroup = null;

            // NOTE: THIS IS BROKEN IN WPF
            // grid.Measure(new Size(double.PositiveInfinity, 100));
            // grid.Arrange(new Rect(new Point(), new Point(100, 100)));
            // PrintColumnDefinitions(grid);
            // with no constraint star columns default to the MinWidth of the sizer in the column
            // Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(0, cd.ActualWidth));
        }

        [Fact]
        public void ColumnDefinitions_Collection_Is_ReadOnly()
        {
            var grid = CreateGrid(
                ("A", new GridLength(50)),
                ("A", new GridLength(50)),
                ("A", new GridLength(50)),
                ("A", new GridLength(50)));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(50, cd.ActualWidth));

            grid.ColumnDefinitions[0] = new ColumnDefinition { Width = new GridLength(25), SharedSizeGroup = "A" };
            grid.ColumnDefinitions[1] = new ColumnDefinition { Width = new GridLength(75), SharedSizeGroup = "B" };
            grid.ColumnDefinitions[2] = new ColumnDefinition { Width = new GridLength(75), SharedSizeGroup = "B" };
            grid.ColumnDefinitions[3] = new ColumnDefinition { Width = new GridLength(25), SharedSizeGroup = "A" };

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(25, cd.ActualWidth));
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => Assert.Equal(75, cd.ActualWidth));
        }

        [Fact]
        public void ColumnDefinitions_Collection_Reset_SharedSizeGroup()
        {
            var grid = CreateGrid(
                ("A", new GridLength(25)),
                ("B", new GridLength(75)),
                ("B", new GridLength(75)),
                ("A", new GridLength(25)));

            var scope = new Grid();
            scope.Children.Add(grid);

            var root = new Grid();
            root.UseLayoutRounding = false;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(25, cd.ActualWidth));
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => Assert.Equal(75, cd.ActualWidth));

            grid.ColumnDefinitions[0].SharedSizeGroup = null;
            grid.ColumnDefinitions[0].Width = new GridLength(50);
            grid.ColumnDefinitions[1].SharedSizeGroup = null;
            grid.ColumnDefinitions[1].Width = new GridLength(50);
            grid.ColumnDefinitions[2].SharedSizeGroup = null;
            grid.ColumnDefinitions[2].Width = new GridLength(50);
            grid.ColumnDefinitions[3].SharedSizeGroup = null;
            grid.ColumnDefinitions[3].Width = new GridLength(50);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == null), cd => Assert.Equal(50, cd.ActualWidth));
        }

        [Fact]
        public void ColumnDefinition_Equals_True()
        {
            var a = new ColumnDefinition(100.0, GridUnitType.Pixel);
            var b = new ColumnDefinition(100.0, GridUnitType.Pixel);

            Assert.True(a.Equals(b as ColumnDefinition));
            Assert.True(a.Equals(b as object));
        }

        [Fact]
        public void ColumnDefinition_Equals_False()
        {
            var a = new ColumnDefinition(100.0, GridUnitType.Pixel);
            var b = new ColumnDefinition(100.0, GridUnitType.Star);

            Assert.False(a.Equals(b as ColumnDefinition));
            Assert.False(a.Equals(b as object));
        }

        [Fact]
        public void ColumnDefinition_Null_Equals_False()
        {
            var a = new ColumnDefinition();

            Assert.False(a.Equals(obj: null));
            Assert.False(a.Equals(other: null));
        }

        [Fact]
        public void ColumnDefinitions_Equals_True()
        {
            var a = new ColumnDefinitions()
            {
                new ColumnDefinition(100.0, GridUnitType.Pixel),
                new ColumnDefinition(200.0, GridUnitType.Pixel)
            };

            var b = new ColumnDefinitions()
            {
                new ColumnDefinition(100.0, GridUnitType.Pixel),
                new ColumnDefinition(200.0, GridUnitType.Pixel)
            };

            Assert.True(a.Equals(b as ColumnDefinitions));
            Assert.True(a.Equals(b as object));
        }

        [Fact]
        public void ColumnDefinitions_Equals_False()
        {
            var a = new ColumnDefinitions()
            {
                new ColumnDefinition(100.0, GridUnitType.Pixel),
                new ColumnDefinition(200.0, GridUnitType.Pixel)
            };

            var b = new ColumnDefinitions()
            {
                new ColumnDefinition(50.0, GridUnitType.Star),
                new ColumnDefinition(50.0, GridUnitType.Star)
            };

            Assert.False(a.Equals(b as ColumnDefinitions));
            Assert.False(a.Equals(b as object));
        }

        [Fact]
        public void ColumnDefinitions_Null_Equals_False()
        {
            var a = new ColumnDefinitions();

            Assert.False(a.Equals(obj: null));
            Assert.False(a.Equals(other: null));
        }

        [Fact]
        public void RowDefinition_Equals_True()
        {
            var a = new RowDefinition(100.0, GridUnitType.Pixel);
            var b = new RowDefinition(100.0, GridUnitType.Pixel);

            Assert.True(a.Equals(b as RowDefinition));
            Assert.True(a.Equals(b as object));
        }

        [Fact]
        public void RowDefinition_Equals_False()
        {
            var a = new RowDefinition(100.0, GridUnitType.Pixel);
            var b = new RowDefinition(100.0, GridUnitType.Star);

            Assert.False(a.Equals(b as RowDefinition));
            Assert.False(a.Equals(b as object));
        }

        [Fact]
        public void RowDefinition_Null_Equals_False()
        {
            var a = new RowDefinition();

            Assert.False(a.Equals(obj: null));
            Assert.False(a.Equals(other: null));
        }

        [Fact]
        public void RowDefinitions_Equals_True()
        {
            var a = new RowDefinitions()
            {
                new RowDefinition(100.0, GridUnitType.Pixel),
                new RowDefinition(200.0, GridUnitType.Pixel),
            };

            var b = new RowDefinitions()
            {
                new RowDefinition(100.0, GridUnitType.Pixel),
                new RowDefinition(200.0, GridUnitType.Pixel),
            };

            Assert.True(a.Equals(b as RowDefinitions));
            Assert.True(a.Equals(b as object));
        }

        [Fact]
        public void RowDefinitions_Equals_False()
        {
            var a = new RowDefinitions()
            {
                new RowDefinition(100.0, GridUnitType.Pixel),
                new RowDefinition(200.0, GridUnitType.Pixel),
            };

            var b = new RowDefinitions()
            {
                new RowDefinition(50.0, GridUnitType.Star),
                new RowDefinition(50.0, GridUnitType.Star),
            };

            Assert.False(a.Equals(b as RowDefinitions));
            Assert.False(a.Equals(b as object));
        }

        [Fact]
        public void RowDefinitions_Null_Equals_False()
        {
            var a = new RowDefinitions();

            Assert.False(a.Equals(obj: null));
            Assert.False(a.Equals(other: null));
        }
    }
}
