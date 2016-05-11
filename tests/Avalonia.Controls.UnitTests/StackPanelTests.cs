// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class StackPanelTests
    {
        [Fact]
        public void Lays_Out_Children_Vertically()
        {
            var target = new StackPanel
            {
                Children = new Controls
                {
                    new Border { Height = 20, Width = 120 },
                    new Border { Height = 30 },
                    new Border { Height = 50 },
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(120, 100), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 120, 20), target.Children[0].Bounds);
            Assert.Equal(new Rect(0, 20, 120, 30), target.Children[1].Bounds);
            Assert.Equal(new Rect(0, 50, 120, 50), target.Children[2].Bounds);
        }

        [Fact]
        public void Lays_Out_Children_Horizontally()
        {
            var target = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children = new Controls
                {
                    new Border { Width = 20, Height = 120 },
                    new Border { Width = 30 },
                    new Border { Width = 50 },
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(100, 120), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 20, 120), target.Children[0].Bounds);
            Assert.Equal(new Rect(20, 0, 30, 120), target.Children[1].Bounds);
            Assert.Equal(new Rect(50, 0, 50, 120), target.Children[2].Bounds);
        }

        [Fact]
        public void Lays_Out_Children_Vertically_With_Gap()
        {
            var target = new StackPanel
            {
                Gap = 10,
                Children = new Controls
                {
                    new Border { Height = 20, Width = 120 },
                    new Border { Height = 30 },
                    new Border { Height = 50 },
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(120, 130), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 120, 20), target.Children[0].Bounds);
            Assert.Equal(new Rect(0, 30, 120, 30), target.Children[1].Bounds);
            Assert.Equal(new Rect(0, 70, 120, 50), target.Children[2].Bounds);
        }

        [Fact]
        public void Lays_Out_Children_Horizontally_With_Gap()
        {
            var target = new StackPanel
            {
                Gap = 10,
                Orientation = Orientation.Horizontal,
                Children = new Controls
                {
                    new Border { Width = 20, Height = 120 },
                    new Border { Width = 30 },
                    new Border { Width = 50 },
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(130, 120), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 20, 120), target.Children[0].Bounds);
            Assert.Equal(new Rect(30, 0, 30, 120), target.Children[1].Bounds);
            Assert.Equal(new Rect(70, 0, 50, 120), target.Children[2].Bounds);
        }

        [Fact]
        public void Lays_Out_Children_Vertically_Even_If_Larger_Than_Panel()
        {
            var target = new StackPanel
            {
                Height = 60,
                Children = new Controls
                {
                    new Border { Height = 20, Width = 120 },
                    new Border { Height = 30 },
                    new Border { Height = 50 },
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(120, 60), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 120, 20), target.Children[0].Bounds);
            Assert.Equal(new Rect(0, 20, 120, 30), target.Children[1].Bounds);
            Assert.Equal(new Rect(0, 50, 120, 50), target.Children[2].Bounds);
        }

        [Fact]
        public void Lays_Out_Children_Horizontally_Even_If_Larger_Than_Panel()
        {
            var target = new StackPanel
            {
                Width = 60,
                Orientation = Orientation.Horizontal,
                Children = new Controls
                {
                    new Border { Width = 20, Height = 120 },
                    new Border { Width = 30 },
                    new Border { Width = 50 },
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(60, 120), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 20, 120), target.Children[0].Bounds);
            Assert.Equal(new Rect(20, 0, 30, 120), target.Children[1].Bounds);
            Assert.Equal(new Rect(50, 0, 50, 120), target.Children[2].Bounds);
        }
    }
}
