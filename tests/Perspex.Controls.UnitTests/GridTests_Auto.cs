// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license double.PositiveInfinityormation.

using System;
using System.Collections.Generic;
using System.Linq;
using Perspex.Controls.Shapes;
using Perspex.Layout;
using Perspex.Media;
using Xunit;

namespace Perspex.Controls.UnitTests
{
    public class GridTests_Auto : GridTests
    {
        [Fact]
        public void MeasureStarRowsWithChild()
        {
            // Check what happens if there is no explicit ColumnDefinition added
            Grid grid = new Grid();
            grid.RowDefinitions = new RowDefinitions("1*");
            grid.Children.Add(new MyContentControl(50, 50));

            // Initial values
            Assert.Equal(new Size(0, 0), grid.DesiredSize);
            Assert.Equal(0, grid.RowDefinitions[0].ActualHeight);

            // After measure
            grid.Measure(Size.Infinity);
            Assert.Equal(new Size(50, 50), grid.DesiredSize);
            Assert.Equal(double.PositiveInfinity, grid.RowDefinitions[0].ActualHeight);

            // Measure again
            grid.Measure(new Size(100, 100));
            Assert.Equal(new Size(50, 50), grid.DesiredSize);
            Assert.Equal(100, grid.RowDefinitions[0].ActualHeight);
        }

        [Fact]
        public void MeasureStarRowsWithChild_ExplicitSize()
        {
            // Check what happens if there is no explicit ColumnDefinition added
            Grid grid = new Grid { Width = 75, Height = 75 };
            grid.RowDefinitions = new RowDefinitions("1*");
            grid.Children.Add(new MyContentControl(50, 50));

            // Initial values
            Assert.Equal(new Size(0, 0), grid.DesiredSize);
            Assert.Equal(0, grid.RowDefinitions[0].ActualHeight);

            // After measure
            grid.Measure(Size.Infinity);
            Assert.Equal(new Size(75, 75), grid.DesiredSize);
            Assert.Equal(75, grid.RowDefinitions[0].ActualHeight);

            // Measure again
            grid.Measure(new Size(100, 100));
            Assert.Equal(new Size(75, 75), grid.DesiredSize);
            Assert.Equal(75, grid.RowDefinitions[0].ActualHeight);
        }

        [Fact]
        public void MeasureStarRowsWithChild_NoSpan()
        {
            // Check what happens if there is no explicit ColumnDefinition added
            Grid grid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
            };
            grid.RowDefinitions = new RowDefinitions("1*");
            grid.Children.Add(new MyContentControl(50, 50));

            // Initial values
            Assert.Equal(new Size(0, 0), grid.DesiredSize);
            Assert.Equal(0, grid.RowDefinitions[0].ActualHeight);

            // After measure
            grid.Measure(Size.Infinity);
            Assert.Equal(new Size(50, 50), grid.DesiredSize);
            Assert.Equal(double.PositiveInfinity, grid.RowDefinitions[0].ActualHeight);

            // Measure again
            grid.Measure(new Size(100, 100));
            Assert.Equal(new Size(50, 50), grid.DesiredSize);
            Assert.Equal(100, grid.RowDefinitions[0].ActualHeight);
        }

        [Fact]
        public void MeasureStarRowsWithChild_NoSpan_ExplicitSize()
        {
            // Check what happens if there is no explicit ColumnDefinition added
            Grid grid = new Grid
            {
                Width = 75,
                Height = 75,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
            };
            grid.RowDefinitions = new RowDefinitions("1*");
            grid.Children.Add(new MyContentControl(50, 50));

            // Initial values
            Assert.Equal(new Size(0, 0), grid.DesiredSize);
            Assert.Equal(0, grid.RowDefinitions[0].ActualHeight);

            // After measure
            grid.Measure(Size.Infinity);
            Assert.Equal(new Size(75, 75), grid.DesiredSize);
            Assert.Equal(75, grid.RowDefinitions[0].ActualHeight);

            // Measure again
            grid.Measure(new Size(100, 100));
            Assert.Equal(new Size(75, 75), grid.DesiredSize);
            Assert.Equal(75, grid.RowDefinitions[0].ActualHeight);
        }

        [Fact]
        public void MeasureStarRowsWithChild2()
        {
            // Check what happens when there are two explicit rows and no explicit column
            Grid grid = new Grid();
            grid.RowDefinitions = new RowDefinitions("1*,1*");
            grid.Children.Add(new MyContentControl(50, 50));

            // Initial values
            Assert.Equal(new Size(0, 0), grid.DesiredSize);
            CheckRowHeights(grid, "#2", 0, 0);

            // After measure
            grid.Measure(Size.Infinity);
            Assert.Equal(new Size(50, 50), grid.DesiredSize);
            CheckRowHeights(grid, "#4", double.PositiveInfinity, double.PositiveInfinity);

            // Measure again
            grid.Measure(new Size(100, 100));
            Assert.Equal(new Size(50, 50), grid.DesiredSize);
            CheckRowHeights(grid, "#6", 50, 50);
        }

        [Fact]
        public void MeasureStarRowsWithChild2_ExplicitSize()
        {
            // Check what happens when there are two explicit rows and no explicit column
            Grid grid = new Grid { Width = 75, Height = 75 };
            grid.RowDefinitions = new RowDefinitions("1*,1*");
            grid.Children.Add(new MyContentControl(50, 50));

            // Initial values
            Assert.Equal(new Size(0, 0), grid.DesiredSize);
            CheckRowHeights(grid, "#2", 0, 0);

            // After measure
            grid.Measure(Size.Infinity);
            Assert.Equal(new Size(75, 75), grid.DesiredSize);
            CheckRowHeights(grid, "#4", 37.5, 37.5);

            // Measure again
            grid.Measure(new Size(100, 100));
            Assert.Equal(new Size(75, 75), grid.DesiredSize);
            CheckRowHeights(grid, "#6", 37.5, 37.5);
        }

        [Fact]
        public void MeasureStarRowsWithChild2_NoSpan()
        {
            // Check what happens when there are two explicit rows and no explicit column
            Grid grid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
            };
            grid.RowDefinitions = new RowDefinitions("1*,1*");
            grid.Children.Add(new MyContentControl(50, 50));

            // Initial values
            Assert.Equal(new Size(0, 0), grid.DesiredSize);
            CheckRowHeights(grid, "#2", 0, 0);

            // After measure
            grid.Measure(Size.Infinity);
            Assert.Equal(new Size(50, 50), grid.DesiredSize);
            CheckRowHeights(grid, "#4", double.PositiveInfinity, double.PositiveInfinity);

