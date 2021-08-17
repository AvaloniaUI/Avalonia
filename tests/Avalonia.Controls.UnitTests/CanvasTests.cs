using System;
using Avalonia.Controls.Shapes;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class CanvasTests
    {
        [Fact]
        public void Left_Property_Should_Work()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            Rectangle rect;
            var target = new Canvas
            {
                Width = 400,
                Height = 400,
                Children =
                {
                    (rect = new Rectangle
                    {
                        MinWidth = 20,
                        MinHeight = 25,
                        [Canvas.LeftProperty] = 30,
                    })
                }
            };

            target.Measure(new Size(400, 400));
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Rect(30, 0, 20, 25), rect.Bounds);
        }

        [Fact]
        public void Top_Property_Should_Work()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            Rectangle rect;
            var target = new Canvas
            {
                Width = 400,
                Height = 400,
                Children =
                {
                    (rect = new Rectangle
                    {
                        MinWidth = 20,
                        MinHeight = 25,
                        [Canvas.TopProperty] = 30,
                    })
                }
            };

            target.Measure(new Size(400, 400));
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Rect(0, 30, 20, 25), rect.Bounds);
        }

        [Fact]
        public void Right_Property_Should_Work()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            Rectangle rect;
            var target = new Canvas
            {
                Width = 400,
                Height = 400,
                Children =
                {
                    (rect = new Rectangle
                    {
                        MinWidth = 20,
                        MinHeight = 25,
                        [Canvas.RightProperty] = 30,
                    })
                }
            };

            target.Measure(new Size(400, 400));
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Rect(350, 0, 20, 25), rect.Bounds);
        }

        [Fact]
        public void Bottom_Property_Should_Work()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            Rectangle rect;
            var target = new Canvas
            {
                Width = 400,
                Height = 400,
                Children =
                {
                    (rect = new Rectangle
                    {
                        MinWidth = 20,
                        MinHeight = 25,
                        [Canvas.BottomProperty] = 30,
                    })
                }
            };

            target.Measure(new Size(400, 400));
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Rect(0, 345, 20, 25), rect.Bounds);
        }
    }
}
