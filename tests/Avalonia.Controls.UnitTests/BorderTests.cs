using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class BorderTests
    {
        [Fact]
        public void Measure_Should_Return_BorderThickness_Plus_Padding_When_No_Child_Present()
        {
            var target = new Border
            {
                Padding = new Thickness(6),
                BorderThickness = new Thickness(4)
            };

            target.Measure(new Size(100, 100));

            Assert.Equal(new Size(20, 20), target.DesiredSize);
        }

        [Fact]
        public void Child_Should_Arrange_With_Zero_Height_Width_If_Padding_Greater_Than_Child_Size()
        {
            Border content;

            var target = new Border
            {
                Padding = new Thickness(6),
                MaxHeight = 12,
                MaxWidth = 12,
                Child = content = new Border
                {
                    Height = 0,
                    Width = 0
                }
            };

            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Rect(6, 6, 0, 0), content.Bounds);
        }
        
        public class UseLayoutRounding
        {
            [Fact]
            public void Measure_Rounds_Padding()
            {
                var target = new Border 
                { 
                    Padding = new Thickness(1),
                    Child = new Canvas
                    {
                        Width = 101,
                        Height = 101,
                    }
                };

                var root = CreateRoot(1.5, target);

                root.LayoutManager.ExecuteInitialLayoutPass();

                // - 1 pixel padding is rounded up to 1.3333; for both sides it is 2.6666
                // - Size of 101 gets rounded up to 101.3333
                // - Desired size = 101.3333 + 2.6666 = 104
                Assert.Equal(new Size(104, 104), target.DesiredSize);
            }

            [Fact]
            public void Measure_Rounds_BorderThickness()
            {
                var target = new Border
                {
                    BorderThickness = new Thickness(1),
                    Child = new Canvas
                    {
                        Width = 101,
                        Height = 101,
                    }
                };

                var root = CreateRoot(1.5, target);

                root.LayoutManager.ExecuteInitialLayoutPass();

                // - 1 pixel border thickness is rounded up to 1.3333; for both sides it is 2.6666
                // - Size of 101 gets rounded up to 101.3333
                // - Desired size = 101.3333 + 2.6666 = 104
                Assert.Equal(new Size(104, 104), target.DesiredSize);
            }

            [Fact]
            public void Measure_Arranges_Child_To_Rounded_BorderThickness()
            {
                Canvas child;
                var target = new Border
                {
                    BorderThickness = new Thickness(1),
                    Width = 82,
                    Height = 82,
                    Child = child = new Canvas(),
                };

                var root = CreateRoot(1.5, target);

                root.LayoutManager.ExecuteInitialLayoutPass();

                // - 1 pixel border thickness is rounded up to 1.3333; for both sides it is 2.6666
                // - Size of 82 needs no rounding
                // - Minus border thickness, space for child is 82 - 2.6666 = 79.3333
                Assert.Equal(1.3333, child.Bounds.Left, 3);
                Assert.Equal(1.3333, child.Bounds.Top, 3);
                Assert.Equal(79.3333, child.Bounds.Width, 3);
                Assert.Equal(79.3333, child.Bounds.Height, 3);
            }

            [Fact]
            public void Measure_Arranges_Child_With_Rounded_Margin()
            {
                Border child;
                var target = new Border
                {
                    Width = 220,
                    Height = 220,
                    Child = child = new Border
                    {
                        Margin = new Thickness(0, 25, 25, 25),
                    },
                };

                var root = CreateRoot(1.5, target);

                root.LayoutManager.ExecuteInitialLayoutPass();

                // - 25 margin gets rounded up to 25.3333
                // - Size of 220 needs no rounding
                Assert.Equal(0, child.Bounds.Left, 3);
                Assert.Equal(25.3333, child.Bounds.Top, 3);
                Assert.Equal(194.6666, child.Bounds.Width, 3);
                Assert.Equal(169.3333, child.Bounds.Height, 3);
            }

            private static TestRoot CreateRoot(
                double scaling,
                Control child,
                Size? constraint = null)
            {
                return new TestRoot
                {
                    LayoutScaling = scaling,
                    UseLayoutRounding = true,
                    Child = child,
                    ClientSize = constraint ?? new Size(1000, 1000),
                };
            }
        }
    }
}
