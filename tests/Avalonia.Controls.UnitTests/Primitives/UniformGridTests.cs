using Avalonia.Controls.Primitives;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Primitives
{
    public class UniformGridTests
    {
        [Fact]
        public void Grid_Columns_Equals_Rows_For_Auto_Columns_And_Rows()
        {
            var target = new UniformGrid()
            {
                Children =
                {
                    new Border { Width = 50, Height = 70 },
                    new Border { Width = 30, Height = 50 },
                    new Border { Width = 80, Height = 90 }
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            // 2 * 2 grid
            Assert.Equal(new Size(2 * 80, 2 * 90), target.Bounds.Size);
        }

        [Fact]
        public void Grid_Expands_Vertically_For_Columns_With_Auto_Rows()
        {
            var target = new UniformGrid()
            {
                Columns = 2,
                Children =
                {
                    new Border { Width = 50, Height = 70 },
                    new Border { Width = 30, Height = 50 },
                    new Border { Width = 80, Height = 90 },
                    new Border { Width = 20, Height = 30 },
                    new Border { Width = 40, Height = 60 }
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            // 2 * 3 grid
            Assert.Equal(new Size(2 * 80, 3 * 90), target.Bounds.Size);
        }

        [Fact]
        public void Grid_Extends_For_Columns_And_First_Column_With_Auto_Rows()
        {
            var target = new UniformGrid()
            {
                Columns = 3,
                FirstColumn = 2,
                Children =
                {
                    new Border { Width = 50, Height = 70 },
                    new Border { Width = 30, Height = 50 },
                    new Border { Width = 80, Height = 90 },
                    new Border { Width = 20, Height = 30 },
                    new Border { Width = 40, Height = 60 }
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            // 3 * 3 grid
            Assert.Equal(new Size(3 * 80, 3 * 90), target.Bounds.Size);
        }

        [Fact]
        public void Grid_Expands_Horizontally_For_Rows_With_Auto_Columns()
        {
            var target = new UniformGrid()
            {
                Rows = 2,
                Children =
                {
                    new Border { Width = 50, Height = 70 },
                    new Border { Width = 30, Height = 50 },
                    new Border { Width = 80, Height = 90 },
                    new Border { Width = 20, Height = 30 },
                    new Border { Width = 40, Height = 60 }
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            // 3 * 2 grid
            Assert.Equal(new Size(3 * 80, 2 * 90), target.Bounds.Size);
        }

        [Fact]
        public void Grid_Size_Is_Limited_By_Rows_And_Columns()
        {
            var target = new UniformGrid()
            {
                Columns = 2,
                Rows = 2,
                Children =
                {
                    new Border { Width = 50, Height = 70 },
                    new Border { Width = 30, Height = 50 },
                    new Border { Width = 80, Height = 90 },
                    new Border { Width = 20, Height = 30 },
                    new Border { Width = 40, Height = 60 }
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            // 2 * 2 grid
            Assert.Equal(new Size(2 * 80, 2 * 90), target.Bounds.Size);
        }

        [Fact]
        public void Not_Visible_Children_Are_Ignored()
        {
            var target = new UniformGrid()
            {
                Children =
                {
                    new Border { Width = 50, Height = 70 },
                    new Border { Width = 30, Height = 50 },
                    new Border { Width = 80, Height = 90, IsVisible = false },
                    new Border { Width = 20, Height = 30 },
                    new Border { Width = 40, Height = 60 }
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            // 2 * 2 grid
            Assert.Equal(new Size(2 * 50, 2 * 70), target.Bounds.Size);
        }

        [Fact]
        public void Children_Do_Not_Overlap_With_125_Percent_Scaling_1()
        {
            // Issue #17699
            var target = new UniformGrid
            {
                Columns = 2,
                Children =
                {
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                }
            };

            var root = new TestRoot
            {
                LayoutScaling = 1.25,
                Child = new Border
                {
                    Width = 100,
                    Height = 100,
                    Child = target,
                }
            };

            root.ExecuteInitialLayoutPass();

            Assert.Equal(new(0, 0, 50.4, 50.4), target.Children[0].Bounds);
            Assert.Equal(new(50.4, 0, 49.6, 50.4), target.Children[1].Bounds);
            Assert.Equal(new(0, 50.4, 50.4, 49.6), target.Children[2].Bounds);
            Assert.Equal(new(50.4, 50.4, 49.6, 49.6), target.Children[3].Bounds);
        }

        [Fact]
        public void Children_Do_Not_Overlap_With_125_Percent_Scaling_2()
        {
            // Issue #17699
            var target = new UniformGrid
            {
                Columns = 4,
                Children =
                {
                    new Border(),
                    new Border(),
                    new Border(),
                    new Border(),
                }
            };

            var root = new TestRoot
            {
                LayoutScaling = 1.25,
                Child = new Border
                {
                    Width = 100,
                    Height = 100,
                    Child = target,
                }
            };

            root.ExecuteInitialLayoutPass();

            Assert.Equal(new(0, 0, 25.6, 100), target.Children[0].Bounds);
            Assert.Equal(new(25.6, 0, 24.8, 100), target.Children[1].Bounds);
            Assert.Equal(new(50.4, 0, 24.8, 100), target.Children[2].Bounds);
            Assert.Equal(new(75.2, 0, 24.8, 100), target.Children[3].Bounds);
        }
    }
}
