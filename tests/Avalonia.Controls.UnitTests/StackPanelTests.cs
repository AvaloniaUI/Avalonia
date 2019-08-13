// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Avalonia.Layout;
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
                Children =
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
                Children =
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
        public void Lays_Out_Children_Vertically_With_Spacing()
        {
            var target = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    new Border { Height = 20, Width = 120 },
                    new Border { Height = 30 },
                    new Border { Height = 50 },
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(120, 120), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 120, 20), target.Children[0].Bounds);
            Assert.Equal(new Rect(0, 30, 120, 30), target.Children[1].Bounds);
            Assert.Equal(new Rect(0, 70, 120, 50), target.Children[2].Bounds);
        }

        [Fact]
        public void Lays_Out_Children_Horizontally_With_Spacing()
        {
            var target = new StackPanel
            {
                Spacing = 10,
                Orientation = Orientation.Horizontal,
                Children =
                {
                    new Border { Width = 20, Height = 120 },
                    new Border { Width = 30 },
                    new Border { Width = 50 },
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(120, 120), target.Bounds.Size);
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
                Children =
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
                Children =
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

        [Fact]
        public void Arranges_Vertical_Children_With_Correct_Bounds()
        {
            var target = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Children =
                {
                    new TestControl
                    {
                        HorizontalAlignment = HorizontalAlignment.Left,
                        MeasureSize = new Size(50, 10),
                    },
                    new TestControl
                    {
                        HorizontalAlignment = HorizontalAlignment.Left,
                        MeasureSize = new Size(150, 10),
                    },
                    new TestControl
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        MeasureSize = new Size(50, 10),
                    },
                    new TestControl
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        MeasureSize = new Size(150, 10),
                    },
                    new TestControl
                    {
                        HorizontalAlignment = HorizontalAlignment.Right,
                        MeasureSize = new Size(50, 10),
                    },
                    new TestControl
                    {
                        HorizontalAlignment = HorizontalAlignment.Right,
                        MeasureSize = new Size(150, 10),
                    },
                    new TestControl
                    {
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        MeasureSize = new Size(50, 10),
                    },
                    new TestControl
                    {
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        MeasureSize = new Size(150, 10),
                    },
                }
            };

            target.Measure(new Size(100, 150));
            Assert.Equal(new Size(100, 80), target.DesiredSize);

            target.Arrange(new Rect(target.DesiredSize));

            var bounds = target.Children.Select(x => x.Bounds).ToArray();

            Assert.Equal(
                new[]
                {
                    new Rect(0, 0, 50, 10),
                    new Rect(0, 10, 100, 10),
                    new Rect(25, 20, 50, 10),
                    new Rect(0, 30, 100, 10),
                    new Rect(50, 40, 50, 10),
                    new Rect(0, 50, 100, 10),
                    new Rect(0, 60, 100, 10),
                    new Rect(0, 70, 100, 10),

                }, bounds);
        }

        [Fact]
        public void Arranges_Horizontal_Children_With_Correct_Bounds()
        {
            var target = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children =
                {
                    new TestControl
                    {
                        VerticalAlignment = VerticalAlignment.Top,
                        MeasureSize = new Size(10, 50),
                    },
                    new TestControl
                    {
                        VerticalAlignment = VerticalAlignment.Top,
                        MeasureSize = new Size(10, 150),
                    },
                    new TestControl
                    {
                        VerticalAlignment = VerticalAlignment.Center,
                        MeasureSize = new Size(10, 50),
                    },
                    new TestControl
                    {
                        VerticalAlignment = VerticalAlignment.Center,
                        MeasureSize = new Size(10, 150),
                    },
                    new TestControl
                    {
                        VerticalAlignment = VerticalAlignment.Bottom,
                        MeasureSize = new Size(10, 50),
                    },
                    new TestControl
                    {
                        VerticalAlignment = VerticalAlignment.Bottom,
                        MeasureSize = new Size(10, 150),
                    },
                    new TestControl
                    {
                        VerticalAlignment = VerticalAlignment.Stretch,
                        MeasureSize = new Size(10, 50),
                    },
                    new TestControl
                    {
                        VerticalAlignment = VerticalAlignment.Stretch,
                        MeasureSize = new Size(10, 150),
                    },
                }
            };

            target.Measure(new Size(150, 100));
            Assert.Equal(new Size(80, 100), target.DesiredSize);

            target.Arrange(new Rect(target.DesiredSize));

            var bounds = target.Children.Select(x => x.Bounds).ToArray();

            Assert.Equal(
                new[]
                {
                    new Rect(0, 0, 10, 50),
                    new Rect(10, 0, 10, 100),
                    new Rect(20, 25, 10, 50),
                    new Rect(30, 0, 10, 100),
                    new Rect(40, 50, 10, 50),
                    new Rect(50, 0, 10, 100),
                    new Rect(60, 0, 10, 100),
                    new Rect(70, 0, 10, 100),
                }, bounds);
        }

        [Theory]
        [InlineData(Orientation.Horizontal)]
        [InlineData(Orientation.Vertical)]
        public void Spacing_Not_Added_For_Invisible_Children(Orientation orientation)
        {
            var targetThreeChildrenOneInvisble = new StackPanel
            {
                Spacing = 40,
                Orientation = orientation,
                Children =
                {
                    new StackPanel { Width = 10, Height= 10, IsVisible = false },
                    new StackPanel { Width = 10, Height= 10 },
                    new StackPanel { Width = 10, Height= 10 },
                }
            };
            var targetTwoChildrenNoneInvisible = new StackPanel
            {
                Spacing = 40,
                Orientation = orientation,
                Children =
                {
                    new StackPanel { Width = 10, Height= 10 },
                    new StackPanel { Width = 10, Height= 10 }
                }
            };

            targetThreeChildrenOneInvisble.Measure(Size.Infinity);
            targetThreeChildrenOneInvisble.Arrange(new Rect(targetThreeChildrenOneInvisble.DesiredSize));

            targetTwoChildrenNoneInvisible.Measure(Size.Infinity);
            targetTwoChildrenNoneInvisible.Arrange(new Rect(targetTwoChildrenNoneInvisible.DesiredSize));

            Size sizeWithTwoChildren = targetTwoChildrenNoneInvisible.Bounds.Size;
            Size sizeWithThreeChildren = targetThreeChildrenOneInvisble.Bounds.Size;

            Assert.Equal(sizeWithTwoChildren, sizeWithThreeChildren);
        }

        private class TestControl : Control
        {
            public Size MeasureConstraint { get; private set; }
            public Size MeasureSize { get; set; }

            protected override Size MeasureOverride(Size availableSize)
            {
                MeasureConstraint = availableSize;
                return MeasureSize;
            }
        }
    }
}
