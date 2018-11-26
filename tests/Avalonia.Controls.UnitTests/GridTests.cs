// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Layout;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class GridTests
    {
        [Fact]
        public void Calculates_Colspan_Correctly()
        {
            var target = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(new GridLength(4, GridUnitType.Pixel)),
                    new ColumnDefinition(GridLength.Auto),
                },
                RowDefinitions = new RowDefinitions
                {
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Auto),
                },
                Children =
                {
                    new Border
                    {
                        Width = 100,
                        Height = 25,
                        [Grid.ColumnSpanProperty] = 3,
                    },
                    new Border
                    {
                        Width = 150,
                        Height = 25,
                        [Grid.RowProperty] = 1,
                    },
                    new Border
                    {
                        Width = 50,
                        Height = 25,
                        [Grid.RowProperty] = 1,
                        [Grid.ColumnProperty] = 2,
                    }
                },
            };

            target.Measure(Size.Infinity);

            // Issue #25 only appears after a second measure
            target.InvalidateMeasure();
            target.Measure(Size.Infinity);

            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(204, 50), target.Bounds.Size);
            Assert.Equal(150d, target.ColumnDefinitions[0].ActualWidth);
            Assert.Equal(4d, target.ColumnDefinitions[1].ActualWidth);
            Assert.Equal(50d, target.ColumnDefinitions[2].ActualWidth);
            Assert.Equal(new Rect(52, 0, 100, 25), target.Children[0].Bounds);
            Assert.Equal(new Rect(0, 25, 150, 25), target.Children[1].Bounds);
            Assert.Equal(new Rect(154, 25, 50, 25), target.Children[2].Bounds);
        }

        [Fact]
        public void Layout_EmptyColumnRow_LayoutLikeANormalPanel()
        {
            // Arrange & Action
            var grid = GridMock.New(arrange: new Size(600, 200));

            // Assert
            GridAssert.ChildrenWidth(grid, 600);
            GridAssert.ChildrenHeight(grid, 200);
        }

        [Fact]
        public void Layout_PixelRowColumn_BoundsCorrect()
        {
            // Arrange & Action
            var rowGrid = GridMock.New(new RowDefinitions("100,200,300"));
            var columnGrid = GridMock.New(new ColumnDefinitions("50,100,150"));

            // Assert
            GridAssert.ChildrenHeight(rowGrid, 100, 200, 300);
            GridAssert.ChildrenWidth(columnGrid, 50, 100, 150);
        }

        [Fact]
        public void Layout_StarRowColumn_BoundsCorrect()
        {
            // Arrange & Action
            var rowGrid = GridMock.New(new RowDefinitions("1*,2*,3*"), 600);
            var columnGrid = GridMock.New(new ColumnDefinitions("*,*,2*"), 600);

            // Assert
            GridAssert.ChildrenHeight(rowGrid, 100, 200, 300);
            GridAssert.ChildrenWidth(columnGrid, 150, 150, 300);
        }

        [Fact]
        public void Layout_MixPixelStarRowColumn_BoundsCorrect()
        {
            // Arrange & Action
            var rowGrid = GridMock.New(new RowDefinitions("1*,2*,150"), 600);
            var columnGrid = GridMock.New(new ColumnDefinitions("1*,2*,150"), 600);

            // Assert
            GridAssert.ChildrenHeight(rowGrid, 150, 300, 150);
            GridAssert.ChildrenWidth(columnGrid, 150, 300, 150);
        }

        [Fact]
        public void Layout_StarRowColumnWithMinLength_BoundsCorrect()
        {
            // Arrange & Action
            var rowGrid = GridMock.New(new RowDefinitions
            {
                new RowDefinition(1, GridUnitType.Star) { MinHeight = 200 },
                new RowDefinition(1, GridUnitType.Star),
                new RowDefinition(1, GridUnitType.Star),
            }, 300);
            var columnGrid = GridMock.New(new ColumnDefinitions
            {
                new ColumnDefinition(1, GridUnitType.Star) { MinWidth = 200 },
                new ColumnDefinition(1, GridUnitType.Star),
                new ColumnDefinition(1, GridUnitType.Star),
            }, 300);

            // Assert
            GridAssert.ChildrenHeight(rowGrid, 200, 50, 50);
            GridAssert.ChildrenWidth(columnGrid, 200, 50, 50);
        }

        [Fact]
        public void Layout_StarRowColumnWithMaxLength_BoundsCorrect()
        {
            // Arrange & Action
            var rowGrid = GridMock.New(new RowDefinitions
            {
                new RowDefinition(1, GridUnitType.Star) { MaxHeight = 200 },
                new RowDefinition(1, GridUnitType.Star),
                new RowDefinition(1, GridUnitType.Star),
            }, 800);
            var columnGrid = GridMock.New(new ColumnDefinitions
            {
                new ColumnDefinition(1, GridUnitType.Star) { MaxWidth = 200 },
                new ColumnDefinition(1, GridUnitType.Star),
                new ColumnDefinition(1, GridUnitType.Star),
            }, 800);

            // Assert
            GridAssert.ChildrenHeight(rowGrid, 200, 300, 300);
            GridAssert.ChildrenWidth(columnGrid, 200, 300, 300);
        }

        [Fact]
        public void Layout_StarColumn_Should_Pass_Calculated_Width_On_Measure_And_Arrange()
        {
            Square child;

            var target = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,*"),
                Children =
                {
                    (child = new Square
                    {
                        [Grid.ColumnProperty] = 0,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    })
                }
            };

            target.Measure(new Size(200, 200));
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(target.ColumnDefinitions[0].ActualWidth, 100);
            Assert.Equal(target.ColumnDefinitions[1].ActualWidth, 100);

            Assert.Equal(new Size(100, 100), child.DesiredSize);
            Assert.Equal(new Rect(0, 50, 100, 100), child.Bounds);
        }

        [Fact]
        public void Layout_LeftAlingment_And_StarColumn_Shouldnt_Stretch()
        {
            Square child;

            var target = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,100"),
                HorizontalAlignment = HorizontalAlignment.Left,
                Children =
                {
                    (child = new Square
                    {
                        [Grid.ColumnProperty] = 0,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    })
                }
            };

            target.Measure(new Size(300, 100));
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(target.ColumnDefinitions[0].ActualWidth, 100);
            Assert.Equal(target.ColumnDefinitions[1].ActualWidth, 100);

            Assert.Equal(new Size(100, 100), child.DesiredSize);
            Assert.Equal(new Rect(0, 0, 100, 100), child.Bounds);

            Assert.Equal(200, target.DesiredSize.Width);
            Assert.Equal(200, target.Bounds.Width);
        }

        [Fact]
        public void Changing_Child_Column_Invalidates_Measure()
        {
            Border child;
            var target = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,*"),
                Children =
                {
                    (child = new Border
                    {
                        [Grid.ColumnProperty] = 0,
                    }),
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));
            Assert.True(target.IsMeasureValid);

            Grid.SetColumn(child, 1);

            Assert.False(target.IsMeasureValid);
        }

        private class Square : Control
        {
            protected override Size MeasureOverride(Size availableSize)
            {
                var min = Math.Min(availableSize.Width, availableSize.Height);
                return new Size(min, min);
            }

            protected override Size ArrangeOverride(Size finalSize)
            {
                var min = Math.Min(finalSize.Width, finalSize.Height);
                return base.ArrangeOverride(new Size(min, min));
            }
        }
    }
}
