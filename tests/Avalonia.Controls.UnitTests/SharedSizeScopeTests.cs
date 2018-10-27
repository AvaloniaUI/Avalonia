using System.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.UnitTests;

using Moq;

using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class SharedSizeScopeTests
    {
        public SharedSizeScopeTests()
        {
        }

        [Fact]
        public void All_Descendant_Grids_Are_Registered_When_Added_After_Setting_Scope()
        {
            var grids = new[] { new Grid(), new Grid(), new Grid() };
            var scope = new Panel();
            scope.Children.AddRange(grids);

            var root = new TestRoot();
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Child = scope;

            Assert.All(grids, g => Assert.True(g.HasSharedSizeScope()));
        }

        [Fact]
        public void All_Descendant_Grids_Are_Registered_When_Setting_Scope()
        {
            var grids = new[] { new Grid(), new Grid(), new Grid() };
            var scope = new Panel();
            scope.Children.AddRange(grids);

            var root = new TestRoot();
            root.Child = scope;
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);

            Assert.All(grids, g => Assert.True(g.HasSharedSizeScope()));
        }

        [Fact]
        public void All_Descendant_Grids_Are_Unregistered_When_Resetting_Scope()
        {
            var grids = new[] { new Grid(), new Grid(), new Grid() };
            var scope = new Panel();
            scope.Children.AddRange(grids);

            var root = new TestRoot();
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Child = scope;

            Assert.All(grids, g => Assert.True(g.HasSharedSizeScope()));
            root.SetValue(Grid.IsSharedSizeScopeProperty, false);
            Assert.All(grids, g => Assert.False(g.HasSharedSizeScope()));
            Assert.Equal(null, root.GetValue(Grid.s_sharedSizeScopeHostProperty));
        }

        [Fact]
        public void Size_Is_Propagated_Between_Grids()
        {
            var grids = new[] { CreateGrid("A", null),CreateGrid(("A",new GridLength(30)), (null, new GridLength()))};
            var scope = new Panel();
            scope.Children.AddRange(grids);

            var root = new TestRoot();
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Child = scope;

            root.Measure(new Size(50, 50));
            root.Arrange(new Rect(new Point(), new Point(50, 50)));
            Assert.Equal(30, grids[0].ColumnDefinitions[0].ActualWidth);
        }

        [Fact]
        public void Size_Propagation_Is_Constrained_To_Innermost_Scope()
        {
            var grids = new[] { CreateGrid("A", null), CreateGrid(("A", new GridLength(30)), (null, new GridLength())) };
            var innerScope = new Panel();
            innerScope.Children.AddRange(grids);
            innerScope.SetValue(Grid.IsSharedSizeScopeProperty, true);

            var outerGrid = CreateGrid(("A", new GridLength(0)));
            var outerScope = new Panel();
            outerScope.Children.AddRange(new[] { outerGrid, innerScope });

            var root = new TestRoot();
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Child = outerScope;

            root.Measure(new Size(50, 50));
            root.Arrange(new Rect(new Point(), new Point(50, 50)));
            Assert.Equal(0, outerGrid.ColumnDefinitions[0].ActualWidth);
        }

        [Fact]
        public void Size_Is_Propagated_Between_Rows_And_Columns()
        {
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,30"),
                RowDefinitions = new RowDefinitions("*,10")
            };

            grid.ColumnDefinitions[1].SharedSizeGroup = "A";
            grid.RowDefinitions[1].SharedSizeGroup = "A";

            var root = new TestRoot();
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Child = grid;

            root.Measure(new Size(50, 50));
            root.Arrange(new Rect(new Point(), new Point(50, 50)));
            Assert.Equal(30, grid.RowDefinitions[1].ActualHeight);
        }

        [Fact]
        public void Size_Group_Changes_Are_Tracked()
        {
            var grids = new[] {
                CreateGrid((null, new GridLength(0, GridUnitType.Auto)), (null, new GridLength())),
                CreateGrid(("A", new GridLength(30)), (null, new GridLength())) };
            var scope = new Panel();
            scope.Children.AddRange(grids);

            var root = new TestRoot();
            root.SetValue(Grid.IsSharedSizeScopeProperty, true);
            root.Child = scope;

            root.Measure(new Size(50, 50));
            root.Arrange(new Rect(new Point(), new Point(50, 50)));
            Assert.Equal(0, grids[0].ColumnDefinitions[0].ActualWidth);

            grids[0].ColumnDefinitions[0].SharedSizeGroup = "A";

            root.Measure(new Size(51, 51));
            root.Arrange(new Rect(new Point(), new Point(51, 51)));
            Assert.Equal(30, grids[0].ColumnDefinitions[0].ActualWidth);

            grids[0].ColumnDefinitions[0].SharedSizeGroup = null;

            root.Measure(new Size(52, 52));
            root.Arrange(new Rect(new Point(), new Point(52, 52)));
            Assert.Equal(0, grids[0].ColumnDefinitions[0].ActualWidth);
        }

        // grid creators
        private Grid CreateGrid(params string[] columnGroups)
        {
            return CreateGrid(columnGroups.Select(s => (s, ColumnDefinition.WidthProperty.GetDefaultValue(typeof(ColumnDefinition)))).ToArray());
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
            var columnDefinitions = new ColumnDefinitions();

            columnDefinitions.AddRange(
                    columns.Select(c => new ColumnDefinition
                    {
                        SharedSizeGroup = c.name,
                        Width = c.width,
                        MinWidth = c.minWidth,
                        MaxWidth = c.maxWidth
                    })
                );
            var grid = new Grid
            {
                ColumnDefinitions = columnDefinitions
            };

            return grid;
        }
    }
}
