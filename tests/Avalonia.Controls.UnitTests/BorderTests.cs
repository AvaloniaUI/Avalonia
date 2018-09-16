// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.UnitTests;
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

        [Fact]
        public void Changing_Background_Brush_Color_Should_Invalidate_Visual()
        {
            var target = new Border()
            {
                Background = new SolidColorBrush(Colors.Red),
            };

            var root = new TestRoot(target);
            var renderer = Mock.Get(root.Renderer);
            renderer.ResetCalls();

            ((SolidColorBrush)target.Background).Color = Colors.Green;

            renderer.Verify(x => x.AddDirty(target), Times.Once);
        }
    }
}