            // Measure again
            grid.Measure(new Size(100, 100));
            Assert.Equal(new Size(50, 50), grid.DesiredSize);
            CheckRowHeights(grid, "#6", 50, 50);
        }

        [Fact]
        public void MeasureStarRowsWithChild2_NoSpan_ExplicitSize()
        {
            // Check what happens when there are two explicit rows and no explicit column
            Grid grid = new Grid
            {
                Width = 75,
                Height = 75,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
            };
            grid.RowDefinitions = new RowDefinitions("1*,1*");
            grid.Children.Add(new MyContentControl(50, 50));

            // Initial values
            Assert.Equal(new Size(0, 0), grid.DesiredSize);
            CheckRowHeights(grid, "#2", 0, 0);

            // After measure
            grid.Measure(Size.Infinity);
            Assert.Equal(new Size(75, 75), grid.DesiredSize);
            CheckRowHeights(grid, "#4", 37.5, 37.5);

            // Measure again
            grid.Measure(new Size(100, 100));
            Assert.Equal(new Size(75, 75), grid.DesiredSize);
            CheckRowHeights(grid, "#6", 37.5, 37.5);
        }


        [Fact]
        public void StarRowsWithChild()
        {
            // Check what happens if there is no explicit ColumnDefinition added
            Grid grid = new Grid();
            grid.RowDefinitions = new RowDefinitions("1*");
            grid.Children.Add(new MyContentControl(50, 50));

            // Initial values
            Assert.Equal(new Size(0, 0), grid.DesiredSize);
            Assert.Equal(0, grid.RowDefinitions[0].ActualHeight);

            // After measure
            grid.Measure(Size.Infinity);
            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
            Assert.Equal(new Size(50, 50), grid.DesiredSize);
            Assert.Equal(50, grid.RowDefinitions[0].ActualHeight);

            // Measure again
            grid.Measure(new Size(100, 100));
            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
            Assert.Equal(new Size(50, 50), grid.DesiredSize);
            Assert.Equal(50, grid.RowDefinitions[0].ActualHeight);
        }

        [Fact]
        public void StarRowsWithChild_ExplicitSize()
        {
            // Check what happens if there is no explicit ColumnDefinition added
            Grid grid = new Grid { Width = 75, Height = 75 };
            grid.RowDefinitions = new RowDefinitions("1*");
            grid.Children.Add(new MyContentControl(50, 50));

            // Initial values
            Assert.Equal(new Size(0, 0), grid.DesiredSize);
            Assert.Equal(0, grid.RowDefinitions[0].ActualHeight);

            // After measure
            grid.Measure(Size.Infinity);
            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
            Assert.Equal(new Size(75, 75), grid.DesiredSize);
            Assert.Equal(75, grid.RowDefinitions[0].ActualHeight);

            // Measure again
            grid.Measure(new Size(100, 100));
            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
            Assert.Equal(new Size(75, 75), grid.DesiredSize);
            Assert.Equal(75, grid.RowDefinitions[0].ActualHeight);
        }

        [Fact]
        public void StarRowsWithChild_NoSpan()
        {
            // Check what happens if there is no explicit ColumnDefinition added
            Grid grid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
            };
            grid.RowDefinitions = new RowDefinitions("1*");
            grid.Children.Add(new MyContentControl(50, 50));

            // Initial values
            Assert.Equal(new Size(0, 0), grid.DesiredSize);
            Assert.Equal(0, grid.RowDefinitions[0].ActualHeight);

            // After measure
            grid.Measure(Size.Infinity);
            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
            Assert.Equal(new Size(50, 50), grid.DesiredSize);
            Assert.Equal(50, grid.RowDefinitions[0].ActualHeight);

            // Measure again
            grid.Measure(new Size(100, 100));
            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
            Assert.Equal(new Size(50, 50), grid.DesiredSize);
            Assert.Equal(50, grid.RowDefinitions[0].ActualHeight);
        }

        [Fact]
        public void StarRowsWithChild_NoSpan_ExplicitSize()
        {
            // Check what happens if there is no explicit ColumnDefinition added
            Grid grid = new Grid
            {
                Width = 75,
                Height = 75,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
            };
            grid.RowDefinitions = new RowDefinitions("1*");
            grid.Children.Add(new MyContentControl(50, 50));

            // Initial values
            Assert.Equal(new Size(0, 0), grid.DesiredSize);
            Assert.Equal(0, grid.RowDefinitions[0].ActualHeight);

            // After measure
            grid.Measure(Size.Infinity);
            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
            Assert.Equal(new Size(75, 75), grid.DesiredSize);
            Assert.Equal(75, grid.RowDefinitions[0].ActualHeight);

            // Measure again
            grid.Measure(new Size(100, 100));
            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
            Assert.Equal(new Size(75, 75), grid.DesiredSize);
            Assert.Equal(75, grid.RowDefinitions[0].ActualHeight);
        }

        [Fact]
        public void StarRowsWithChild2()
        {
            // Check what happens when there are two explicit rows and no explicit column
            Grid grid = new Grid();
            grid.RowDefinitions = new RowDefinitions("1*,1*");
            grid.Children.Add(new MyContentControl(50, 50));

            // Initial values
            Assert.Equal(new Size(0, 0), grid.DesiredSize);
            CheckRowHeights(grid, "#2", 0, 0);

            // After measure
            grid.Measure(Size.Infinity);
            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
            Assert.Equal(new Size(50, 50), grid.DesiredSize);
            CheckRowHeights(grid, "#4", 50, 0);

            // Measure again
            grid.Measure(new Size(100, 100));
            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
            Assert.Equal(new Size(50, 50), grid.DesiredSize);
            CheckRowHeights(grid, "#6", 50, 0);
        }

        [Fact]
        public void StarRowsWithChild2_ExplicitSize()
        {
            // Check what happens when there are two explicit rows and no explicit column
            Grid grid = new Grid { Width = 75, Height = 75 };
            grid.RowDefinitions = new RowDefinitions("1*,1*");
            grid.Children.Add(new MyContentControl(50, 50));

            // Initial values
            Assert.Equal(new Size(0, 0), grid.DesiredSize);
            CheckRowHeights(grid, "#2", 0, 0);

            // After measure
            grid.Measure(Size.Infinity);
            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
            Assert.Equal(new Size(75, 75), grid.DesiredSize);
            CheckRowHeights(grid, "#4", 37.5, 37.5);

            // Measure again
            grid.Measure(new Size(100, 100));
            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
            Assert.Equal(new Size(75, 75), grid.DesiredSize);
            CheckRowHeights(grid, "#6", 37.5, 37.5);
        }

        [Fact]
        public void StarRowsWithChild2_NoSpan()
        {
            // Check what happens when there are two explicit rows and no explicit column
            Grid grid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
            };
            grid.RowDefinitions = new RowDefinitions("1*,1*");
            grid.Children.Add(new MyContentControl(50, 50));

            // Initial values
            Assert.Equal(new Size(0, 0), grid.DesiredSize);
            CheckRowHeights(grid, "#2", 0, 0);

            // After measure
            grid.Measure(Size.Infinity);
            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
            Assert.Equal(new Size(50, 50), grid.DesiredSize);
            CheckRowHeights(grid, "#4", 50, 0);

            // Measure again
            grid.Measure(new Size(100, 100));
            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
            Assert.Equal(new Size(50, 50), grid.DesiredSize);
            CheckRowHeights(grid, "#6", 50, 0);
        }

        [Fact]
        public void StarRowsWithChild2_NoSpan_ExplicitSize()
        {
            // Check what happens when there are two explicit rows and no explicit column
            Grid grid = new Grid
            {
                Width = 75,
                Height = 75,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
            };
            grid.RowDefinitions = new RowDefinitions("1*,1*");
            grid.Children.Add(new MyContentControl(50, 50));

            // Initial values
            Assert.Equal(new Size(0, 0), grid.DesiredSize);
            CheckRowHeights(grid, "#2", 0, 0);

            // After measure
            grid.Measure(Size.Infinity);
            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
            Assert.Equal(new Size(75, 75), grid.DesiredSize);
            CheckRowHeights(grid, "#4", 37.5, 37.5);

            // Measure again
            grid.Measure(new Size(100, 100));
            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
            Assert.Equal(new Size(75, 75), grid.DesiredSize);
            CheckRowHeights(grid, "#6", 37.5, 37.5);
        }

        [Fact]
        public void ExpandInArrange_GridParent()
        {
            // Measure with double.PositiveInfinityinity and check results.
            MyGrid grid = new MyGrid();
            grid.RowDefinitions = new RowDefinitions("1*");
            grid.ColumnDefinitions = new ColumnDefinitions("1*");
            grid.AddChild(DecoratorWithChild(), 0, 0, 1, 1);

            grid.Reset();
            grid.Measure(Size.Infinity);
            grid.Arrange(new Rect(grid.DesiredSize));
            grid.CheckMeasureArgs("#1", Size.Infinity);
            grid.CheckMeasureResult("#2", new Size(50, 50));
            Assert.Equal(new Size(50, 50), grid.DesiredSize);

            // When we pass in the desired size as the arrange arg,
            // the rows/cols use that as their height/width
            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
            grid.CheckArrangeArgs("#4", grid.DesiredSize);
            grid.CheckArrangeResult("#5", grid.DesiredSize);
            CheckRowHeights(grid, "#6", grid.DesiredSize.Height);
            grid.CheckColWidths("#7", grid.DesiredSize.Width);

            // If we pass in twice the desired size, the rows/cols consume that too
            grid.Reset();
            grid.Measure(Size.Infinity);
            grid.Arrange(new Rect(0, 0, 100, 100));
            grid.CheckArrangeArgs("#9", new Size(100, 100));
            grid.CheckArrangeResult("#10", new Size(100, 100));
            CheckRowHeights(grid, "#11", 100);
            grid.CheckColWidths("#12", 100);

            // If we measure with a finite size, the rows/cols still expand
            // to consume the available space
            grid.Reset();
            grid.Measure(new Size(1000, 1000));
            grid.CheckMeasureArgs("#13", new Size(1000, 1000));
            grid.CheckMeasureResult("#14", new Size(50, 50));
            Assert.Equal(new Size(50, 50), grid.DesiredSize);

            // When we pass in the desired size as the arrange arg,
            // the rows/cols use that as their height/width
            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
            grid.CheckArrangeArgs("#16", grid.DesiredSize);
            grid.CheckArrangeResult("#17", grid.DesiredSize);
            CheckRowHeights(grid, "#18", grid.DesiredSize.Height);
            grid.CheckColWidths("#19", grid.DesiredSize.Width);

            // If we pass in twice the desired size, the rows/cols consume that too
            grid.Reset();
            grid.Arrange(new Rect(0, 0, 100, 100));
            grid.CheckMeasureArgs("#20"); // No remeasures
            grid.CheckArrangeArgs("#21", new Size(100, 100));
            grid.CheckArrangeResult("#22", new Size(100, 100));
            CheckRowHeights(grid, "#23", 100);
            grid.CheckColWidths("#24", 100);
        }

        [Fact]
        public void ExpandStars_UnfixedSize()
        {
            // If a width/height is *not* set on the grid, it doesn't expand stars.
            var canvas = new Canvas { Width = 120, Height = 120 };
            PanelPoker poker = new PanelPoker();
            MyGrid grid = new MyGrid { Name = "TEDDY" };
            grid.RowDefinitions = new RowDefinitions("*,*,*");
            grid.ColumnDefinitions = new ColumnDefinitions("*,*,*");

            canvas.Children.Add(poker);
            poker.Grid = grid;
            grid.AddChild(new MyContentControl(100, 100), 1, 1, 1, 1);

            canvas.Measure(Size.Infinity);
            canvas.Arrange(new Rect(canvas.DesiredSize));

            Assert.Equal(Size.Infinity, poker.MeasureArgs[0]);
            Assert.Equal(new Size(100, 100), poker.MeasureResults[0]);
            Assert.Equal(new Size(100, 100), poker.ArrangeArgs[0]);
            Assert.Equal(new Size(100, 100), poker.ArrangeResults[0]);

            CheckRowHeights(grid, "#5", 0, 100, 0);
            grid.CheckColWidths("#6", 0, 100, 0);

            grid.CheckMeasureArgs("#7", Size.Infinity);
            grid.CheckMeasureResult("#8", new Size(100, 100));

            grid.CheckArrangeArgs("#9", new Size(100, 100));
            grid.CheckArrangeResult("#10", new Size(100, 100));

            // Do not expand if we already consume 100 px
            grid.Reset();
            grid.Arrange(new Rect(0, 0, 100, 100));
            grid.CheckArrangeArgs("#11");

            // If we give extra space, we expand the rows.
            grid.Measure(Size.Infinity);
            grid.Arrange(new Rect(0, 0, 500, 500));

            CheckRowHeights(grid, "#12", 166.5, 166.5, 166.5);
            grid.CheckColWidths("#13", 166.5, 166.5, 166.5);

            grid.CheckArrangeArgs("#14", new Size(167, 167));
            grid.CheckArrangeResult("#15", new Size(167, 167));
        }

        [Fact]
        public void ExpandStars_FixedSize()
        {
            // If a width/height is set on the grid, it expands stars.
            var canvas = new Canvas { Width = 120, Height = 120 };
            PanelPoker poker = new PanelPoker { Width = 120, Height = 120 };
            MyGrid grid = new MyGrid { Name = "Griddy" };
            grid.RowDefinitions = new RowDefinitions("*,*,*");
            grid.ColumnDefinitions = new ColumnDefinitions("*,*,*");

            canvas.Children.Add(poker);
            poker.Grid = grid;
            grid.AddChild(new MyContentControl(100, 100), 1, 1, 1, 1);

            canvas.Measure(Size.Infinity);
            canvas.Arrange(new Rect(canvas.DesiredSize));

            Assert.Equal(new Size(120, 120), poker.MeasureArgs[0]);
            Assert.Equal(new Size(40, 40), poker.MeasureResults[0]);
            Assert.Equal(new Size(120, 120), poker.ArrangeArgs[0]);
            Assert.Equal(new Size(120, 120), poker.ArrangeResults[0]);

            CheckRowHeights(grid, "#5", 40, 40, 40);
            grid.CheckColWidths("#6", 40, 40, 40);

            grid.CheckMeasureArgs("#7", new Size(40, 40));
            grid.CheckMeasureResult("#8", new Size(40, 40));

            grid.CheckArrangeArgs("#9", new Size(40, 40));
            grid.CheckArrangeResult("#10", new Size(40, 40));
        }

        [Fact]
        public void ExpandStars_NoRowsOrCols()
        {
            // If the rows/cols are autogenerated, we still expand them
            Grid grid = new Grid();
            grid.Children.Add(new Rectangle { Width = 50, Height = 50 });

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(0, 0, 200, 200));

            Assert.Equal(200, grid.Bounds.Width);
            Assert.Equal(200, grid.Bounds.Height);
        }

        [Fact]
        public void ExpandStars_NoRowsOrCols2()
        {
            // We don't expand autogenerated rows/cols if we don't have Alignment.Stretch
            Grid grid = new Grid { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            grid.Children.Add(new Rectangle { Width = 50, Height = 50 });

            grid.Measure(new Size(200, 200));
            grid.Arrange(new Rect(0, 0, 200, 200));

            Assert.Equal(50, grid.Bounds.Width);
            Assert.Equal(50, grid.Bounds.Height);
        }

        [Fact]
        public void ExpandInArrange2()
        {
            // Measure with a finite value and check results.
            MyGrid grid = new MyGrid();
            grid.RowDefinitions = new RowDefinitions("*");
            grid.ColumnDefinitions = new ColumnDefinitions("*");
            grid.AddChild(DecoratorWithChild(), 0, 0, 1, 1);

            grid.Measure(new Size(75, 75));
            grid.CheckMeasureArgs("#1", new Size(75, 75));
            grid.CheckMeasureResult("#2", new Size(50, 50));
            Assert.Equal(new Size(50, 50), grid.DesiredSize);

            // Check that everything is as expected when we pass in DesiredSize as the argument to Arrange
            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));

            grid.CheckArrangeArgs("#4", grid.DesiredSize);
            grid.CheckArrangeResult("#5", grid.DesiredSize);
            CheckRowHeights(grid, "#6", grid.DesiredSize.Height);
            grid.CheckColWidths("#7", grid.DesiredSize.Width);

            grid.Reset();
            grid.Measure(Size.Infinity);
            grid.Arrange(new Rect(0, 0, 100, 100));
            grid.CheckArrangeArgs("#9", new Size(100, 100));
            grid.CheckArrangeResult("#10", new Size(100, 100));
            CheckRowHeights(grid, "#11", 100);
            grid.CheckColWidths("#12", 100);
        }

        [Fact]
        public void StarRows3b2()
        {
            var canvas = new Canvas { Width = 120, Height = 120 };
            PanelPoker poker = new PanelPoker();
            MyGrid grid = new MyGrid();
            grid.RowDefinitions = new RowDefinitions("*,*,*");
            grid.ColumnDefinitions = new ColumnDefinitions("*,*,*");

            canvas.Children.Add(poker);
            poker.Grid = grid;
            poker.Children.Add(grid);
            grid.AddChild(new MyContentControl(100, 100), 1, 1, 1, 1);
            canvas.Measure(Size.Infinity);
            canvas.Arrange(new Rect(canvas.DesiredSize));

            Assert.Equal(Size.Infinity, poker.MeasureArgs[0]);
            Assert.Equal(new Size(100, 100), poker.MeasureResults[0]);
            Assert.Equal(new Size(100, 100), poker.ArrangeArgs[0]);
            Assert.Equal(new Size(100, 100), poker.ArrangeResults[0]);

            grid.CheckColWidths("#5", 0, 100, 0);
            CheckRowHeights(grid, "#6", 0, 100, 0);

            grid.CheckMeasureArgs("#7", Size.Infinity);
            grid.CheckMeasureResult("#8", new Size(100, 100));

            grid.CheckArrangeArgs("#9", new Size(100, 100));
            grid.CheckArrangeResult("#10", new Size(100, 100));
        }

        [Fact]
        public void StarRows3c()
        {
            var canvas = new Canvas { Width = 120, Height = 120 };
            var poker = new MyContentControl();
            MyGrid grid = new MyGrid();
            grid.RowDefinitions = new RowDefinitions("*,*,*");
            grid.ColumnDefinitions = new ColumnDefinitions("*,*,*");

            canvas.Children.Add(poker);
            poker.Child = grid;
            grid.AddChild(new MyContentControl(100, 100), 1, 1, 1, 1);
            canvas.Measure(Size.Infinity);
            canvas.Arrange(new Rect(canvas.DesiredSize));

            Assert.Equal(Size.Infinity, poker.MeasureOverrideArg);
            Assert.Equal(new Size(100, 100), poker.MeasureOverrideResult);
            Assert.Equal(new Size(100, 100), poker.ArrangeOverrideArg);
            Assert.Equal(new Size(100, 100), poker.ArrangeOverrideResult);

            grid.CheckColWidths("#5", 0, 100, 0);
            CheckRowHeights(grid, "#6", 0, 100, 0);

            grid.CheckMeasureArgs("#7", Size.Infinity);
            grid.CheckMeasureResult("#8", new Size(100, 100));

            grid.CheckArrangeArgs("#9", new Size(100, 100));
            grid.CheckArrangeResult("#10", new Size(100, 100));
        }

        [Fact]
        public void StarRows3d()
        {
            var poker = new MyContentControl { Width = 120, Height = 120 };
            MyGrid grid = new MyGrid { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            grid.RowDefinitions = new RowDefinitions("*,*,*");
            grid.ColumnDefinitions = new ColumnDefinitions("*,*,*");

            poker.Child = grid;
            grid.AddChild(new MyContentControl(100, 100), 1, 1, 1, 1);
            poker.Measure(Size.Infinity);
            poker.Arrange(new Rect(poker.DesiredSize));

            Assert.Equal(new Size(120, 120), poker.MeasureOverrideArg);
            Assert.Equal(new Size(40, 40), poker.MeasureOverrideResult);
            Assert.Equal(new Size(40, 40), grid.DesiredSize);
            Assert.Equal(new Size(120, 120), poker.DesiredSize);
            Assert.Equal(new Size(120, 120), poker.ArrangeOverrideArg);
            Assert.Equal(new Size(120, 120), poker.ArrangeOverrideResult);

            grid.CheckColWidths("#5", 0, 40, 0);
            CheckRowHeights(grid, "#6", 0, 40, 0);

            grid.CheckMeasureArgs("#7", new Size(40, 40));
            grid.CheckMeasureResult("#8", new Size(40, 40));

            grid.CheckArrangeArgs("#9", new Size(40, 40));
            grid.CheckArrangeResult("#10", new Size(40, 40));
        }

        [Fact]
        public void ExpandInArrange()
        {
            // Measure with double.PositiveInfinityinity and check results.
            MyGrid grid = new MyGrid();
            grid.RowDefinitions = new RowDefinitions("*");
            grid.ColumnDefinitions = new ColumnDefinitions("*");
            grid.AddChild(DecoratorWithChild(), 0, 0, 1, 1);

            grid.Measure(Size.Infinity);
            grid.CheckMeasureArgs("#1", Size.Infinity);
            grid.CheckMeasureResult("#2", new Size(50, 50));
            Assert.Equal(new Size(50, 50), grid.DesiredSize);

            // Check that everything is as expected when we pass in DesiredSize as the argument to Arrange
            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
            grid.CheckArrangeArgs("#4", grid.DesiredSize);
            grid.CheckArrangeResult("#5", grid.DesiredSize);
            CheckRowHeights(grid, "#6", grid.DesiredSize.Height);
            grid.CheckColWidths("#7", grid.DesiredSize.Width);

            grid.Reset();
            grid.Measure(Size.Infinity);
            grid.Arrange(new Rect(0, 0, 100, 100));
            grid.CheckArrangeArgs("#9", new Size(100, 100));
            grid.CheckArrangeResult("#10", new Size(100, 100));
            CheckRowHeights(grid, "#11", 100);
            grid.CheckColWidths("#12", 100);
        }

        [Fact]
        public void AutoStarInfiniteChildren()
        {
            Grid holder = new Grid { Width = 500, Height = 500 };
            MyGrid g = new MyGrid { Name = "Ted!" };
            g.RowDefinitions = new RowDefinitions("*,Auto");
            g.ColumnDefinitions = new ColumnDefinitions("*,Auto");

            g.AddChild(CreateInfiniteChild(), 0, 0, 1, 1);
            g.AddChild(CreateInfiniteChild(), 0, 1, 1, 1);
            g.AddChild(CreateInfiniteChild(), 1, 0, 1, 1);
            g.AddChild(CreateInfiniteChild(), 1, 1, 1, 1);

            // FIXME: I think this fails because the first time the ScrollViewer measures it calculates
            // the visibility of the Horizontal/Vertical scroll bar incorrectly. It's desired size on the
            // first measure is (327, 327) whereas it should be (327, 310). A few measure cycles later and
            // it will be correct, but chews up much more CPU than it should.
            holder.Children.Add(g);

            holder.Measure(Size.Infinity);
            holder.Arrange(new Rect(holder.DesiredSize));

            g.CheckMeasureOrder("#1", 3, 1, 2, 1, 0);
            g.CheckMeasureArgs("#2", Size.Infinity, Size.Infinity, new Size(173, double.PositiveInfinity), new Size(double.PositiveInfinity, 190), new Size(173, 190));
            g.CheckMeasureResult("#3", new Size(173, 190), new Size(327, 190), new Size(173, 310), new Size(327, 310), new Size(173, 310));
            g.CheckRowHeights("#4", 190, 310);
            g.CheckColWidths("#5", 173, 327);
            Assert.Equal(new Size(500, 500), g.DesiredSize);
        }

        [Fact]
        public void ChildInvalidatesGrid()
        {
            var child = new MyContentControl(50, 50);
            Grid grid = new Grid();
            grid.Children.Add(child);
            grid.Measure(new Size(100, 100));
            Assert.Equal(new Size(50, 50), grid.DesiredSize);

            ((Control)child.Content).Height = 60;
            ((Control)child.Content).Width = 10;

            grid.Measure(new Size(100, 100));
            Assert.Equal(new Size(10, 60), grid.DesiredSize);
        }

        [Fact]
        public void ChildInvalidatesGrid2()
        {
            var child = new MyContentControl(50, 50);
            MyGrid grid = new MyGrid();
            grid.Children.Add(child);

            grid.Measure(new Size(100, 100));
            Assert.Equal(1, grid.MeasuredElements.Count);

            child.InvalidateMeasure();
            grid.Measure(new Size(100, 100));
            Assert.Equal(2, grid.MeasuredElements.Count);
        }

        ////[Fact]
        ////public void ExpandStarsInBorder()
        ////{
        ////    MyGrid grid = CreateGridWithChildren();

        ////    var parent = new Border();
        ////    parent.Child = grid;

        ////    TestPanel.Width = 75;
        ////    TestPanel.Height = 75;

        ////    CreateAsyncTest(parent,
        ////        () =>
        ////        {
        ////            CheckRowHeights(grid, "#1", 12, 25, 38);

        ////            grid.HorizontalAlignment = HorizontalAlignment.Left;
        ////            grid.VerticalAlignment = VerticalAlignment.Center;
        ////            parent.InvalidateSubtree();
        ////        }, () =>
        ////        {
        ////            CheckRowHeights(grid, "#2", 12, 15, 15);
        ////            grid.Width = 50;
        ////            grid.Height = 50;
        ////            parent.InvalidateSubtree();
        ////        }, () =>
        ////        {
        ////            CheckRowHeights(grid, "#3", 8, 17, 25);

        ////            grid.ClearValue(Grid.HorizontalAlignmentProperty);
        ////            grid.ClearValue(Grid.VerticalAlignmentProperty);
        ////            parent.InvalidateSubtree();
        ////        }, () =>
        ////        {
        ////            CheckRowHeights(grid, "#4", 8, 17, 25);
        ////        }
        ////    );
        ////}

        ////        [Fact]
        ////        [Asynchronous]
        ////        public void ExpandStarsInCanvas()
        ////        {
        ////            Grid grid = CreateGridWithChildren();

        ////            var parent = new Canvas();
        ////            parent.Children.Add(grid);

        ////            TestPanel.Width = 75;
        ////            TestPanel.Height = 75;

        ////            CreateAsyncTest(parent,
        ////                () => {
        ////                    CheckRowHeights(grid, "#1", 15, 15, 15);

        ////                    grid.HorizontalAlignment = HorizontalAlignment.Left;
        ////                    grid.VerticalAlignment = VerticalAlignment.Center;
        ////                    parent.InvalidateSubtree();
        ////                }, () => {
        ////                    CheckRowHeights(grid, "#2", 15, 15, 15);

        ////                    grid.Width = 50;
        ////                    grid.Height = 50;
        ////                    parent.InvalidateSubtree();
        ////                }, () => {
        ////                    CheckRowHeights(grid, "#3", 8, 17, 25);

        ////                    grid.ClearValue(Grid.HorizontalAlignmentProperty);
        ////                    grid.ClearValue(Grid.VerticalAlignmentProperty);
        ////                    parent.InvalidateSubtree();
        ////                }, () => {
        ////                    CheckRowHeights(grid, "#4", 8, 17, 25);
        ////                }
        ////            );
        ////        }

        ////        [Fact]
        ////        [Asynchronous]
        ////        [MoonlightBug]
        ////        public void ExpandStarsInGrid()
        ////        {
        ////            MyGrid grid = CreateGridWithChildren();

        ////            var parent = new Grid();
        ////            parent.AddRows(new GridLength(75));
        ////            parent.AddColumns(new GridLength(75));
        ////            parent.AddChild(grid, 0, 0, 1, 1);

        ////            TestPanel.Width = 75;
        ////            TestPanel.Height = 75;

        ////            CreateAsyncTest(parent,
        ////                () => {
        ////                    grid.CheckMeasureArgs("#1a", new Size(12, 12), new Size(25, 12), new Size(38, 12),
        ////                                                  new Size(12, 25), new Size(25, 25), new Size(38, 25),
        ////                                                  new Size(12, 38), new Size(25, 38), new Size(38, 38));
        ////                    CheckRowHeights(grid, "#1", 12, 25, 38);

        ////                    grid.HorizontalAlignment = HorizontalAlignment.Left;
        ////                    grid.VerticalAlignment = VerticalAlignment.Center;
        ////                    parent.InvalidateSubtree();
        ////                    grid.Reset();
        ////                }, () => {
        ////                    grid.CheckMeasureArgs("#2a", new Size(12, 12), new Size(25, 12), new Size(38, 12),
        ////                                                  new Size(12, 25), new Size(25, 25), new Size(38, 25),
        ////                                                  new Size(12, 38), new Size(25, 38), new Size(38, 38));
        ////                    CheckRowHeights(grid, "#2", 12, 15, 15);

        ////                    grid.Width = 50;
        ////                    grid.Height = 50;
        ////                    parent.InvalidateSubtree();
        ////                    grid.Reset();
        ////                }, () => {
        ////                    grid.CheckMeasureArgs("#3a", new Size(8, 8), new Size(17, 8), new Size(25, 8),
        ////                                                  new Size(8, 17), new Size(17, 17), new Size(25, 17),
        ////                                                  new Size(8, 25), new Size(17, 25), new Size(25, 25));
        ////                    CheckRowHeights(grid, "#3", 8, 17, 25);

        ////                    grid.ClearValue(Grid.HorizontalAlignmentProperty);
        ////                    grid.ClearValue(Grid.VerticalAlignmentProperty);
        ////                    parent.InvalidateSubtree();
        ////                    grid.Reset();
        ////                }, () => {
        ////                    grid.CheckMeasureArgs("#4a", new Size(8, 8), new Size(17, 8), new Size(25, 8),
        ////                                                  new Size(8, 17), new Size(17, 17), new Size(25, 17),
        ////                                                  new Size(8, 25), new Size(17, 25), new Size(25, 25));
        ////                    CheckRowHeights(grid, "#4", 8, 17, 25);
        ////                }
        ////            );
        ////        }

        ////        [Fact]
        ////        [Asynchronous]
        ////        [MoonlightBug]
        ////        public void ExpandStarsInStackPanel()
        ////        {
        ////            MyGrid grid = CreateGridWithChildren();
        ////            var parent = new StackPanel();
        ////            parent.Children.Add(grid);

        ////            TestPanel.Width = 75;
        ////            TestPanel.Height = 75;

        ////            CreateAsyncTest(parent,
        ////                () => {
        ////                    CheckRowHeights(grid, "#1", 15, 15, 15);
        ////                    grid.CheckColWidths("#2", 12, 25, 38);

        ////                    grid.HorizontalAlignment = HorizontalAlignment.Left;
        ////                    grid.VerticalAlignment = VerticalAlignment.Center;
        ////                    parent.InvalidateSubtree();
        ////                }, () => {
        ////                    CheckRowHeights(grid, "#3", 15, 15, 15);
        ////                    grid.CheckColWidths("#4", 12, 15, 15);

        ////                    grid.Width = 50;
        ////                    grid.Height = 50;
        ////                    parent.InvalidateSubtree();
        ////                }, () => {
        ////                    CheckRowHeights(grid, "#5", 8, 17, 25);
        ////                    grid.CheckColWidths("#6", 8, 17, 25);

        ////                    grid.ClearValue(Grid.HorizontalAlignmentProperty);
        ////                    grid.ClearValue(Grid.VerticalAlignmentProperty);
        ////                    parent.InvalidateSubtree();
        ////                }, () => {
        ////                    CheckRowHeights(grid, "#7", 8, 17, 25);
        ////                    grid.CheckColWidths("#8", 8, 17, 25);
        ////                }
        ////            );
        ////        }

        ////        [Fact]
        ////        [Asynchronous]
        ////        public void ExpandStarsInStackPanel2()
        ////        {
        ////            Grid grid = new Grid();
        ////            grid.AddRows(Auto);
        ////            grid.AddColumns(Auto);

        ////            var parent = new StackPanel();

        ////            for (int i = 0; i < 4; i++)
        ////            {
        ////                MyGrid g = new MyGrid { Name = "Grid" + i };
        ////                g.AddRows(Star);
        ////                g.AddColumns(Star);
        ////                g.Children.Add(new MyContentControl
        ////                {
        ////                    Content = new Rectangle
        ////                    {
        ////                        RadiusX = 4,
        ////                        RadiusY = 4,
        ////                        StrokeThickness = 2,
        ////                        Fill = new SolidColorBrush(Colors.Red),
        ////                        Stroke = new SolidColorBrush(Colors.Black)
        ////                    }
        ////                });
        ////                g.Children.Add(new MyContentControl
        ////                {
        ////                    Content = new Rectangle
        ////                    {
        ////                        Fill = new SolidColorBrush(Colors.Blue),
        ////                        HorizontalAlignment = HorizontalAlignment.Center,
        ////                        VerticalAlignment = VerticalAlignment.Center,
        ////                        Height = 17,
        ////                        Width = 20 + i * 20
        ////                    }
        ////                });
        ////                parent.Children.Add(g);
        ////            }
        ////            grid.Children.Add(parent);

        ////            CreateAsyncTest(grid, () => {
        ////                for (int i = 0; i < parent.Children.Count; i++)
        ////                {
        ////                    MyGrid g = (MyGrid)parent.Children[i];
        ////                    Assert.Equal(new Size(20 + i * 20, 17), g.DesiredSize, "#1." + i);
        ////                    Assert.Equal(new Size(80, 17), g.RenderSize, "#2." + i);

        ////                    g.CheckMeasureArgs("#3", Infinity, Infinity);
        ////                    g.CheckMeasureResult("#4", new Size(0, 0), new Size(20 + i * 20, 17));

        ////                    g.CheckRowHeights("#5", 17);
        ////                    g.CheckColWidths("#6", 80);

        ////                    g.CheckArrangeArgs("#7", new Size(80, 17), new Size(80, 17));
        ////                    g.CheckArrangeResult("#8", new Size(80, 17), new Size(80, 17));
        ////                }
        ////            });
        ////        }

        [Fact]
        public void MeasureMaxAndMin()
        {
            MyGrid g = new MyGrid();
            var child = new MyContentControl(50, 50);
            g.RowDefinitions = new RowDefinitions("Auto");
            g.ColumnDefinitions = new ColumnDefinitions("Auto,Auto");
            g.AddChild(child, 0, 0, 1, 1);
            g.Measure(Size.Infinity);
            g.Arrange(new Rect(g.DesiredSize));

            g.CheckMeasureArgs("#1", Size.Infinity);
            g.CheckRowHeights("#2", 50, 0);

            g.Reset();
            g.InvalidateMeasure();
            g.RowDefinitions[0].MaxHeight = 20;
            g.Measure(Size.Infinity);
            g.Arrange(new Rect(g.DesiredSize));

            g.CheckMeasureArgs("#3", Size.Infinity);
            g.CheckRowHeights("#4", 50, 0);
        }

        [Fact]
        public void MeasureMaxAndMin2()
        {
            MyGrid g = new MyGrid();
            var child = new MyContentControl(50, 50);
            g.RowDefinitions = new RowDefinitions("50");
            g.ColumnDefinitions = new ColumnDefinitions("50,50");
            g.AddChild(child, 0, 0, 1, 1);
            g.Measure(Size.Infinity);
            g.Arrange(new Rect(g.DesiredSize));

            g.CheckMeasureArgs("#1", new Size(50, 50));
            g.CheckRowHeights("#2", 50, 50);

            g.Reset();
            g.InvalidateMeasure();
            g.RowDefinitions[0].MaxHeight = 20;
            g.Measure(Size.Infinity);
            g.Arrange(new Rect(g.DesiredSize));

            g.CheckMeasureArgs("#3", new Size(50, 20));
            g.CheckRowHeights("#4", 20, 50);
        }

        [Fact]
        public void MeasureMaxAndMin3()
        {
            MyGrid g = new MyGrid();
            var child = new MyContentControl(50, 50);
            g.RowDefinitions = new RowDefinitions("20,20");
            g.ColumnDefinitions = new ColumnDefinitions("50");
            g.AddChild(child, 0, 0, 2, 2);

            g.RowDefinitions[0].MaxHeight = 5;
            g.RowDefinitions[1].MaxHeight = 30;
            g.Measure(Size.Infinity);
            g.Arrange(new Rect(g.DesiredSize));

            var arg = child.MeasureOverrideArg;
            Assert.Equal(25, arg.Height);
            g.RowDefinitions[0].MaxHeight = 10;

            g.Measure(Size.Infinity);
            arg = child.MeasureOverrideArg;
            Assert.Equal(30, arg.Height);
            g.RowDefinitions[0].MaxHeight = 20;

            g.Measure(Size.Infinity);
            arg = child.MeasureOverrideArg;
            Assert.Equal(40, arg.Height);
        }

        [Fact]
        public void MeasureAutoRows()
        {
            MyGrid grid = new MyGrid();

            grid.RowDefinitions = new RowDefinitions("Auto,Auto,Auto");
            grid.ColumnDefinitions = new ColumnDefinitions("50,50");

            grid.AddChild(new MyContentControl(50, 50), 0, 0, 2, 1);
            grid.AddChild(new MyContentControl(50, 60), 0, 1, 1, 1);

            grid.Measure(new Size(0, 0));
            grid.CheckMeasureArgs("#1", new Size(50, double.PositiveInfinity), new Size(50, double.PositiveInfinity));
            grid.Reset();
            Assert.Equal(new Size(0, 0), grid.DesiredSize);

            grid.Measure(new Size(50, 40));
            grid.CheckMeasureSizes("#3", new Size(50, double.PositiveInfinity), new Size(50, double.PositiveInfinity));
            grid.Reset();
            Assert.Equal(new Size(50, 40), grid.DesiredSize);

            grid.Measure(new Size(500, 400));
            grid.CheckMeasureSizes("#5", new Size(50, double.PositiveInfinity), new Size(50, double.PositiveInfinity));
            grid.Reset();
            Assert.Equal(new Size(100, 60), grid.DesiredSize);
        }

        ////[Fact]
        ////public void MeasureAutoRows2()
        ////{
        ////    MyGrid grid = new MyGrid();

        ////    grid.RowDefinitions = new RowDefinitions("Auto,Auto,Auto");
        ////    grid.ColumnDefinitions = new ColumnDefinitions("50,50");

        ////    MyContentControl c = new MyContentControl(50, 50);
        ////    grid.AddChild(c, 0, 0, 2, 1);
        ////    grid.AddChild(new MyContentControl(50, 60), 0, 1, 1, 1);
        ////    grid.AddChild(new MyContentControl(50, 20), 0, 1, 1, 1);

        ////    grid.Measure(new Size(500, 400));
        ////    grid.CheckMeasureArgs("#1", new Size(50, double.PositiveInfinity), new Size(50, double.PositiveInfinity), new Size(50, double.PositiveInfinity));
        ////    grid.CheckMeasureOrder("#2", 0, 1, 2);
        ////    Assert.Equal(new Size(100, 60), grid.DesiredSize);

        ////    grid.ChangeRow(2, 1);
        ////    grid.Reset();
        ////    grid.InvalidateMeasure();
        ////    grid.CheckMeasureArgs("#3", new Size(50, double.PositiveInfinity));
        ////    grid.CheckMeasureOrder("#4", 2);
        ////    Assert.Equal(new Size(100, 80), grid.DesiredSize);

        ////    grid.InvalidateMeasure();
        ////    ((Control)c.Content).Height = 100;

        ////    grid.Reset();
        ////    grid.Measure(new Size(500, 400));
        ////    grid.CheckMeasureArgs("#5", new Size(50, double.PositiveInfinity), new Size(50, double.PositiveInfinity), new Size(50, double.PositiveInfinity));
        ////    Assert.Equal(new Size(100, 100), grid.DesiredSize);

        ////    grid.Reset();
        ////    grid.ChangeRow(2, 2);
        ////    grid.Measure(new Size(500, 400));
        ////    grid.CheckMeasureArgs("#7", new Size(50, double.PositiveInfinity));
        ////    grid.CheckMeasureOrder("#8", 2);
        ////    Assert.Equal(new Size(100, 120), grid.DesiredSize);
        ////}

        [Fact]
        public void ChangingGridPropertiesInvalidates()
        {
            // Normally remeasuring with the same width/height does not result in MeasureOverride
            // being called, but if we change a grid property, it does.
            MyGrid g = new MyGrid();
            g.RowDefinitions = new RowDefinitions("Auto,Auto,Auto");
            g.ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto");
            g.AddChild(DecoratorWithChild(), 0, 0, 1, 1);

            g.Measure(new Size(50, 50));
            g.CheckMeasureArgs("#1", new Size(double.PositiveInfinity, double.PositiveInfinity));

            g.Reset();
            g.Measure(new Size(50, 50));
            g.CheckMeasureArgs("#2");

            g.ChangeRowSpan(0, 2);
            g.Reset();
            g.Measure(new Size(50, 50));
            g.CheckMeasureArgs("#3", new Size(double.PositiveInfinity, double.PositiveInfinity));

            g.ChangeColSpan(0, 2);
            g.Reset();
            g.Measure(new Size(50, 50));
            g.CheckMeasureArgs("#4", new Size(double.PositiveInfinity, double.PositiveInfinity));

            g.ChangeRow(0, 1);
            g.Reset();
            g.Measure(new Size(50, 50));
            g.CheckMeasureArgs("#5", new Size(double.PositiveInfinity, double.PositiveInfinity));

            g.ChangeCol(0, 1);
            g.Reset();
            g.Measure(new Size(50, 50));
            g.CheckMeasureArgs("#6", new Size(double.PositiveInfinity, double.PositiveInfinity));
        }

        ////        [Fact]
        ////        [Asynchronous]
        ////        public void MeasureAutoRows3()
        ////        {
        ////            Grid grid = new Grid();

        ////            grid.AddColumns(new GridLength(50), new GridLength(50));
        ////            grid.AddRows(GridLength.Auto, GridLength.Auto, GridLength.Auto);

        ////            grid.AddChild(new MyContentControl(50, 50), 0, 1, 2, 1);
        ////            grid.AddChild(new MyContentControl(50, 60), 1, 1, 1, 1);
        ////            grid.AddChild(new MyContentControl(50, 70), 0, 1, 3, 1);

        ////            CreateAsyncTest(grid, () => {
        ////                CheckRowHeights(grid, "#1", 3.33, 63.33, 3.33);
        ////            });
        ////        }

        ////        [Fact]
        ////        [Asynchronous]
        ////        public void MeasureAutoRows4()
        ////        {
        ////            Grid grid = new Grid();

        ////            grid.AddColumns(new GridLength(50), new GridLength(50));
        ////            grid.AddRows(GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto);

        ////            grid.AddChild(new MyContentControl(50, 30), 0, 1, 3, 1);
        ////            grid.AddChild(new MyContentControl(50, 90), 0, 1, 1, 1);
        ////            grid.AddChild(new MyContentControl(50, 50), 0, 1, 2, 1);

        ////            grid.AddChild(new MyContentControl(50, 70), 1, 1, 4, 1);
        ////            grid.AddChild(new MyContentControl(50, 120), 1, 1, 2, 1);
        ////            grid.AddChild(new MyContentControl(50, 30), 2, 1, 3, 1);

        ////            grid.AddChild(new MyContentControl(50, 10), 3, 1, 1, 1);
        ////            grid.AddChild(new MyContentControl(50, 50), 3, 1, 2, 1);
        ////            grid.AddChild(new MyContentControl(50, 80), 3, 1, 2, 1);

        ////            grid.AddChild(new MyContentControl(50, 20), 4, 1, 1, 1);

        ////            CreateAsyncTest(grid, () => {
        ////                CheckRowHeights(grid, "#1", 90, 60, 60, 35, 45);
        ////            });
        ////        }

        ////        [Fact]
        ////        public void MeasureAutoAndFixedRows()
        ////        {
        ////            Grid grid = new Grid { };

        ////            grid.AddColumns(new GridLength(50), new GridLength(50));
        ////            grid.AddRows(new GridLength(20), new GridLength(20));
        ////            grid.AddChild(new MyContentControl(50, 50), 0, 1, 2, 1);

        ////            grid.Measure(Size.Infinity);
        ////            CheckRowHeights(grid, "#1", 20, 20);
        ////            grid.CheckMeasureSizes("#2", new Size(50, 40));
        ////            Assert.Equal(new Size(100, 40), grid.DesiredSize);

        ////            grid.RowDefinitions[0].Height = new GridLength(30);
        ////            grid.Measure(Size.Infinity);
        ////            CheckRowHeights(grid, "#4", 30, 20);
        ////            grid.CheckMeasureSizes("#5", new Size(50, 50));
        ////            Assert.Equal(new Size(100, 50), grid.DesiredSize);

        ////            grid.RowDefinitions.Insert(0, new RowDefinition { Height = GridLength.Auto });
        ////            grid.Measure(Size.Infinity);
        ////            CheckRowHeights(grid, "#7", double.PositiveInfinity, 30, 20);
        ////            grid.CheckMeasureSizes("#8", new Size(50, double.PositiveInfinity));
        ////            Assert.Equal(new Size(100, 70), grid.DesiredSize);

        ////            grid.Children.Clear();
        ////            grid.AddChild(new MyContentControl(50, 150), 0, 1, 2, 1);
        ////            grid.Measure(Size.Infinity);
        ////            grid.CheckDesired("#13", new Size(50, 150));
        ////            CheckRowHeights(grid, "#10", double.PositiveInfinity, 30, 20);
        ////            grid.CheckMeasureSizes("#11", new Size(50, double.PositiveInfinity));
        ////            grid.CheckMeasureResult("#12", new Size(50, 150));
        ////            Assert.Equal(new Size(100, 170), grid.DesiredSize);
        ////        }

        ////        [Fact]
        ////        [MoonlightBug("Layout rounding regression")]
        ////        public void MeasureAutoAndStarRows()
        ////        {
        ////            MyGrid grid = new MyGrid();

        ////            grid.AddColumns(new GridLength(50));
        ////            grid.AddRows(GridLength.Auto, GridLength.Auto, new GridLength(1, GridUnitType.Star), GridLength.Auto, GridLength.Auto);

        ////            grid.AddChild(new MyContentControl(50, 50), 0, 0, 3, 1);
        ////            grid.AddChild(new MyContentControl(50, 60), 1, 0, 3, 1);

        ////            grid.Measure(new Size(100, 100));
        ////            CheckRowHeights(grid, "#1", double.PositiveInfinity, double.PositiveInfinity, 100, double.PositiveInfinity, double.PositiveInfinity);
        ////            grid.CheckMeasureArgs("#2", new Size(50, 100), new Size(50, 100));
        ////            grid.CheckMeasureOrder("#3", 0, 1);
        ////            Assert.Equal(new Size(50, 60), grid.DesiredSize);

        ////            grid.RowDefinitions[2].MaxHeight = 15;
        ////            grid.Reset();
        ////            grid.Measure(new Size(100, 100));
        ////            CheckRowHeights(grid, "#5", double.PositiveInfinity, double.PositiveInfinity, 15, double.PositiveInfinity, double.PositiveInfinity);
        ////            grid.CheckMeasureArgs("#6", new Size(50, 15), new Size(50, 15));
        ////            Assert.Equal(new Size(50, 15), grid.DesiredSize);

        ////            grid.RowDefinitions.Clear();
        ////            grid.AddRows(GridLength.Auto, GridLength.Auto, GridLength.Auto, new GridLength(1, GridUnitType.Star), GridLength.Auto);
        ////            grid.Reset();
        ////            grid.Measure(new Size(100, 100));
        ////            CheckRowHeights(grid, "#8", double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, 50, double.PositiveInfinity);
        ////            grid.CheckMeasureArgs("#9", new Size(50, double.PositiveInfinity), new Size(50, 83.33));
        ////            Assert.Equal(new Size(50, 77), grid.DesiredSize);

        ////            grid.RowDefinitions[3].MaxHeight = 15;
        ////            grid.Reset();
        ////            grid.Measure(new Size(100, 100));
        ////            CheckRowHeights(grid, "#11", double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, 15, double.PositiveInfinity);
        ////            grid.CheckMeasureArgs("#12", new Size(50, 48.8));
        ////            grid.CheckMeasureOrder("#13", 1);
        ////            Assert.Equal(new Size(50, 65), grid.DesiredSize);
        ////        }

        ////        [Fact]
        ////        public void StarStarRows_LimitedHeight_RowSpan_ExactSpace()
        ////        {
        ////            var grid = new Grid();
        ////            grid.RowDefinitions.Add(new RowDefinition { Height = Star });
        ////            grid.RowDefinitions.Add(new RowDefinition { MaxHeight = 20, Height = Star });

        ////            var child1 = DecoratorWithChild();
        ////            var child2 = DecoratorWithChild();
        ////            (child1.Content as FrameworkElement).Height = 50;
        ////            (child2.Content as FrameworkElement).Height = 70;

        ////            grid.AddChild(child1, 0, 0, 1, 1);
        ////            grid.AddChild(child2, 0, 0, 2, 1);

        ////            Action<Size> sized = delegate {
        ////                Assert.Equal(50, grid.RowDefinitions[0].ActualHeight, "#row 0 sized");
        ////                Assert.Equal(20, grid.RowDefinitions[1].ActualHeight, "#row 1 sized");
        ////            };

        ////            child1.MeasureHook = sized;
        ////            child2.MeasureHook = sized;
        ////            grid.Measure(new Size(70, 70));

        ////            // The row definitions have already been fully sized before the first
        ////            // call to measure a child
        ////            Assert.Equal(new Size(70, 50), child1.MeasureOverrideArg);
        ////            Assert.Equal(new Size(70, 70), child2.MeasureOverrideArg);
        ////            Assert.Equal(new Size(50, 70), grid.DesiredSize);
        ////        }

        ////        [Fact]
        ////        public void StarStarRows_LimitedHeight_RowSpan_InfiniteSpace()
        ////        {
        ////            var grid = new Grid();
        ////            grid.RowDefinitions.Add(new RowDefinition { Height = Star });
        ////            grid.RowDefinitions.Add(new RowDefinition { MaxHeight = 20, Height = Star });

        ////            var child1 = DecoratorWithChild();
        ////            var child2 = DecoratorWithChild();
        ////            (child1.Content as FrameworkElement).Height = 50;
        ////            (child2.Content as FrameworkElement).Height = 70;
        ////            grid.AddChild(child1, 0, 0, 1, 1);
        ////            grid.AddChild(child2, 0, 0, 2, 1);

        ////            grid.Measure(Size.Infinity);
        ////            Assert.Equal(Infinity, child1.MeasureOverrideArg);
        ////            Assert.Equal(Infinity, child2.MeasureOverrideArg);
        ////            Assert.Equal(new Size(50, 70), grid.DesiredSize);
        ////        }

        ////        [Fact]
        ////        public void StarStarRows_StarCol_LimitedHeight()
        ////        {
        ////            var g = new Grid();
        ////            var child = DecoratorWithChild();

        ////            g.RowDefinitions.Add(new RowDefinition { Height = Star });
        ////            g.RowDefinitions.Add(new RowDefinition { Height = Star, MaxHeight = 20 });
        ////            g.AddChild(child, 0, 0, 1, 1);

        ////            g.Measure(new Size(100, 100));
        ////            Assert.Equal(new Size(100, 80), child.MeasureOverrideArg);
        ////        }

        ////        [Fact]
        ////        public void StarRow_AutoCol_LimitedHeigth()
        ////        {
        ////            var g = new Grid();
        ////            var child = DecoratorWithChild();

        ////            g.RowDefinitions.Add(new RowDefinition { Height = Star });
        ////            g.RowDefinitions.Add(new RowDefinition { Height = Star, MaxHeight = 20 });
        ////            g.ColumnDefinitions.Add(new ColumnDefinition { Width = Auto });
        ////            g.AddChild(child, 0, 0, 1, 1);

        ////            g.Measure(new Size(100, 100));
        ////            Assert.Equal(new Size(double.PositiveInfinity, 80), child.MeasureOverrideArg);
        ////        }

        ////        [Fact]
        ////        public void StarRow_AutoStarCol_LimitedWidth()
        ////        {
        ////            var g = new Grid();
        ////            var child = DecoratorWithChild();

        ////            g.RowDefinitions.Add(new RowDefinition { Height = Star });
        ////            g.ColumnDefinitions.Add(new ColumnDefinition { Width = Auto });
        ////            g.ColumnDefinitions.Add(new ColumnDefinition { Width = Star, MaxWidth = 20 });
        ////            g.AddChild(child, 0, 0, 1, 1);

        ////            g.Measure(new Size(100, 100));
        ////            Assert.Equal(new Size(double.PositiveInfinity, 100), child.MeasureOverrideArg);
        ////        }

        ////        [Fact]
        ////        public void AutoRow_StarCol()
        ////        {
        ////            var g = new Grid();
        ////            var child = DecoratorWithChild();
        ////            g.RowDefinitions.Add(new RowDefinition { Height = Star });
        ////            g.RowDefinitions.Add(new RowDefinition { Height = Star, MaxHeight = 20 });

        ////            g.AddChild(child, 0, 0, 1, 1);
        ////            g.Measure(new Size(100, 100));
        ////            Assert.Equal(new Size(100, 80), child.MeasureOverrideArg);
        ////        }

        ////        [Fact]
        ////        [Asynchronous]
        ////        public void FixedGridAllStar()
        ////        {
        ////            // Specify the width/height on the grid and measure the widths/heights of the rows/cols
        ////            GridLength oneStar = new GridLength(1, GridUnitType.Star);
        ////            GridLength twoStar = new GridLength(2, GridUnitType.Star);
        ////            GridLength threeStar = new GridLength(3, GridUnitType.Star);

        ////            MyGrid g = new MyGrid { Name = "Ted", ShowGridLines = true, Width = 240, Height = 240 };
        ////            g.AddColumns(twoStar, oneStar, twoStar, oneStar);
        ////            g.AddRows(oneStar, threeStar, oneStar, oneStar);
        ////            CreateAsyncTest(g, () => {
        ////                g.CheckRowHeights("#1", 40, 120, 40, 40);
        ////                g.CheckColWidths("#2", 80, 40, 80, 40);
        ////                Assert.Equal(new Size(240, 240), g.DesiredSize);
        ////            });
        ////        }

        ////        [Fact]
        ////        public void UnfixedGridAllStar()
        ////        {
        ////            // Check the widths/heights of the rows/cols without specifying a size for the grid
        ////            // Measuring the rows initialises the sizes to Infinity for 'star' elements
        ////            Grid grid = new Grid();
        ////            grid.AddRows(new GridLength(1, GridUnitType.Star));
        ////            grid.AddColumns(new GridLength(1, GridUnitType.Star));

        ////            // Initial values
        ////            Assert.Equal(new Size(0, 0), grid.DesiredSize);
        ////            Assert.Equal(0, grid.RowDefinitions[0].ActualHeight);
        ////            Assert.Equal(0, grid.ColumnDefinitions[0].ActualWidth);

        ////            // After measure
        ////            grid.Measure(Size.Infinity);
        ////            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
        ////            Assert.Equal(new Size(0, 0), grid.DesiredSize);
        ////            Assert.Equal(0, grid.RowDefinitions[0].ActualHeight);
        ////            Assert.Equal(0, grid.ColumnDefinitions[0].ActualWidth);

        ////            // Measure again
        ////            grid.Measure(new Size(100, 100));
        ////            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
        ////            Assert.Equal(new Size(0, 0), grid.DesiredSize);
        ////            Assert.Equal(0, grid.RowDefinitions[0].ActualHeight);
        ////            Assert.Equal(0, grid.ColumnDefinitions[0].ActualWidth);
        ////        }

        ////        [Fact]
        ////        public void MeasureStarRowsNoChild()
        ////        {
        ////            // Measuring the rows initialises the sizes to Infinity for 'star' elements
        ////            double double.PositiveInfinity = double.PositiveInfinity;
        ////            Grid grid = new Grid();
        ////            grid.AddRows(new GridLength(1, GridUnitType.Star));
        ////            grid.AddColumns(new GridLength(1, GridUnitType.Star));

        ////            // Initial values
        ////            Assert.Equal(new Size(0, 0), grid.DesiredSize);
        ////            Assert.Equal(0, grid.RowDefinitions[0].ActualHeight);
        ////            Assert.Equal(0, grid.ColumnDefinitions[0].ActualWidth);

        ////            // After measure
        ////            grid.Measure(Size.Infinity);
        ////            Assert.Equal(new Size(0, 0), grid.DesiredSize);
        ////            Assert.Equal(double.PositiveInfinity, grid.RowDefinitions[0].ActualHeight);
        ////            Assert.Equal(double.PositiveInfinity, grid.ColumnDefinitions[0].ActualWidth);

        ////            // Measure again
        ////            grid.Measure(new Size(100, 100));
        ////            Assert.Equal(new Size(0, 0), grid.DesiredSize);
        ////            Assert.Equal(double.PositiveInfinity, grid.RowDefinitions[0].ActualHeight);
        ////            Assert.Equal(double.PositiveInfinity, grid.ColumnDefinitions[0].ActualWidth);
        ////        }

        ////        [Fact]
        ////        [Asynchronous]
        ////        public void RowspanAutoTest()
        ////        {
        ////            // This test demonstrates the following rules:
        ////            // 1) Elements with RowSpan/ColSpan == 1 distribute their height first
        ////            // 2) The rest of the elements distribute height in LIFO order
        ////            Grid grid = new Grid();
        ////            grid.AddRows(GridLength.Auto, GridLength.Auto, GridLength.Auto);
        ////            grid.AddColumns(new GridLength(50));

        ////            var child50 = new MyContentControl(50, 50);
        ////            var child60 = new MyContentControl(50, 60);

        ////            grid.AddChild(child50, 0, 0, 1, 1);
        ////            grid.AddChild(child60, 0, 0, 1, 1);

        ////            CreateAsyncTest(grid,
        ////                () => {
        ////                    // Check the initial values
        ////                    CheckRowHeights(grid, "#1", 60, 0, 0);

        ////                    // Now make the smaller element use rowspan = 2
        ////                    Grid.SetRowSpan(child50, 2);
        ////                }, () => {
        ////                    CheckRowHeights(grid, "#2", 60, 0, 0);

        ////                    // Then make the larger element us rowspan = 2
        ////                    Grid.SetRowSpan(child50, 1);
        ////                    Grid.SetRowSpan(child60, 2);
        ////                }, () => {
        ////                    CheckRowHeights(grid, "#3", 55, 5, 0);

        ////                    // Swap the order in which they are added to the grid
        ////                    grid.Children.Clear();
        ////                    grid.AddChild(child60, 0, 0, 2, 0);
        ////                    grid.AddChild(child50, 0, 0, 1, 0);
        ////                }, () => {
        ////                    // Swapping the order has no effect here
        ////                    CheckRowHeights(grid, "#4", 55, 5, 0);

        ////                    // Then give both rowspan = 2
        ////                    Grid.SetRowSpan(child50, 2);
        ////                }, () => {
        ////                    CheckRowHeights(grid, "#5", 30, 30, 0);

        ////                    // Finally give the larger element rowspan = 3
        ////                    Grid.SetRowSpan(child60, 3);
        ////                }, () => {
        ////                    CheckRowHeights(grid, "#6", 28.333, 28.333, 3.333);

        ////                    // Swap the order in which the elements are added again
        ////                    grid.Children.Clear();
        ////                    grid.AddChild(child50, 0, 0, 2, 0);
        ////                    grid.AddChild(child60, 0, 0, 3, 0);
        ////                }, () => {
        ////                    CheckRowHeights(grid, "#7", 25, 25, 20);
        ////                }
        ////            );
        ////        }

        ////        [Fact]
        ////        [Asynchronous]
        ////        public void SizeExceedsBounds()
        ////        {
        ////            Grid grid = new Grid();
        ////            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50), MaxHeight = 40, MinHeight = 60 });
        ////            grid.AddChild(new MyContentControl(50, 50), 0, 0, 0, 0);
        ////            CreateAsyncTest(grid, () => {
        ////                Assert.Equal(60, grid.RowDefinitions[0].ActualHeight);
        ////            });
        ////        }

        ////        [Fact]
        ////        [Asynchronous]
        ////        public void SizeExceedsBounds2()
        ////        {
        ////            Grid grid = new Grid();
        ////            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50), MaxHeight = 60, MinHeight = 40 });
        ////            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50), MaxHeight = 60, MinHeight = 40 });
        ////            grid.AddChild(new MyContentControl(100, 1000), 0, 0, 0, 0);
        ////            CreateAsyncTest(grid,
        ////                () => {
        ////                    Assert.Equal(50, grid.RowDefinitions[0].ActualHeight);
        ////                    grid.ChangeRowSpan(0, 2);
        ////                }, () => {
        ////                    Assert.Equal(50, grid.RowDefinitions[0].ActualHeight);
        ////                }
        ////            );
        ////        }

        ////        [Fact]
        ////        [Asynchronous]
        ////        public void StarAutoConstrainedGrid()
        ////        {
        ////            MyGrid g = new MyGrid { Width = 170, Height = 170 };
        ////            g.AddRows(GridLength.Auto, new GridLength(1, GridUnitType.Star));
        ////            g.AddColumns(GridLength.Auto, new GridLength(1, GridUnitType.Star));

        ////            g.AddChild(DecoratorWithChild(), 0, 1, 1, 1);
        ////            g.AddChild(DecoratorWithChild(), 1, 0, 1, 1);
        ////            g.AddChild(DecoratorWithChild(), 1, 1, 1, 1);
        ////            g.AddChild(DecoratorWithChild(), 0, 0, 1, 1);

        ////            foreach (MyContentControl child in g.Children)
        ////            {
        ////                Assert.Equal(0, child.ActualHeight, "height");
        ////                Assert.Equal(0, child.ActualWidth, "height");

        ////                Rectangle content = (Rectangle)child.Content;
        ////                Assert.Equal(50, content.ActualHeight, "content height");
        ////                Assert.Equal(50, content.ActualWidth, "content width");
        ////            }

        ////            CreateAsyncTest(g, () => {
        ////                g.CheckFinalMeasureArg("#1",
        ////                    new Size(120, double.PositiveInfinity), new Size(double.PositiveInfinity, 120),
        ////                    new Size(120, 120), new Size(double.PositiveInfinity, double.PositiveInfinity));

        ////            });
        ////        }

        ////        [Fact]
        ////        public void StarAutoConstrainedGrid2()
        ////        {
        ////            MyGrid g = new MyGrid { Width = 170, Height = 170 };
        ////            g.AddRows(GridLength.Auto, new GridLength(1, GridUnitType.Star));
        ////            g.AddColumns(GridLength.Auto, new GridLength(1, GridUnitType.Star));

        ////            g.AddChild(DecoratorWithChild(), 0, 1, 1, 1);
        ////            g.AddChild(DecoratorWithChild(), 1, 0, 1, 1);
        ////            g.AddChild(DecoratorWithChild(), 1, 1, 1, 1);
        ////            g.AddChild(DecoratorWithChild(), 0, 0, 1, 1);

        ////            foreach (MyContentControl child in g.Children)
        ////            {
        ////                Assert.Equal(0, child.ActualHeight, "height");
        ////                Assert.Equal(0, child.ActualWidth, "height");

        ////                Rectangle content = (Rectangle)child.Content;
        ////                Assert.Equal(50, content.ActualHeight, "content height");
        ////                Assert.Equal(50, content.ActualWidth, "content width");
        ////            }
        ////            g.Measure(new Size(170, 170));
        ////            g.CheckFinalMeasureArg("#1",
        ////                    new Size(120, double.PositiveInfinity), new Size(double.PositiveInfinity, 120),
        ////                    new Size(120, 120), new Size(double.PositiveInfinity, double.PositiveInfinity));
        ////        }

        [Fact]
        public void StarAutoIsNotInfinite()
        {
            var child1 = new MyContentControl { };
            var child2 = new MyContentControl { };
            MyGrid grid = new MyGrid();
            grid.RowDefinitions = new RowDefinitions("Auto,Auto,Auto,*");
            grid.ColumnDefinitions = new ColumnDefinitions("Auto,*");

            grid.AddChild(child1, 0, 0, 1, 1);
            grid.AddChild(child2, 0, 0, 4, 2);

            grid.Measure(new Size(100, 100));
            Assert.Equal(Size.Infinity, child1.MeasureOverrideArg);
            Assert.Equal(new Size(100, 100), child2.MeasureOverrideArg);
        }

        ////        [Fact]
        ////        [Asynchronous]
        ////        public void StarRows()
        ////        {
        ////            GridUnitType star = GridUnitType.Star;
        ////            MyGrid grid = new MyGrid { Name = "TESTER", Width = 100, Height = 210 };
        ////            grid.AddRows(new GridLength(1, star), new GridLength(2, star));
        ////            grid.AddChild(new MyContentControl(50, 50), 0, 0, 0, 0);
        ////            CreateAsyncTest(grid,
        ////                () => {
        ////                    CheckRowHeights(grid, "#1", 70, 140);
        ////                    grid.CheckMeasureArgs("#1a", new Size(100, 70));
        ////                    grid.AddRows(new GridLength(30));
        ////                    grid.Reset();
        ////                }, () => {
        ////                    CheckRowHeights(grid, "#2", 60, 120, 30);
        ////                    grid.CheckMeasureArgs("#2a", new Size(100, 60));
        ////                    grid.Reset();

        ////                    // Add a child to the fixed row
        ////                    grid.AddChild(new MyContentControl(50, 80), 2, 0, 0, 0);
        ////                }, () => {
        ////                    CheckRowHeights(grid, "#3", 60, 120, 30);
        ////                    grid.CheckMeasureArgs("#3a", new Size(100, 30));
        ////                    grid.Reset();

        ////                    // Make the child span the last two rows
        ////                    grid.ChangeRow(1, 1);
        ////                    grid.ChangeRowSpan(1, 2);
        ////                }, () => {
        ////                    CheckRowHeights(grid, "#4", 60, 120, 30);
        ////                    grid.CheckMeasureArgs("#4a", new Size(100, 150));
        ////                    grid.Reset();

        ////                    // Add another fixed row and move the large child to span both
        ////                    grid.AddRows(new GridLength(30));
        ////                    grid.ChangeRow(1, 2);
        ////                }, () => {
        ////                    grid.CheckFinalMeasureArg("#MeasureArgs", new Size(100, 50), new Size(100, 60));
        ////                    CheckRowHeights(grid, "#5", 50, 100, 30, 30);
        ////                }
        ////            );
        ////        }

        ////        [Fact]
        ////        [Asynchronous]
        ////        public void StarRows2()
        ////        {
        ////            GridUnitType star = GridUnitType.Star;
        ////            MyGrid grid = new MyGrid { Width = 100, Height = 210 };
        ////            grid.AddRows(new GridLength(1, star), new GridLength(2, star));
        ////            grid.AddChild(new MyContentControl(50, 50), 0, 0, 0, 0);
        ////            CreateAsyncTest(grid,
        ////                () => {
        ////                    CheckRowHeights(grid, "#1", 70, 140);
        ////                    grid.CheckMeasureArgs("#1b", new Size(100, 70));
        ////                    grid.AddRows(GridLength.Auto);

        ////                    grid.Reset();
        ////                }, () => {
        ////                    CheckRowHeights(grid, "#2", 70, 140, 0);
        ////                    grid.CheckMeasureArgs("#2b"); // MeasureOverride isn't called

        ////                    // Add a child to the fixed row
        ////                    grid.AddChild(new MyContentControl(50, 80), 2, 0, 0, 0);
        ////                    grid.Reset();
        ////                }, () => {
        ////                    CheckRowHeights(grid, "#3", 43, 87, 80);
        ////                    grid.CheckMeasureArgs("#3b", new Size(100, double.PositiveInfinity), new Size(100, 43));
        ////                    grid.CheckMeasureOrder("#3c", 1, 0);

        ////                    // Make the child span the last two rows
        ////                    grid.ChangeRow(1, 1);
        ////                    grid.ChangeRowSpan(1, 2);
        ////                    grid.Reset();
        ////                }, () => {
        ////                    CheckRowHeights(grid, "#4", 70, 140, 0);
        ////                    grid.CheckMeasureArgs("#4b", new Size(100, 70), new Size(100, 140));
        ////                    grid.CheckMeasureOrder("#4c", 0, 1);

        ////                    // Add another fixed row and move the large child to span both
        ////                    grid.AddRows(GridLength.Auto);
        ////                    grid.ChangeRow(1, 2);
        ////                    grid.Reset();
        ////                }, () => {
        ////                    CheckRowHeights(grid, "#5", 43, 87, 40, 40);
        ////                    grid.CheckMeasureArgs("#5b", new Size(100, double.PositiveInfinity), new Size(100, 43));
        ////                    grid.CheckMeasureOrder("#5c", 1, 0);
        ////                }
        ////            );
        ////        }

        ////        class PanelPoker : Panel
        ////        {
        ////            public List<Size> ArrangeArgs = new List<Size>();
        ////            public List<Size> ArrangeResults = new List<Size>();

        ////            public List<Size> MeasureArgs = new List<Size>();
        ////            public List<Size> MeasureResults = new List<Size>();

        ////            public MyGrid Grid { get; set; }

        ////            public PanelPoker()
        ////            {
        ////            }

        ////            protected override Size ArrangeOverride(Size finalSize)
        ////            {
        ////                ArrangeArgs.Add(finalSize);
        ////                Grid.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
        ////                ArrangeResults.Add(new Size(Grid.ActualWidth, Grid.ActualHeight));
        ////                return ArrangeResults.Last();
        ////            }

        ////            protected override Size MeasureOverride(Size availableSize)
        ////            {
        ////                MeasureArgs.Add(availableSize);
        ////                Grid.Measure(availableSize);
        ////                MeasureResults.Add(Grid.DesiredSize);
        ////                return MeasureResults.Last();
        ////            }
        ////        }

        ////        [Fact]
        ////        [Asynchronous]
        ////        public void StarRows3()
        ////        {
        ////            GridLength oneStar = new GridLength(1, GridUnitType.Star);
        ////            MyGrid grid = new MyGrid();
        ////            grid.AddRows(oneStar, oneStar, oneStar);
        ////            grid.AddColumns(oneStar, oneStar, oneStar);

        ////            Canvas canvas = new Canvas { Width = 120, Height = 120 };
        ////            canvas.Children.Add(grid);
        ////            grid.AddChild(new MyContentControl(100, 100), 1, 1, 1, 1);

        ////            CreateAsyncTest(canvas,
        ////                () => { },
        ////                () => {
        ////                    CheckRowHeights(grid, "#3", 0, 100, 0);

        ////                    grid.CheckMeasureArgs("#1", Infinity);
        ////                    grid.CheckMeasureResult("#2", new Size(100, 100));

        ////                    CheckRowHeights(grid, "#3", 0, 100, 0);
        ////                    grid.CheckArrangeArgs("#4", new Size(100, 100));
        ////                    grid.CheckArrangeResult("#5", new Size(100, 100));
        ////                }
        ////            );
        ////        }

        ////        [Fact]
        ////        [Asynchronous]
        ////        public void StarRows3b()
        ////        {
        ////            var canvas = new Canvas { Width = 120, Height = 120 };
        ////            PanelPoker poker = new PanelPoker();
        ////            MyGrid grid = new MyGrid();
        ////            grid.AddRows(Star, Star, Star);
        ////            grid.AddColumns(Star, Star, Star);

        ////            canvas.Children.Add(poker);
        ////            poker.Grid = grid;
        ////            grid.AddChild(new MyContentControl(100, 100), 1, 1, 1, 1);

        ////            CreateAsyncTest(canvas,
        ////                () => { },
        ////                () => {
        ////                    Assert.Equal(Infinity, poker.MeasureArgs[0]);
        ////                    Assert.Equal(new Size(100, 100), poker.MeasureResults[0]);
        ////                    Assert.Equal(new Size(100, 100), poker.ArrangeArgs[0]);
        ////                    Assert.Equal(new Size(100, 100), poker.ArrangeResults[0]);

        ////                    CheckRowHeights(grid, "#5", 0, 100, 0);
        ////                    grid.CheckColWidths("#6", 0, 100, 0);

        ////                    grid.CheckMeasureArgs("#7", Infinity);
        ////                    grid.CheckMeasureResult("#8", new Size(100, 100));

        ////                    grid.CheckArrangeArgs("#9", new Size(100, 100));
        ////                    grid.CheckArrangeResult("#10", new Size(100, 100));
        ////                }
        ////            );
        ////        }

        ////        [Fact]
        ////        [MoonlightBug("For some bizarre reason, calling Arrange here *does not* result in the children being arranged.")]
        ////        public void StarRows5()
        ////        {
        ////            GridLength oneStar = new GridLength(1, GridUnitType.Star);
        ////            MyGrid grid = new MyGrid { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        ////            grid.AddRows(oneStar, oneStar, oneStar);
        ////            grid.AddColumns(oneStar, oneStar, oneStar);

        ////            grid.AddChild(new MyContentControl(240, 240), 0, 0, 3, 3);
        ////            grid.AddChild(new MyContentControl(150, 150), 0, 0, 1, 1);

        ////            TestPanel.Children.Add(grid);
        ////            grid.Measure(new Size(240, 240));
        ////            grid.Arrange(new Rect(0, 0, 120, 120));

        ////            CheckRowHeights(grid, "#1", 80, 80, 80);
        ////            grid.CheckMeasureArgs("#2", new Size(240, 240), new Size(80, 80));
        ////            grid.CheckMeasureResult("#3", new Size(240, 240), new Size(80, 80));
        ////            grid.CheckDesired("#4", new Size(240, 240), new Size(80, 80));
        ////            grid.CheckMeasureOrder("#5", 0, 1);
        ////        }

        ////        [Fact]
        ////        [Asynchronous]
        ////        public void AutoRows()
        ////        {
        ////            // This checks that rows expand to be large enough to hold the largest child
        ////            Grid grid = new Grid();

        ////            grid.AddColumns(new GridLength(50), new GridLength(50));
        ////            grid.AddRows(GridLength.Auto, GridLength.Auto);

        ////            grid.AddChild(new LayoutPoker { Width = 50, Height = 50 }, 0, 0, 1, 1);
        ////            grid.AddChild(new LayoutPoker { Width = 50, Height = 60 }, 0, 1, 1, 1);

        ////            CreateAsyncTest(grid,
        ////                () => {
        ////                    CheckRowHeights(grid, "#1", 60, 0);
        ////                    Grid.SetRow((FrameworkElement)grid.Children[1], 1);
        ////                }, () => {
        ////                    CheckRowHeights(grid, "#2", 50, 60);
        ////                }
        ////            );
        ////        }

        ////        [Fact]
        ////        [Asynchronous]
        ////        public void AutoRows2()
        ////        {
        ////            // Start off with two elements in the first row with the smaller element having rowspan = 2
        ////            // and see how rowspan affects the rendering.
        ////            Grid grid = new Grid();

        ////            grid.AddColumns(new GridLength(50), new GridLength(50));
        ////            grid.AddRows(GridLength.Auto, GridLength.Auto, GridLength.Auto);

        ////            grid.AddChild(new LayoutPoker { Width = 50, Height = 50 }, 0, 0, 2, 1);
        ////            grid.AddChild(new LayoutPoker { Width = 50, Height = 60 }, 0, 1, 1, 1);

        ////            // Start off with both elements at row 1, and the smaller element having rowspan = 2
        ////            CreateAsyncTest(grid,
        ////                () => {
        ////                    // If an element spans across multiple rows and one of those rows
        ////                    // is already large enough to contain that element, it puts itself
        ////                    // entirely inside that row
        ////                    CheckRowHeights(grid, "#1", 60, 0, 0);

        ////                    grid.ChangeRow(1, 1);
        ////                }, () => {
        ////                    // An 'auto' row which has no children whose rowspan/colspan
        ////                    // *ends* in that row has a height of zero
        ////                    CheckRowHeights(grid, "#2", 0, 60, 0);
        ////                    grid.ChangeRow(1, 2);
        ////                }, () => {
        ////                    // If an element which spans multiple rows is the only element in
        ////                    // the rows it spans, it divides evenly between the rows it spans
        ////                    CheckRowHeights(grid, "#2", 25, 25, 60);
        ////                    grid.ChangeRow(1, 0);
        ////                    grid.ChangeRow(0, 1);
        ////                }, () => {
        ////                    CheckRowHeights(grid, "#2", 60, 25, 25);
        ////                }
        ////            );
        ////        }

        ////        [Fact]
        ////        [Asynchronous]
        ////        public void AutoRows3()
        ////        {
        ////            // Start off with two elements in the first row with the larger element having rowspan = 2
        ////            // and see how rowspan affects the rendering.
        ////            Grid grid = new Grid();

        ////            grid.AddColumns(new GridLength(50), new GridLength(50));
        ////            grid.AddRows(GridLength.Auto, GridLength.Auto, GridLength.Auto);

        ////            grid.AddChild(new LayoutPoker { Width = 50, Height = 50 }, 0, 0, 1, 1);
        ////            grid.AddChild(new LayoutPoker { Width = 50, Height = 60 }, 0, 1, 2, 1);

        ////            CreateAsyncTest(grid,
        ////                () => {
        ////                    CheckRowHeights(grid, "#1", 55, 5, 0);
        ////                    grid.ChangeRow(1, 1);
        ////                }, () => {
        ////                    CheckRowHeights(grid, "#2", 50, 30, 30);
        ////                    grid.ChangeRow(1, 2);
        ////                }, () => {
        ////                    CheckRowHeights(grid, "#3", 50, 0, 60);
        ////                    grid.ChangeRow(1, 0);
        ////                    grid.ChangeRow(0, 1);
        ////                }, () => {
        ////                    CheckRowHeights(grid, "#3", 5, 55, 0);
        ////                }
        ////            );
        ////        }

        ////        [Fact]
        ////        [Asynchronous]
        ////        public void AutoRows4()
        ////        {
        ////            // See how rowspan = 3 affects this with 5 rows.
        ////            Grid grid = new Grid();

        ////            grid.AddColumns(new GridLength(50), new GridLength(50));
        ////            grid.AddRows(GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto);

        ////            // Give first child a rowspan of 2
        ////            grid.AddChild(new LayoutPoker { Width = 50, Height = 50 }, 0, 0, 1, 1);
        ////            grid.AddChild(new LayoutPoker { Width = 50, Height = 60 }, 0, 1, 3, 1);

        ////            CreateAsyncTest(grid,
        ////                () => {
        ////                    // If an element spans across multiple rows and one of those rows
        ////                    // is already large enough to contain that element, it puts itself
        ////                    // entirely inside that row
        ////                    CheckRowHeights(grid, "#1", 53.33, 3.33, 3.33, 0, 0);
        ////                    grid.ChangeRow(1, 1);
        ////                }, () => {
        ////                    // An 'auto' row which has no children whose rowspan/colspan
        ////                    // *ends* in that row has a height of zero
        ////                    CheckRowHeights(grid, "#2", 50, 20, 20, 20, 0);
        ////                    grid.ChangeRow(1, 2);
        ////                }, () => {
        ////                    // If an element which spans multiple rows is the only element in
        ////                    // the rows it spans, it divides evenly between the rows it spans
        ////                    CheckRowHeights(grid, "#3", 50, 0, 20, 20, 20);

        ////                    grid.ChangeRow(1, 0);
        ////                    grid.ChangeRow(0, 1);
        ////                }, () => {
        ////                    // If there are two auto rows beside each other and an element spans those
        ////                    // two rows, the total height is averaged between the two rows.
        ////                    CheckRowHeights(grid, "#4", 3.33, 53.33, 3.33, 0, 0);
        ////                }
        ////            );
        ////        }

        ////        [Fact]
        ////        [Asynchronous]
        ////        public void AutoRows5()
        ////        {
        ////            Grid grid = new Grid();

        ////            grid.AddColumns(new GridLength(50));
        ////            grid.AddRows(GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto);

        ////            grid.AddChild(new LayoutPoker { Width = 50, Height = 50 }, 0, 0, 3, 1);
        ////            grid.AddChild(new LayoutPoker { Width = 50, Height = 60 }, 1, 0, 3, 1);

        ////            // When calculating the heights of automatic rows, the children added to the grid
        ////            // distribute their height in the opposite order in which they were added.
        ////            CreateAsyncTest(grid,
        ////                () => {
        ////                    // Here the element with height 60 distributes its height first
        ////                    CheckRowHeights(grid, "#1", 3.33, 23.33, 23.33, 20, 0);
        ////                    grid.ChangeRow(1, 1);

        ////                    grid.ChangeRow(0, 1);
        ////                    grid.ChangeRow(1, 0);
        ////                }, () => {
        ////                    // Reversing the rows does not stop the '60' element from
        ////                    // Distributing its height first
        ////                    CheckRowHeights(grid, "#2", 20, 23.33, 23.33, 3.33, 0);

        ////                    // Now reverse the order in which the elements are added so that
        ////                    // the '50' element distributes first.
        ////                    grid.Children.Clear();
        ////                    grid.AddChild(new LayoutPoker { Width = 50, Height = 60 }, 1, 0, 3, 1);
        ////                    grid.AddChild(new LayoutPoker { Width = 50, Height = 50 }, 0, 0, 3, 1);

        ////                }, () => {
        ////                    CheckRowHeights(grid, "#3", 16.66, 25.55, 25.55, 8.88, 0);
        ////                    grid.ChangeRow(1, 1);

        ////                    grid.ChangeRow(0, 1);
        ////                    grid.ChangeRow(1, 0);
        ////                }, () => {
        ////                    CheckRowHeights(grid, "#4", 16.66, 25.55, 25.55, 8.88, 0);
        ////                }
        ////            );
        ////        }

        ////        [Fact]
        ////        [Asynchronous]
        ////        public void AutoAndFixedRows()
        ////        {
        ////            MyGrid grid = new MyGrid();

        ////            grid.AddColumns(new GridLength(50));
        ////            grid.AddRows(GridLength.Auto, GridLength.Auto, new GridLength(15), GridLength.Auto, GridLength.Auto);

        ////            grid.AddChild(new MyContentControl(50, 50), 0, 0, 3, 1);
        ////            grid.AddChild(new MyContentControl(50, 60), 1, 0, 3, 1);

        ////            // If an element spans multiple rows and one of them is *not* auto, it attempts to put itself
        ////            // entirely inside that row
        ////            CreateAsyncTest(grid,
        ////                () => {
        ////                    CheckRowHeights(grid, "#1", 0, 0, 60, 0, 0);
        ////                    grid.CheckMeasureArgs("#1b", new Size(50, double.PositiveInfinity), new Size(50, double.PositiveInfinity));
        ////                    grid.CheckMeasureOrder("#1c", 0, 1);

        ////                    // Forcing a maximum height on the fixed row makes it distribute
        ////                    // remaining height among the 'auto' rows.
        ////                    grid.RowDefinitions[2].MaxHeight = 15;
        ////                    grid.Reset();
        ////                }, () => {
        ////                    // Nothing needs to get re-measured, but the heights are redistributed as expected.
        ////                    CheckRowHeights(grid, "#2", 6.25, 28.75, 15, 22.5, 0);
        ////                    grid.CheckMeasureArgs("#2b");
        ////                    grid.CheckMeasureOrder("#2c");

        ////                    grid.RowDefinitions.Clear();
        ////                    grid.AddRows(GridLength.Auto, GridLength.Auto, GridLength.Auto, new GridLength(15), GridLength.Auto);
        ////                    grid.Reset();
        ////                }, () => {
        ////                    // Once again there's no remeasuring, just redistributing.
        ////                    CheckRowHeights(grid, "#3", 16.66, 16.66, 16.66, 60, 0);
        ////                    grid.CheckMeasureArgs("#3b");
        ////                    grid.CheckMeasureOrder("#3c");
        ////                }
        ////            );
        ////        }

        ////        [Fact]
        ////        [Asynchronous]
        ////        public void AutoAndFixedRows2()
        ////        {
        ////            TestPanel.Width = 200;
        ////            TestPanel.Height = 1000;

        ////            MyGrid grid = new MyGrid();
        ////            grid.AddColumns(new GridLength(50), new GridLength(50), new GridLength(50));
        ////            grid.AddRows(new GridLength(30), new GridLength(40), GridLength.Auto, new GridLength(50));
        ////            grid.AddChild(new MyContentControl(600, 600), 0, 0, 4, 4);
        ////            grid.AddChild(new MyContentControl(80, 70), 0, 1, 1, 1);
        ////            grid.AddChild(new MyContentControl(50, 60), 1, 0, 1, 1);
        ////            grid.AddChild(new MyContentControl(10, 500), 1, 1, 1, 1);

        ////            CreateAsyncTest(grid, () => {
        ////                CheckRowHeights(grid, "#1", 190, 200, 0, 210);
        ////                grid.CheckMeasureArgs("#2",
        ////                                            new Size(150, double.PositiveInfinity),
        ////                                            new Size(50, 30),
        ////                                            new Size(50, 40),
        ////                                            new Size(50, 40));
        ////                grid.CheckMeasureOrder("#3", 0, 1, 2, 3);
        ////            });
        ////        }

        ////        [Fact]
        ////        [Asynchronous]
        ////        public void AutoAndFixedRows3()
        ////        {
        ////            Grid grid = new Grid { Width = 10, Height = 10 };
        ////            grid.AddColumns(new GridLength(50), new GridLength(50));
        ////            grid.AddRows(new GridLength(20), new GridLength(20));

        ////            grid.AddChild(new MyContentControl(50, 50), 0, 1, 2, 1);

        ////            CreateAsyncTest(grid,
        ////                () => {
        ////                    CheckRowHeights(grid, "#1", 20, 20);
        ////                    grid.RowDefinitions.Insert(0, new RowDefinition { Height = GridLength.Auto });
        ////                }, () => {
        ////                    CheckRowHeights(grid, "#2", 0, 50, 20);
        ////                    grid.RowDefinitions[1].MaxHeight = 35;
        ////                }, () => {
        ////                    CheckRowHeights(grid, "#3", 15, 35, 20);
        ////                    grid.RowDefinitions[1].MaxHeight = 20;
        ////                    grid.ChangeRowSpan(0, 4);
        ////                }, () => {
        ////                    CheckRowHeights(grid, "#4", 0, 20, 30);
        ////                    grid.AddRows(new GridLength(20));
        ////                }, () => {
        ////                    CheckRowHeights(grid, "#5", 0, 20, 20, 20);
        ////                }
        ////            );
        ////        }

        ////        [Fact]
        ////        [Asynchronous]
        ////        public void AutoAndStarRows()
        ////        {
        ////            TestPanel.MaxHeight = 160;
        ////            MyGrid grid = new MyGrid();

        ////            grid.AddColumns(new GridLength(50));
        ////            grid.AddRows(GridLength.Auto, GridLength.Auto, new GridLength(1, GridUnitType.Star), GridLength.Auto, GridLength.Auto);

        ////            grid.AddChild(new MyContentControl(50, 50), 0, 0, 3, 1);
        ////            grid.AddChild(new MyContentControl(50, 60), 1, 0, 3, 1);

        ////            // Elements will put themselves entirely inside a 'star' row if they ca
        ////            CreateAsyncTest(grid,
        ////                () => {
        ////                    CheckRowHeights(grid, "#1", 0, 0, 160, 0, 0);
        ////                    grid.CheckMeasureArgs("#1b", new Size(50, 160), new Size(50, 160));
        ////                    grid.CheckMeasureOrder("#1c", 0, 1);

        ////                    // Forcing a maximum height on the star row doesn't spread
        ////                    // remaining height among the auto rows.
        ////                    grid.RowDefinitions[2].MaxHeight = 15;
        ////                    grid.Reset();
        ////                }, () => {
        ////                    CheckRowHeights(grid, "#2", 0, 0, 15, 0, 0);
        ////                    grid.CheckMeasureArgs("#2b", new Size(50, 15), new Size(50, 15));
        ////                    grid.CheckMeasureOrder("#2c", 0, 1);

        ////                    grid.RowDefinitions.Clear();
        ////                    grid.AddRows(GridLength.Auto, GridLength.Auto, GridLength.Auto, new GridLength(1, GridUnitType.Star), GridLength.Auto);
        ////                    grid.Reset();
        ////                }, () => {
        ////                    CheckRowHeights(grid, "#3", 16.66, 16.66, 16.66, 110, 0);
        ////                    grid.CheckMeasureArgs("#3b", new Size(50, double.PositiveInfinity), new Size(50, 143.333));
        ////                    grid.CheckMeasureOrder("#3c", 0, 1);

        ////                    grid.RowDefinitions[3].MaxHeight = 15;
        ////                    grid.Reset();
        ////                }, () => {
        ////                    CheckRowHeights(grid, "#4", 16.66, 16.66, 16.66, 15, 0);
        ////                    grid.CheckMeasureArgs("#4b", new Size(50, 48.333));
        ////                    grid.CheckMeasureOrder("#4c", 1);
        ////                }
        ////            );
        ////        }


        ////        [Fact]
        ////        [Asynchronous]
        ////        public void AutoCol_Empty_MaxWidth()
        ////        {
        ////            // Ensure MaxWidth is respected in an empty Auto segment
        ////            var grid = new Grid();
        ////            grid.AddColumns(Auto, Star);
        ////            grid.ColumnDefinitions[0].MaxWidth = 10;
        ////            grid.AddChild(DecoratorWithChild(), 0, 1, 0, 0);

        ////            CreateAsyncTest(grid, () => {
        ////                grid.UpdateLayout();
        ////                Assert.Equal(0, grid.ColumnDefinitions[0].ActualWidth);
        ////            });
        ////        }

        ////        [Fact]
        ////        [Asynchronous]
        ////        public void AutoCol_Empty_MinWidth()
        ////        {
        ////            // Ensure MinWidth is respected in an empty Auto segment
        ////            var grid = new Grid();
        ////            grid.AddColumns(Auto, Star);
        ////            grid.ColumnDefinitions[0].MinWidth = 10;
        ////            grid.AddChild(DecoratorWithChild(), 0, 1, 0, 0);

        ////            CreateAsyncTest(grid, () => {
        ////                grid.UpdateLayout();
        ////                Assert.Equal(10, grid.ColumnDefinitions[0].ActualWidth);
        ////            });
        ////        }

        ////        [Fact]
        ////        [Asynchronous]
        ////        public void AutoCol_MaxWidth()
        ////        {
        ////            // MaxWidth is *not* respected in an Auto segment
        ////            var grid = new Grid();
        ////            grid.AddColumns(Auto, Star);
        ////            grid.ColumnDefinitions[0].MaxWidth = 10;
        ////            grid.AddChild(DecoratorWithChild(), 0, 0, 0, 0);

        ////            CreateAsyncTest(grid, () => {
        ////                grid.UpdateLayout();
        ////                Assert.Equal(50, grid.ColumnDefinitions[0].ActualWidth);
        ////            });
        ////        }

        ////        [Fact]
        ////        [Asynchronous]
        ////        public void AutoCol_MinWidth()
        ////        {
        ////            var grid = new Grid();
        ////            grid.AddColumns(Auto, Star);
        ////            grid.ColumnDefinitions[0].MinWidth = 10;
        ////            grid.AddChild(DecoratorWithChild(), 0, 0, 0, 0);

        ////            CreateAsyncTest(grid, () => {
        ////                grid.UpdateLayout();
        ////                Assert.Equal(50, grid.ColumnDefinitions[0].ActualWidth);
        ////            });
        ////        }

        private static void CheckRowHeights(Grid grid, string message, params double[] heights)
        {
            for (int i = 0; i < grid.RowDefinitions.Count; i++)
                IsBetween(heights[i] - 0.55, heights[i] + 0.55, grid.RowDefinitions[i].ActualHeight);
        }

        MyContentControl DecoratorWithChild()
        {
            return DecoratorWithChild(50, 50);
        }

        MyContentControl DecoratorWithChild(int width, int height)
        {
            return new MyContentControl
            {
                Child = new Rectangle
                {
                    Width = width,
                    Height = height,
                    Fill = new SolidColorBrush(Colors.Red)
                }
            };
        }

        Control CreateInfiniteChild()
        {
            // Creates a child (ScrollViewer) which will consume as much space as is available to it
            // and does *not* have an explicit width/height set on it.
            return new MyContentControl
            {
                Child = new ScrollViewer
                {
                    Content = new Rectangle
                    {
                        Width = 300,
                        Height = 300,
                    }
                }
            };
        }

        class PanelPoker : Panel
        {
            public List<Size> ArrangeArgs = new List<Size>();
            public List<Size> ArrangeResults = new List<Size>();

            public List<Size> MeasureArgs = new List<Size>();
            public List<Size> MeasureResults = new List<Size>();

            public MyGrid Grid { get; set; }

            public PanelPoker()
            {
            }

            protected override Size ArrangeOverride(Size finalSize)
            {
                ArrangeArgs.Add(finalSize);
                Grid.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
                ArrangeResults.Add(Grid.Bounds.Size);
                return ArrangeResults.Last();
            }

            protected override Size MeasureOverride(Size availableSize)
            {
                MeasureArgs.Add(availableSize);
                Grid.Measure(availableSize);
                MeasureResults.Add(Grid.DesiredSize);
                return MeasureResults.Last();
            }
        }

        class SettablePanel : Panel
        {
            public Size? ArrangeArg { get; set; }
            public Size? MeasureArg { get; set; }
            public Grid Grid { get; set; }

            protected override Size ArrangeOverride(Size finalSize)
            {
                if (ArrangeArg.HasValue)
                    Grid.Arrange(new Rect(0, 0, ArrangeArg.Value.Width, ArrangeArg.Value.Height));
                else
                    Grid.Arrange(new Rect(0, 0, Grid.DesiredSize.Width, Grid.DesiredSize.Height));
                return Grid.DesiredSize;
            }

            protected override Size MeasureOverride(Size availableSize)
            {
                if (MeasureArg.HasValue)
                    Grid.Measure(MeasureArg.Value);
                else
                    Grid.Measure(availableSize);
                return Grid.DesiredSize;
            }
        }
    }
}
