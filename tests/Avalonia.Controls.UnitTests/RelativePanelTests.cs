using Avalonia.Controls.Shapes;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class RelativePanelTests
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

        [Fact]
        public void ChildCenter_Measures_Correctly()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
            var rect1 = new Rectangle { Height = 20, Width = 20 };
            var rect2 = new Rectangle { Height = 20, Width = 20 };
            var rect3 = new Rectangle { Height = 20, Width = 20 };

            var target = new RelativePanel
            {
                Children =
                {
                    rect1, rect2, rect3
                }
            };

            RelativePanel.SetAlignHorizontalCenterWithPanel(rect1, true);
            RelativePanel.SetRightOf(rect2, rect1);
            RelativePanel.SetRightOf(rect3, rect2);

            target.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            target.Arrange(new Rect(0, 0, target.DesiredSize.Width, target.DesiredSize.Height));

            Assert.Equal(new Size(100, 20), target.Bounds.Size);
            Assert.Equal(new Rect(40, 0, 20, 20), target.Children[0].Bounds);
            Assert.Equal(new Rect(60, 0, 20, 20), target.Children[1].Bounds);
            Assert.Equal(new Rect(80, 0, 20, 20), target.Children[2].Bounds);
        }


        [Fact]
        public void ComplexLayout()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
            var rect1 = new Rectangle { Width = 50, Height = 50 };
            var rect2 = new Rectangle { Width = 50, Height = 30 };
            var rect3 = new Rectangle { Width = 50, Height = 60 };
            var rect4 = new Rectangle { Width = 60, Height = 55 };
            var rect5 = new Rectangle { Width = 76, Height = 55 };
            var rectTop = new Rectangle { Width = 70, Height = 50 };
            var rectBottom = new Rectangle { Width = double.NaN, Height = 50 };

            var target = new RelativePanel
            {
                Children =
                {
                    rect1, rect2, rect3, rect4, rect5, rectTop, rectBottom
                }
            };

            RelativePanel.SetAlignHorizontalCenterWithPanel(rect1, true);
            RelativePanel.SetRightOf(rect2, rect1);
            RelativePanel.SetRightOf(rect3, rect2);
            RelativePanel.SetBelow(rect4, rect1);
            RelativePanel.SetRightOf(rect5, rect4);
            RelativePanel.SetBelow(rect5, rect3);

            RelativePanel.SetAlignLeftWithPanel(rectTop, true);
            RelativePanel.SetAlignTopWithPanel(rectTop, true);
            RelativePanel.SetAlignRightWithPanel(rectTop, true);

            RelativePanel.SetAlignLeftWithPanel(rectBottom, true);
            RelativePanel.SetAlignBottomWithPanel(rectBottom, true);
            RelativePanel.SetAlignRightWithPanel(rectBottom, true);
            
            target.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            target.Arrange(new Rect(0, 0, target.DesiredSize.Width, target.DesiredSize.Height));

            Assert.Equal(new Size(250, 115), target.Bounds.Size);
            Assert.Equal(new Rect(100, 0, 50, 50), target.Children[0].Bounds);
            Assert.Equal(new Rect(150, 0, 50, 30), target.Children[1].Bounds);
            Assert.Equal(new Rect(200, 0, 50, 60), target.Children[2].Bounds);
            Assert.Equal(new Rect(0, 50, 60, 55), target.Children[3].Bounds);
            Assert.Equal(new Rect(60, 60, 76, 55), target.Children[4].Bounds);

            Assert.Equal(new Rect(90, 0, 70, 50), target.Children[5].Bounds);
            Assert.Equal(new Rect(0, 65, 250, 50), target.Children[6].Bounds);
        }
    }
}
