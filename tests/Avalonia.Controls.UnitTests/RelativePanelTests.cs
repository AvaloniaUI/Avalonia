using Avalonia.Controls.Shapes;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class RelativePanelTests : ScopedTestBase
    {
        [Fact]
        public void Lays_Out_1_Child_Next_the_other()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
            var rect1 = new Rectangle { Height = 20, Width = 20 };
            var rect2 = new Rectangle { Height = 20, Width = 20 };

            var target = new RelativePanel
            {
                VerticalAlignment = Layout.VerticalAlignment.Top,
                HorizontalAlignment = Layout.HorizontalAlignment.Left,
                Children =
                {
                    rect1, rect2
                }
            };

            RelativePanel.SetAlignLeftWithPanel(rect1 , true);
            RelativePanel.SetRightOf(rect2, rect1);
            target.Measure(new Size(400, 400));
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(40, 20), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 20, 20), target.Children[0].Bounds);
            Assert.Equal(new Rect(20, 0, 20, 20), target.Children[1].Bounds);
        }

        [Fact]
        public void Lays_Out_1_Child_Below_the_other()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
            var rect1 = new Rectangle { Height = 20, Width = 20 };
            var rect2 = new Rectangle { Height = 20, Width = 20 };

            var target = new RelativePanel
            {
                VerticalAlignment = Layout.VerticalAlignment.Top,
                HorizontalAlignment = Layout.HorizontalAlignment.Left,
                Children =
                {
                    rect1, rect2
                }
            };

            RelativePanel.SetAlignLeftWithPanel(rect1, true);
            RelativePanel.SetBelow(rect2, rect1);
            target.Measure(new Size(400, 400));
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(20, 40), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 20, 20), target.Children[0].Bounds);
            Assert.Equal(new Rect(0, 20, 20, 20), target.Children[1].Bounds);
        }

        [Fact]
        public void RelativePanel_Can_Center()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
            var rect1 = new Rectangle { Height = 20, Width = 20 };
            var rect2 = new Rectangle { Height = 20, Width = 20 };

            var target = new RelativePanel
            {
                VerticalAlignment = Layout.VerticalAlignment.Center,
                HorizontalAlignment = Layout.HorizontalAlignment.Center,
                Children =
                {
                    rect1, rect2
                }
            };

            RelativePanel.SetAlignLeftWithPanel(rect1, true);
            RelativePanel.SetBelow(rect2, rect1);
            target.Measure(new Size(400, 400));
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(20, 40), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 20, 20), target.Children[0].Bounds);
            Assert.Equal(new Rect(0, 20, 20, 20), target.Children[1].Bounds);
        }

        [Fact]
        public void LeftOf_Measures_Correctly()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
            var rect1 = new Rectangle { Height = 20, Width = 20 };
            var rect2 = new Rectangle { Height = 20, Width = 20 };

            var target = new RelativePanel
            {
                VerticalAlignment = Layout.VerticalAlignment.Center,
                HorizontalAlignment = Layout.HorizontalAlignment.Center,
                Children =
                {
                    rect1, rect2
                }
            };

            RelativePanel.SetLeftOf(rect2, rect1);
            target.Measure(new Size(400, 400));
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(20, 20), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 20, 20), target.Children[0].Bounds);
            Assert.Equal(new Rect(-20, 0, 20, 20), target.Children[1].Bounds);
        }

        [Fact]
        public void Above_Measures_Correctly()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
            var rect1 = new Rectangle { Height = 20, Width = 20 };
            var rect2 = new Rectangle { Height = 20, Width = 20 };

            var target = new RelativePanel
            {
                VerticalAlignment = Layout.VerticalAlignment.Center,
                HorizontalAlignment = Layout.HorizontalAlignment.Center,
                Children =
                {
                    rect1, rect2
                }
            };

            RelativePanel.SetAbove(rect2, rect1);
            target.Measure(new Size(400, 400));
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(20, 20), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 20, 20), target.Children[0].Bounds);
            Assert.Equal(new Rect(0, -20, 20, 20), target.Children[1].Bounds);
        }

        [Theory]
        [InlineData(100, 100, 100, 100)]
        [InlineData(100, double.PositiveInfinity, 100, 40)]
        [InlineData(double.PositiveInfinity, 100, 20, 100)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, 20, 40)]
        public void StretchedPanel_Measures_Correctly(double availableWidth, double availableHeight, double desiredWidth, double desiredHeight)
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
            var rect1 = new Rectangle { Height = 20, Width = 20 };
            var rect2 = new Rectangle { Height = 20, Width = 20 };

            var target = new RelativePanel
            {
                VerticalAlignment = Layout.VerticalAlignment.Stretch,
                HorizontalAlignment = Layout.HorizontalAlignment.Stretch,
                Children =
                {
                    rect1, rect2
                }
            };

            RelativePanel.SetBelow(rect2, rect1);
            target.Measure(new Size(availableWidth, availableHeight));
            target.Arrange(new Rect(target.DesiredSize));
            
            Assert.Equal(desiredWidth, target.DesiredSize.Width);
            Assert.Equal(desiredHeight, target.DesiredSize.Height);
        }

        [Fact]
        public void Lays_Out_A_Chain_From_The_Right_Edge_Inside_Another_RelativePanel()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
            var right = new Rectangle { Height = 20, Width = 20 };
            var middle = new Rectangle { Height = 20, Width = 20 };
            var left = new Rectangle { Height = 20, Width = 20 };

            RelativePanel.SetAlignRightWithPanel(right, true);
            RelativePanel.SetLeftOf(middle, right);
            RelativePanel.SetLeftOf(left, middle);

            // Declared the way they read in markup, each one before the child it points at.
            var target = new RelativePanel
            {
                Children =
                {
                    left, middle, right
                }
            };

            var outer = new RelativePanel { Children = { target } };
            RelativePanel.SetAlignTopWithPanel(target, true);

            outer.Measure(new Size(400, 400));
            outer.Arrange(new Rect(0, 0, 400, 400));

            Assert.Equal(right.Bounds.Left, middle.Bounds.Right);
            Assert.Equal(middle.Bounds.Left, left.Bounds.Right);
        }

        [Fact]
        public void Measures_A_LeftOf_Chain_At_Infinite_Width()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var right = new Rectangle { Width = 80, Height = 40 };
            var middle = new Rectangle { Width = 80, Height = 40 };
            var left = new Rectangle { Width = 80, Height = 40 };

            RelativePanel.SetAlignRightWithPanel(right, true);
            RelativePanel.SetLeftOf(middle, right);
            RelativePanel.SetLeftOf(left, middle);

            var target = new RelativePanel { Children = { right, middle, left } };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(240, 40), target.DesiredSize);
            Assert.Equal(0, left.Bounds.Left);
        }

        [Fact]
        public void Measures_A_LeftOf_Chain_With_A_Tall_Far_Child_At_Infinite_Width()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var right = new Rectangle { Width = 80, Height = 40 };
            var middle = new Rectangle { Width = 80, Height = 40 };
            var left = new Rectangle { Width = 80, Height = 200 };

            RelativePanel.SetAlignRightWithPanel(right, true);
            RelativePanel.SetLeftOf(middle, right);
            RelativePanel.SetLeftOf(left, middle);

            var target = new RelativePanel { Children = { right, middle, left } };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(240, 200), target.DesiredSize);
            Assert.Equal(new Point(0, 0), left.Bounds.Position);
        }

        [Fact]
        public void Measures_An_Above_Chain_At_Infinite_Height()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var bottom = new Rectangle { Width = 80, Height = 40 };
            var middle = new Rectangle { Width = 80, Height = 40 };
            var top = new Rectangle { Width = 80, Height = 40 };

            RelativePanel.SetAlignBottomWithPanel(bottom, true);
            RelativePanel.SetAbove(middle, bottom);
            RelativePanel.SetAbove(top, middle);

            var target = new RelativePanel { Children = { bottom, middle, top } };

            target.Measure(new Size(400, double.PositiveInfinity));
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(120, target.DesiredSize.Height);
            Assert.Equal(0, top.Bounds.Top);
        }

        [Fact]
        public void Measures_A_Chain_Declared_Before_Its_Targets_At_Infinite_Width()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var logo = new Rectangle { Width = 80, Height = 40 };
            var min = new Rectangle { Width = 80, Height = 40 };
            var full = new Rectangle { Width = 80, Height = 40 };
            var close = new Rectangle { Width = 80, Height = 40 };

            RelativePanel.SetAlignLeftWithPanel(logo, true);
            RelativePanel.SetLeftOf(min, full);
            RelativePanel.SetLeftOf(full, close);
            RelativePanel.SetAlignRightWithPanel(close, true);

            // Declared the way they read in markup, each one before the child it points at.
            var target = new RelativePanel { Children = { logo, min, full, close } };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(close.Bounds.Left, full.Bounds.Right);
            Assert.Equal(full.Bounds.Left, min.Bounds.Right);
        }
    }
}
