// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Xunit;

namespace Avalonia.Controls.UnitTests.Presenters
{
    public class ContentPresenterTests_Layout
    {
        [Theory]
        [InlineData(HorizontalAlignment.Stretch, VerticalAlignment.Stretch, 0, 0, 100, 100)]
        [InlineData(HorizontalAlignment.Left, VerticalAlignment.Stretch, 0, 0, 16, 100)]
        [InlineData(HorizontalAlignment.Right, VerticalAlignment.Stretch, 84, 0, 16, 100)]
        [InlineData(HorizontalAlignment.Center, VerticalAlignment.Stretch, 42, 0, 16, 100)]
        [InlineData(HorizontalAlignment.Stretch, VerticalAlignment.Top, 0, 0, 100, 16)]
        [InlineData(HorizontalAlignment.Stretch, VerticalAlignment.Bottom, 0, 84, 100, 16)]
        [InlineData(HorizontalAlignment.Stretch, VerticalAlignment.Center, 0, 42, 100, 16)]
        public void Content_Alignment_Is_Applied_To_Child_Bounds(
            HorizontalAlignment h,
            VerticalAlignment v,
            double expectedX,
            double expectedY,
            double expectedWidth,
            double expectedHeight)
        {
            Border content;
            var target = new ContentPresenter
            {
                HorizontalContentAlignment = h,
                VerticalContentAlignment = v,
                Content = content = new Border
                {
                    MinWidth = 16,
                    MinHeight = 16,
                },
            };

            target.UpdateChild();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Rect(expectedX, expectedY, expectedWidth, expectedHeight), content.Bounds);
        }

        [Theory]
        [InlineData(HorizontalAlignment.Stretch, VerticalAlignment.Stretch, 10, 10, 80, 80)]
        [InlineData(HorizontalAlignment.Left, VerticalAlignment.Stretch, 10, 10, 16, 80)]
        [InlineData(HorizontalAlignment.Right, VerticalAlignment.Stretch, 74, 10, 16, 80)]
        [InlineData(HorizontalAlignment.Center, VerticalAlignment.Stretch, 42, 10, 16, 80)]
        [InlineData(HorizontalAlignment.Stretch, VerticalAlignment.Top, 10, 10, 80, 16)]
        [InlineData(HorizontalAlignment.Stretch, VerticalAlignment.Bottom, 10, 74, 80, 16)]
        [InlineData(HorizontalAlignment.Stretch, VerticalAlignment.Center, 10, 42, 80, 16)]
        public void Content_Alignment_And_Padding_Are_Applied_To_Child_Bounds(
            HorizontalAlignment h,
            VerticalAlignment v,
            double expectedX,
            double expectedY,
            double expectedWidth,
            double expectedHeight)
        {
            Border content;
            var target = new ContentPresenter
            {
                HorizontalContentAlignment = h,
                VerticalContentAlignment = v,
                Padding = new Thickness(10),
                Content = content = new Border
                {
                    MinWidth = 16,
                    MinHeight = 16,
                },
            };

            target.UpdateChild();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Rect(expectedX, expectedY, expectedWidth, expectedHeight), content.Bounds);
        }

        [Fact]
        public void Should_Correctly_Align_Child_With_Fixed_Size()
        {
            Border content;
            var target = new ContentPresenter
            {
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Content = content = new Border
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Width = 16,
                    Height = 16,
                },
            };

            target.UpdateChild();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            // Check correct result for Issue #1447.
            Assert.Equal(new Rect(0, 84, 16, 16), content.Bounds);
        }

        [Fact]
        public void Content_Can_Be_Stretched()
        {
            Border content;
            var target = new ContentPresenter
            {
                Content = content = new Border
                {
                    MinWidth = 16,
                    MinHeight = 16,
                },
            };

            target.UpdateChild();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Rect(0, 0, 100, 100), content.Bounds);
        }

        [Fact]
        public void Content_Can_Be_Right_Aligned()
        {
            Border content;
            var target = new ContentPresenter
            {
                Content = content = new Border
                {
                    MinWidth = 16,
                    MinHeight = 16,
                    HorizontalAlignment = HorizontalAlignment.Right
                },
            };

            target.UpdateChild();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Rect(84, 0, 16, 100), content.Bounds);
        }

        [Fact]
        public void Content_Can_Be_Bottom_Aligned()
        {
            Border content;
            var target = new ContentPresenter
            {
                Content = content = new Border
                {
                    MinWidth = 16,
                    MinHeight = 16,
                    VerticalAlignment = VerticalAlignment.Bottom,
                },
            };

            target.UpdateChild();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Rect(0, 84, 100, 16), content.Bounds);
        }

        [Fact]
        public void Content_Can_Be_TopLeft_Aligned()
        {
            Border content;
            var target = new ContentPresenter
            {
                Content = content = new Border
                {
                    MinWidth = 16,
                    MinHeight = 16,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                },
            };

            target.UpdateChild();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Rect(84, 0, 16, 16), content.Bounds);
        }

        [Fact]
        public void Content_Can_Be_TopRight_Aligned()
        {
            Border content;
            var target = new ContentPresenter
            {
                Content = content = new Border
                {
                    MinWidth = 16,
                    MinHeight = 16,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                },
            };

            target.UpdateChild();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Rect(84, 0, 16, 16), content.Bounds);
        }

        [Fact]
        public void Child_Arrange_With_Zero_Height_When_Padding_Height_Greater_Than_Child_Height()
        {
            Border content;
            var target = new ContentPresenter
            {
                Padding = new Thickness(32),
                MaxHeight = 32,
                MaxWidth = 32,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Content = content = new Border
                {
                    Height = 0,
                    Width = 0,
                },
            };

            target.UpdateChild();

            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Rect(32, 32, 0, 0), content.Bounds);
        }
    }
}