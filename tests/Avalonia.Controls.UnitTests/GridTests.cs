// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls;
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
    }
}
