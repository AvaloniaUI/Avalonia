﻿using Avalonia.Controls.Shapes;
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
    }
}
