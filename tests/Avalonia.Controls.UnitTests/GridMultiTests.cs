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
    public class GridMultiTests
    {
        private readonly ITestOutputHelper output;

        public GridMultiTests(ITestOutputHelper output)
        {
            this.output = output;
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
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(0, cd.ActualWidth));
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => Assert.Equal(0, cd.ActualWidth));
        }

        // [Fact]
        // public void Size_Propagation_Is_Constrained_To_Innermost_Scope()
        // {
        //     var grids = new[] { CreateGrid("A", null), CreateGrid(("A", new GridLength(30)), (null, new GridLength())) };
        //     var innerScope = new Grid();
        //     foreach(var xgrids in grids)
        //     innerScope.Children.Add(xgrids);
        //     innerScope.SetValue(Grid.IsSharedSizeScopeProperty, true);

        //     var outerGrid = CreateGrid(("A", new GridLength(0)));
        //     var outerScope = new Grid();
        //     outerScope.Children.Add(outerGrid);
        //     outerScope.Children.Add(innerScope);

        //     var root = new Grid();
        //     root.SetValue(Grid.IsSharedSizeScopeProperty, true);
        //     root.Children.Add(outerScope);

        //     root.Measure(new Size(50, 50));
        //     root.Arrange(new Rect(new Point(), new Point(50, 50)));
        //     Assert.Equal(1, outerGrid.ColumnDefinitions[0].ActualWidth);
        // }

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
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(40, cd.ActualWidth));

            grid.ColumnDefinitions.RemoveAt(2);

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(30, cd.ActualWidth));

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

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(new Point(), new Point(200, 200)));
            PrintColumnDefinitions(grid);
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(0, cd.ActualWidth));
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
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Children.Add(scope);

            grid.Measure(new Size(100, 100));
            grid.Arrange(new Rect(new Point(), new Point(100, 100)));
            PrintColumnDefinitions(grid);
            // all in group are equal to the first fixed column
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(19, cd.ActualWidth - 1));

            grid.ColumnDefinitions[0].SharedSizeGroup = null;

            grid.Measure(new Size(100, 100));
            grid.Arrange(new Rect(new Point(), new Point(100, 100)));
            PrintColumnDefinitions(grid);
            // all in group are equal to width (MinWidth) of the sizer in the second column
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(20, cd.ActualWidth));

            grid.ColumnDefinitions[1].SharedSizeGroup = null;

            grid.Measure(new Size(double.PositiveInfinity, 100));
            grid.Arrange(new Rect(new Point(), new Point(100, 100)));
            PrintColumnDefinitions(grid);
            // with no constraint star columns default to the MinWidth of the sizer in the column
            Assert.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => Assert.Equal(0, cd.ActualWidth));
        }

        // grid creators
        // private Grid CreateGrid(params string[] columnGroups)
        // {
        //     return CreateGrid(columnGroups.Select(s => (s, (double)ColumnDefinition.WidthProperty.DefaultMetadata.DefaultValue)).ToArray());
        // }  

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
            return ctrl;
        }
    }


}