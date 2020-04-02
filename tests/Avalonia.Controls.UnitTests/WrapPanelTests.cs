using Avalonia.Layout;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class WrapPanelTests
    {
        [Fact]
        public void Lays_Out_Horizontally_On_Separate_Lines()
        {
            var target = new WrapPanel()
            {
                Width = 100,
                Children =
                            {
                                new Border { Height = 50, Width = 100 },
                                new Border { Height = 50, Width = 100 },
                            }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(100, 100), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 100, 50), target.Children[0].Bounds);
            Assert.Equal(new Rect(0, 50, 100, 50), target.Children[1].Bounds);
        }

        [Fact]
        public void Lays_Out_Horizontally_On_A_Single_Line()
        {
            var target = new WrapPanel()
            {
                Width = 200,
                Children =
                            {
                                new Border { Height = 50, Width = 100 },
                                new Border { Height = 50, Width = 100 },
                            }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(200, 50), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 100, 50), target.Children[0].Bounds);
            Assert.Equal(new Rect(100, 0, 100, 50), target.Children[1].Bounds);
        }

        [Fact]
        public void Lays_Out_Vertically_Children_On_A_Single_Line()
        {
            var target = new WrapPanel()
            {
                Orientation = Orientation.Vertical,
                Height = 120,
                Children =
                            {
                                new Border { Height = 50, Width = 100 },
                                new Border { Height = 50, Width = 100 },
                            }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(100, 120), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 100, 50), target.Children[0].Bounds);
            Assert.Equal(new Rect(0, 50, 100, 50), target.Children[1].Bounds);
        }

        [Fact]
        public void Lays_Out_Vertically_On_Separate_Lines()
        {
            var target = new WrapPanel()
            {
                Orientation = Orientation.Vertical,
                Height = 60,
                Children =
                            {
                                new Border { Height = 50, Width = 100 },
                                new Border { Height = 50, Width = 100 },
                            }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(200, 60), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 100, 50), target.Children[0].Bounds);
            Assert.Equal(new Rect(100, 0, 100, 50), target.Children[1].Bounds);
        }

        [Fact]
        public void Applies_ItemWidth_And_ItemHeight_Properties()
        {
            var target = new WrapPanel()
            {
                Orientation = Orientation.Horizontal,
                Width = 50,
                ItemWidth = 20,
                ItemHeight = 15,
                Children =
                            {
                                new Border(),
                                new Border(),
                            }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(50, 15), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 20, 15), target.Children[0].Bounds);
            Assert.Equal(new Rect(20, 0, 20, 15), target.Children[1].Bounds);
        }

        [Fact]
        void ItemWidth_Trigger_InvalidateMeasure()
        {
            var target = new WrapPanel();

            target.Measure(new Size(10, 10));

            Assert.True(target.IsMeasureValid);

            target.ItemWidth = 1;

            Assert.False(target.IsMeasureValid);
        }

        [Fact]
        void ItemHeight_Trigger_InvalidateMeasure()
        {
            var target = new WrapPanel();

            target.Measure(new Size(10, 10));

            Assert.True(target.IsMeasureValid);

            target.ItemHeight = 1;

            Assert.False(target.IsMeasureValid);
        }
    }
}
