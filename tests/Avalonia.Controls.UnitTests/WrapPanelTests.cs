using System;
using System.Numerics;
using Avalonia.Layout;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class WrapPanelTests : ScopedTestBase
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

        public static TheoryData<Orientation, WrapPanelItemsAlignment> GetItemsAlignmentValues()
        {
            var data = new TheoryData<Orientation, WrapPanelItemsAlignment>();
            foreach (var orientation in Enum.GetValues<Orientation>())
            {
                foreach (var alignment in Enum.GetValues<WrapPanelItemsAlignment>())
                {
                    data.Add(orientation, alignment);
                }
            }
            return data;
        }

        [Theory, MemberData(nameof(GetItemsAlignmentValues))]
        public void Lays_Out_With_Items_Alignment(Orientation orientation, WrapPanelItemsAlignment itemsAlignment)
        {
            var lineHeight = 50d;
            var target = new WrapPanel
            {
                Width = 200,
                Height = 200,
                Orientation = orientation,
                ItemsAlignment = itemsAlignment,
                UseLayoutRounding = false
            };

            if (orientation is Orientation.Horizontal)
            {
                target.ItemHeight = lineHeight;
                target.Children.Add(new Border { MinWidth = 50 });
                target.Children.Add(new Border { MinWidth = 100 });
                target.Children.Add(new Border { MinWidth = 150 });
            }
            else
            {
                target.ItemWidth = lineHeight;
                target.Children.Add(new Border { MinHeight = 50 });
                target.Children.Add(new Border { MinHeight = 100 });
                target.Children.Add(new Border { MinHeight = 150 });
            }

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(200, 200), target.Bounds.Size);

            var row1Bounds = target.Children[0].Bounds.Union(target.Children[1].Bounds);
            var row2Bounds = target.Children[2].Bounds;

            // fix layout rounding
            row1Bounds = new Rect(
                Math.Round(row1Bounds.X),
                Math.Round(row1Bounds.Y),
                Math.Round(row1Bounds.Width),
                Math.Round(row1Bounds.Height));

            if (orientation is Orientation.Vertical)
            {
                // X <=> Y, Width <=> Height
                var reflectionMatrix = new Matrix4x4(
                    0, 1, 0, 0,  // X' = Y
                    1, 0, 0, 0,  // Y' = X
                    0, 0, 1, 0,  // Z' = Z
                    0, 0, 0, 1   // W' = W
                );
                row1Bounds = row1Bounds.TransformToAABB(reflectionMatrix);
                row2Bounds = row2Bounds.TransformToAABB(reflectionMatrix);
            }

            Assert.Equal(itemsAlignment switch
            {
                WrapPanelItemsAlignment.Stretch or WrapPanelItemsAlignment.Justify /*or WrapPanelItemsAlignment.StretchAll*/ => new(0, 0, 200, lineHeight),
                WrapPanelItemsAlignment.Center => new(25, 0, 150, lineHeight),
                WrapPanelItemsAlignment.End => new(50, 0, 150, lineHeight),
                _ => new(0, 0, 150, lineHeight)
            }, row1Bounds);

            Assert.Equal(itemsAlignment switch
            {
                // WrapPanelItemsAlignment.StretchAll => new(0, lineHeight, 200, lineHeight),
                WrapPanelItemsAlignment.Center => new(25, lineHeight, 150, lineHeight),
                WrapPanelItemsAlignment.End => new(50, lineHeight, 150, lineHeight),
                _ => new(0, 50, 150, 50)
            }, row2Bounds);
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
        public void Lays_Out_Horizontally_On_Separate_Lines_With_Spacing()
        {
            var target = new WrapPanel
            {
                Width = 100,
                ItemSpacing = 10,
                LineSpacing = 20,
                Children =
                {
                    new Border { Height = 50, Width = 60 }, // line 0
                    new Border { Height = 50, Width = 30 }, // line 0
                    new Border { Height = 50, Width = 70 }, // line 1
                    new Border { Height = 50, Width = 30 }, // line 2
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(100, 190), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 60, 50), target.Children[0].Bounds);
            Assert.Equal(new Rect(70, 0, 30, 50), target.Children[1].Bounds);
            Assert.Equal(new Rect(0, 70, 70, 50), target.Children[2].Bounds);
            Assert.Equal(new Rect(0, 140, 30, 50), target.Children[3].Bounds);
        }

        [Fact]
        public void Lays_Out_Horizontally_On_Separate_Lines_With_Spacing_Invisible()
        {
            var target = new WrapPanel
            {
                ItemSpacing = 10,
                Children =
                {
                    new Border { Height = 50, Width = 60 }, // line 0
                    new Border { Height = 50, Width = 30 , IsVisible = false }, // line 0
                    new Border { Height = 50, Width = 50 }, // line 0
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(120, 50), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 60, 50), target.Children[0].Bounds);
            Assert.Equal(new Rect(70, 0, 50, 50), target.Children[2].Bounds);
        }

        [Fact]
        public void Lays_Out_Horizontally_On_Separate_Lines_With_Spacing_Vertical()
        {
            var target = new WrapPanel
            {
                Height = 100,
                Orientation = Orientation.Vertical,
                ItemSpacing = 10,
                LineSpacing = 20,
                Children =
                {
                    new Border { Width = 50, Height = 60 }, // line 0
                    new Border { Width = 50, Height = 30 }, // line 0
                    new Border { Width = 50, Height = 70 }, // line 1
                    new Border { Width = 50, Height = 30 }, // line 2
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(190, 100), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 50, 60), target.Children[0].Bounds);
            Assert.Equal(new Rect(0, 70, 50, 30), target.Children[1].Bounds);
            Assert.Equal(new Rect(70, 0, 50, 70), target.Children[2].Bounds);
            Assert.Equal(new Rect(140, 0, 50, 30), target.Children[3].Bounds);
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
        public void Zero_Size_Visible_Child()
        {
            var target = new WrapPanel()
            {
                Orientation = Orientation.Horizontal,
                Width = 50,
                ItemSpacing = 10,
                LineSpacing = 10,
                Children =
                {
                    new Border(), // line 0
                    new Border // line 1
                    {
                        Width = 50,
                        Height = 50 
                    },
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(50, 60), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 0, 0), target.Children[0].Bounds);
            Assert.Equal(new Rect(0, 10, 50, 50), target.Children[1].Bounds);
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
