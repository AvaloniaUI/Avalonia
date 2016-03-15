// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Diagnostics;
using Perspex.Controls;
using Perspex.Controls.Shapes;
using Perspex.Media;
using Xunit;

namespace Perspex.Controls.UnitTests
{
    public class GridTests
    {
      private class LayoutPoker : Panel
      {
         public Size MeasureResult = new Size(0, 0);
         public Size MeasureArg = new Size(0, 0);
         public Size ArrangeResult = new Size(0, 0);
         public Size ArrangeArg = new Size(0, 0);
         public Func<Size> ArrangeFunc;
         public Func<Size> MeasureFunc;

         protected override Size MeasureOverride(Size availableSize)
         {
            MeasureArg = availableSize;
            MeasureResult = MeasureFunc != null ? MeasureFunc() : MeasureResult;
            Debug.WriteLine($"Panel available size is {availableSize}");
            return MeasureResult;
         }

         protected override Size ArrangeOverride(Size finalSize)
         {
            ArrangeArg = finalSize;
            ArrangeResult = ArrangeFunc != null ? ArrangeFunc() : ArrangeResult;
            Debug.WriteLine($"Panel final size is {finalSize}");
            return ArrangeResult;
         }
      }


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
                Children = new Controls
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
      public void ComputeActualWidth()
      {
         var c = new Grid();

         Assert.Equal(new Size(0, 0), c.DesiredSize);
         Assert.Equal(new Size(0, 0), c.Bounds.Size);

         c.MaxWidth = 25;
         c.Width = 50;
         c.MinHeight = 33;

         Assert.Equal(new Size(0, 0), c.DesiredSize);
         Assert.Equal(new Size(0, 0), c.Bounds.Size);

         c.Measure(new Size(100, 100));

         Assert.Equal(new Size(25, 33), c.DesiredSize);
         Assert.Equal(new Size(0, 0), c.DesiredSize);
      }

      [Fact]
      public void ChildlessMeasureTest()
      {
         Grid g = new Grid();

         g.Measure(new Size(200, 200));

         Assert.Equal(new Size(0, 0), g.DesiredSize);
      }

      [Fact]
      public void ChildlessWidthHeightMeasureTest()
      {
         Grid g = new Grid();

         g.Width = 300;
         g.Height = 300;

         g.Measure(new Size(200, 200));

         Assert.Equal(new Size(200, 200), g.DesiredSize);
      }

      [Fact]
      public void ChildlessMarginTest()
      {
         Grid g = new Grid();

         g.Margin = new Thickness(5);

         g.Measure(new Size(200, 200));

         Assert.Equal(new Size(10, 10), g.DesiredSize);
      }

      [Fact]
      public void Childless_ColumnDefinition_Width_constSize_singleColumn()
      {
         Grid g = new Grid();

         ColumnDefinition def;

         def = new ColumnDefinition();
         def.Width = new GridLength(200);
         g.ColumnDefinitions.Add(def);

         g.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

         Assert.Equal(new Size(200, 0), g.DesiredSize);

         g.Measure(new Size(100, 100));

         Assert.Equal(new Size(100, 0), g.DesiredSize);
      }

      [Fact]
      public void ChildlessMargin_ColumnDefinition_Width_constSize_singleColumn()
      {
         Grid g = new Grid();

         ColumnDefinition def;

         def = new ColumnDefinition();
         def.Width = new GridLength(200);
         g.ColumnDefinitions.Add(def);

         g.Margin = new Thickness(5);

         g.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

         Assert.Equal(new Size(210, 10), g.DesiredSize);

         g.Measure(new Size(100, 100));

         Assert.Equal(new Size(100, 10), g.DesiredSize);
      }

      [Fact]
      public void ChildlessMargin_ColumnDefinition_Width_constSize_multiColumn()
      {
         Grid g = new Grid();

         ColumnDefinition def;

         def = new ColumnDefinition();
         def.Width = new GridLength(200);
         g.ColumnDefinitions.Add(def);

         def = new ColumnDefinition();
         def.Width = new GridLength(200);
         g.ColumnDefinitions.Add(def);

         g.Margin = new Thickness(5);

         g.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

         Assert.Equal(new Size(410, 10), g.DesiredSize);

         g.Measure(new Size(100, 100));

         Assert.Equal(new Size(100, 10), g.DesiredSize);
      }

      [Fact]
      public void ChildlessMargin_ColumnDefinition_Width_autoSize_singleColumn()
      {
         Grid g = new Grid();

         ColumnDefinition def;

         def = new ColumnDefinition();
         def.Width = GridLength.Auto;
         g.ColumnDefinitions.Add(def);

         g.Margin = new Thickness(5);

         g.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

         Assert.Equal(new Size(10, 10), g.DesiredSize);

         g.Measure(new Size(100, 100));

         Assert.Equal(new Size(10, 10), g.DesiredSize);
      }

      [Fact]
      public void ChildlessMargin_ColumnDefinition_Width_autoSize_constSize_multiColumn()
      {
         Grid g = new Grid();

         ColumnDefinition def;

         def = new ColumnDefinition();
         def.Width = GridLength.Auto;
         g.ColumnDefinitions.Add(def);

         def = new ColumnDefinition();
         def.Width = new GridLength(200);
         g.ColumnDefinitions.Add(def);

         g.Margin = new Thickness(5);

         g.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

         Assert.Equal(new Size(210, 10), g.DesiredSize);

         g.Measure(new Size(100, 100));

         Assert.Equal(new Size(100, 10), g.DesiredSize);
      }


      [Fact]
      public void ChildlessMargin_ColumnDefinition_Width_starSize_singleColumn()
      {
         Grid g = new Grid();

         ColumnDefinition def;

         def = new ColumnDefinition();
         def.Width = new GridLength(2, GridUnitType.Star);
         g.ColumnDefinitions.Add(def);

         g.Margin = new Thickness(5);

         g.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

         Assert.Equal(new Size(10, 10), g.DesiredSize);

         g.Measure(new Size(100, 100));

         Assert.Equal(new Size(10, 10), g.DesiredSize);
      }

      [Fact]
      public void ChildlessMargin_ColumnDefinition_Width_starSize_constSize_multiColumn()
      {
         Grid g = new Grid();

         ColumnDefinition def;

         def = new ColumnDefinition();
         def.Width = new GridLength(2, GridUnitType.Star);
         g.ColumnDefinitions.Add(def);

         def = new ColumnDefinition();
         def.Width = new GridLength(200);
         g.ColumnDefinitions.Add(def);

         g.Margin = new Thickness(5);

         g.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

         Assert.Equal(new Size(210, 10), g.DesiredSize);

         g.Measure(new Size(100, 100));

         Assert.Equal(new Size(100, 10), g.DesiredSize);
      }

      [Fact]
      public void ChildlessMargin_RowDefinition_Height_constSize_singleRow()
      {
         Grid g = new Grid();

         RowDefinition def;

         def = new RowDefinition();
         def.Height = new GridLength(200);
         g.RowDefinitions.Add(def);

         g.Margin = new Thickness(5);

         g.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

         Assert.Equal(new Size(10, 210), g.DesiredSize);

         g.Measure(new Size(100, 100));

         Assert.Equal(new Size(10, 100), g.DesiredSize);
      }

      [Fact]
      public void ChildlessMargin_RowDefinition_Height_constSize_multiRow()
      {
         Grid g = new Grid();

         RowDefinition def;

         def = new RowDefinition();
         def.Height = new GridLength(200);
         g.RowDefinitions.Add(def);

         def = new RowDefinition();
         def.Height = new GridLength(200);
         g.RowDefinitions.Add(def);

         g.Margin = new Thickness(5);

         g.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

         Assert.Equal(new Size(10, 410), g.DesiredSize);

         g.Measure(new Size(100, 100));

         Assert.Equal(new Size(10, 100), g.DesiredSize);
      }

      [Fact]
      public void ChildlessMargin_RowDefinition_Height_autoSize_singleRow()
      {
         Grid g = new Grid();

         RowDefinition def;

         def = new RowDefinition();
         def.Height = GridLength.Auto;
         g.RowDefinitions.Add(def);

         g.Margin = new Thickness(5);

         g.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

         Assert.Equal(new Size(10, 10), g.DesiredSize);

         g.Measure(new Size(100, 100));

         Assert.Equal(new Size(10, 10), g.DesiredSize);
      }

      [Fact]
      public void ChildlessMargin_RowDefinition_Height_autoSize_constSize_multiRow()
      {
         Grid g = new Grid();

         RowDefinition def;

         def = new RowDefinition();
         def.Height = GridLength.Auto;
         g.RowDefinitions.Add(def);

         def = new RowDefinition();
         def.Height = new GridLength(200);
         g.RowDefinitions.Add(def);

         g.Margin = new Thickness(5);

         g.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

         Assert.Equal(new Size(10, 210), g.DesiredSize);

         g.Measure(new Size(100, 100));

         Assert.Equal(new Size(10, 100), g.DesiredSize);
      }

      [Fact]
      public void EmptyRowDefinitionsFixedMeasuredSize()
      {
         Grid grid = new Grid();
         grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(100, GridUnitType.Pixel)));
         grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(10, GridUnitType.Pixel)));
         grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(100, GridUnitType.Pixel)));

         GridSplitter splitter = new GridSplitter();
         splitter.Width = 10;
         splitter.Background = Brushes.Gray;
         grid.Children.Add(splitter);
         Grid.SetColumn(splitter, 1);

         Rectangle rect = new Rectangle();
         rect.Fill = Brushes.Red;
         grid.Children.Add(rect);
         Grid.SetColumn(rect, 2);

         grid.Measure(new Size(210, 100));
         grid.Arrange(new Rect(grid.DesiredSize));

         Assert.Equal(new Size(210, 100), grid.DesiredSize);
      }

      [Fact]
      public void EmptyColumnDefinitionsFixedMeasuredSize()
      {
         Grid grid = new Grid();
         grid.RowDefinitions.Add(new RowDefinition(new GridLength(100, GridUnitType.Pixel)));
         grid.RowDefinitions.Add(new RowDefinition(new GridLength(10, GridUnitType.Pixel)));
         grid.RowDefinitions.Add(new RowDefinition(new GridLength(100, GridUnitType.Pixel)));

         GridSplitter splitter = new GridSplitter();
         splitter.Width = 10;
         splitter.Background = Brushes.Gray;
         grid.Children.Add(splitter);
         Grid.SetColumn(splitter, 1);

         Rectangle rect = new Rectangle();
         rect.Fill = Brushes.Red;
         grid.Children.Add(rect);
         Grid.SetColumn(rect, 2);

         grid.Measure(new Size(100, 210));
         grid.Arrange(new Rect(grid.DesiredSize));

         Assert.Equal(new Size(100, 210), grid.DesiredSize);
      }


      [Fact]
      public void EmptyRowDefinitionsInfiniteMeasuredSize()
      {
         Grid grid = new Grid();
         grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(100, GridUnitType.Pixel)));
         grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(10, GridUnitType.Pixel)));
         grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(100, GridUnitType.Pixel)));

         GridSplitter splitter = new GridSplitter();
         splitter.Width = 10;
         splitter.Background = Brushes.Gray;
         grid.Children.Add(splitter);
         Grid.SetColumn(splitter, 1);

         Rectangle rect = new Rectangle();
         rect.Fill = Brushes.Red;
         grid.Children.Add(rect);
         Grid.SetColumn(rect, 2);

         grid.Measure(Size.Infinity);
         grid.Arrange(new Rect(grid.DesiredSize));

         Assert.Equal(new Size(210, 0), grid.DesiredSize);
      }

      [Fact]
      public void EmptyColumnDefinitionsInfiniteMeasuredSize()
      {
         Grid grid = new Grid();
         grid.RowDefinitions.Add(new RowDefinition(new GridLength(100, GridUnitType.Pixel)));
         grid.RowDefinitions.Add(new RowDefinition(new GridLength(10, GridUnitType.Pixel)));
         grid.RowDefinitions.Add(new RowDefinition(new GridLength(100, GridUnitType.Pixel)));

         GridSplitter splitter = new GridSplitter();
         splitter.Width = 10;
         splitter.Background = Brushes.Gray;
         grid.Children.Add(splitter);
         Grid.SetColumn(splitter, 1);

         Rectangle rect = new Rectangle();
         rect.Fill = Brushes.Red;
         grid.Children.Add(rect);
         Grid.SetColumn(rect, 2);

         grid.Measure(new Size(0, 210));
         grid.Arrange(new Rect(grid.DesiredSize));

         Assert.Equal(new Size(0, 210), grid.DesiredSize);
      }

      [Fact]
      public void ChildlessMargin_RowDefinition_Height_starSize_singleRow()
      {
         Grid g = new Grid();

         RowDefinition def;

         def = new RowDefinition();
         def.Height = new GridLength(2, GridUnitType.Star);
         g.RowDefinitions.Add(def);

         g.Margin = new Thickness(5);

         g.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

         Assert.Equal(new Size(10, 10), g.DesiredSize);

         g.Measure(new Size(100, 100));

         Assert.Equal(new Size(10, 10), g.DesiredSize);
      }

      [Fact]
      public void ChildlessMargin_RowDefinition_Height_starSize_constSize_multiRow()
      {
         Grid g = new Grid();

         RowDefinition def;

         def = new RowDefinition();
         def.Height = new GridLength(2, GridUnitType.Star);
         g.RowDefinitions.Add(def);

         def = new RowDefinition();
         def.Height = new GridLength(200);
         g.RowDefinitions.Add(def);

         g.Margin = new Thickness(5);

         g.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

         Assert.Equal(new Size(10, 210), g.DesiredSize);

         g.Measure(new Size(100, 100));

         Assert.Equal(new Size(10, 100), g.DesiredSize);
      }


      // 3 children, two columns, two rows.  the columns
      // are Auto sized, the rows are absolute (200 pixels
      // each).
      // 
      // +-------------------+
      // |                   |
      // |     child1        |
      // |                   |
      // +--------+----------+
      // |        |          |
      // | child2 |  child3  |
      // |        |          |
      // +--------+----------+
      //
      // child1 has colspan of 2
      // child2 and 3 are explicitly sized (width = 150 and 200, respectively)
      //
      [Fact]
      public void ComplexLayout1()
      {
         Grid g = new Grid();

         RowDefinition rdef;
         ColumnDefinition cdef;

         // Add rows
         rdef = new RowDefinition();
         rdef.Height = new GridLength(200);
         g.RowDefinitions.Add(rdef);

         rdef = new RowDefinition();
         rdef.Height = new GridLength(200);
         g.RowDefinitions.Add(rdef);

         cdef = new ColumnDefinition();
         cdef.Width = GridLength.Auto;
         g.ColumnDefinitions.Add(cdef);

         cdef = new ColumnDefinition();
         cdef.Width = GridLength.Auto;
         g.ColumnDefinitions.Add(cdef);

         Canvas child1, child2, child3;
         ContentControl mc;

         // child1
         child1 = new Canvas();
         child1.Width = 200;
         child1.Height = 200;
         mc = new ContentControl { Content = child1 };
         Grid.SetRow(mc, 0);
         Grid.SetColumn(mc, 0);
         Grid.SetColumnSpan(mc, 2);
         g.Children.Add(mc);

         // child2
         child2 = new Canvas();
         child2.Width = 150;
         child2.Height = 200;
         mc = new ContentControl { Content = child2 };
         Grid.SetRow(mc, 0);
         Grid.SetColumn(mc, 0);
         g.Children.Add(mc);

         // child3
         child3 = new Canvas();
         child3.Width = 200;
         child3.Height = 200;
         mc = new ContentControl { Content = child3 };
         Grid.SetRow(mc, 0);
         Grid.SetColumn(mc, 0);
         g.Children.Add(mc);

         g.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
         //g.CheckMeasureArgs("#MeasureOverrideArg", new Size(inf, 200), new Size(inf, 200), new Size(inf, 200));
         //g.Reset();
         Assert.Equal(new Size(200, 400), g.DesiredSize);
      }

      [Fact]
      public void ComplexLayout2()
      {
         Grid g = new Grid();

         RowDefinition rdef;
         ColumnDefinition cdef;

         rdef = new RowDefinition();
         rdef.Height = new GridLength(200);
         g.RowDefinitions.Add(rdef);

         cdef = new ColumnDefinition();
         cdef.Width = new GridLength(200);
         g.ColumnDefinitions.Add(cdef);

         g.Margin = new Thickness(5);

         LayoutPoker c = new LayoutPoker();

         Grid.SetRow(c, 0);
         Grid.SetColumn(c, 0);

         g.Children.Add(c);

         c.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

         // first test with the child sized larger than the row/column definitions
         c.Width = 400;
         c.Height = 400;

         g.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

         Assert.Equal(400, c.Width);
         Assert.Equal(400, c.Height);

         Assert.Equal(new Size(200, 200), c.DesiredSize);
         Assert.Equal(new Size(400, 400), c.MeasureArg);
         Assert.Equal(new Size(210, 210), g.DesiredSize);

         g.Measure(new Size(100, 100));

         Assert.Equal(new Size(100, 100), g.DesiredSize);
         Assert.Equal(new Size(400, 400), c.MeasureArg);
         Assert.Equal(new Size(200, 200), c.DesiredSize);

         // now test with the child sized smaller than the row/column definitions
         c.Width = 100;
         c.Height = 100;

         g.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

         Assert.Equal(new Size(100, 100), c.MeasureArg);
         Assert.Equal(new Size(210, 210), g.DesiredSize);
      }


      [Fact]
      public void ArrangeTest()
      {
         Grid g = new Grid();

         RowDefinition rdef;
         ColumnDefinition cdef;

         rdef = new RowDefinition();
         rdef.Height = new GridLength(50);
         g.RowDefinitions.Add(rdef);

         cdef = new ColumnDefinition();
         cdef.Width = new GridLength(100);
         g.ColumnDefinitions.Add(cdef);

         g.Margin = new Thickness(5);

         var r = new Border();
         ContentControl mc = new ContentControl { Content = r };
         Grid.SetRow(mc, 0);
         Grid.SetColumn(mc, 0);

         g.Children.Add(mc);

         g.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
         //g.CheckMeasureArgs("#MeasureOverrideArg", new Size(100, 50));
         //g.Reset();
         Assert.Equal(new Size(0, 0), r.Bounds.Size);
         Assert.Equal(new Size(0, 0), g.Bounds.Size);

         g.Arrange(new Rect(0, 0, g.DesiredSize.Width, g.DesiredSize.Height));
         //g.CheckRowHeights("#RowHeights", 50);
         Assert.Equal(new Size(0, 0), r.DesiredSize);
         Assert.Equal(new Size(110, 60), g.DesiredSize);

         //Assert.Equal(new Rect(0, 0, 100, 50).ToString(), LayoutInformation.GetLayoutSlot(r).ToString(), "slot");
         Assert.Equal(new Size(100, 50), r.Bounds.Size);
         Assert.Equal(new Size(100, 50), g.Bounds.Size);
      }

      [Fact]
      public void ArrangeTest_TwoChildren()
      {
         Grid g = new Grid();

         RowDefinition rdef;
         ColumnDefinition cdef;

         rdef = new RowDefinition();
         rdef.Height = new GridLength(50);
         g.RowDefinitions.Add(rdef);

         cdef = new ColumnDefinition();
         cdef.Width = new GridLength(100);
         g.ColumnDefinitions.Add(cdef);

         cdef = new ColumnDefinition();
         cdef.Width = new GridLength(20);
         g.ColumnDefinitions.Add(cdef);

         g.Margin = new Thickness(5);

         var ra = new Border();
         var rb = new Border();

         Grid.SetRow(ra, 0);
         Grid.SetColumn(ra, 0);

         Grid.SetRow(rb, 0);
         Grid.SetColumn(rb, 1);

         g.Children.Add(ra);
         g.Children.Add(rb);

         g.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

         Assert.Equal(new Size(0, 0), ra.DesiredSize);
         Assert.Equal(new Size(0, 0), rb.DesiredSize);
         Assert.Equal(new Size(130, 60), g.DesiredSize);

         g.Arrange(new Rect(0, 0, g.DesiredSize.Width, g.DesiredSize.Height));

         Assert.Equal(new Size(0, 0), ra.DesiredSize);
         Assert.Equal(new Size(130, 60), g.DesiredSize);

         Assert.Equal(new Size(100, 50),ra.Bounds.Size);
         Assert.Equal(new Size(20, 50), rb.Bounds.Size);
         Assert.Equal(new Size(120, 50), g.Bounds.Size);
      }

      [Fact]
      public void ArrangeDefaultDefinitions()
      {
         Grid grid = new Grid();

         Border b = new Border();
         b.Background = Brushes.Red;

         Border b2 = new Border();
         b2.Background = Brushes.Green;
         b2.Width = b2.Height = 50;

         grid.Children.Add(new ContentControl { Content = b });
         grid.Children.Add(new ContentControl { Content = b2 });

         //grid.Measure(new Size(inf, inf));
         //grid.CheckMeasureArgs("#MeasureOverrideArg", new Size(inf, inf), new Size(inf, inf));
         //grid.Reset();

         grid.Measure(new Size(400, 300));
         //grid.CheckMeasureArgs("#MeasureOverrideArg 2", new Size(400, 300), new Size(400, 300));
         //grid.Reset();

         grid.Width = 100;
         grid.Height = 100;

         grid.Measure(new Size(400, 300));
         //grid.CheckMeasureArgs("#MeasureOverrideArg 3", new Size(100, 100), new Size(100, 100));
         //grid.Reset();
         grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));

         Assert.Equal(new Size(100, 100), grid.Bounds.Size);
         Assert.Equal(new Size(100, 100), b.Bounds.Size);
         Assert.Equal(new Size(50, 50), b2.Bounds.Size);
      }

      [Fact]
      public void DefaultDefinitions()
      {
         Grid grid = new Grid();

         grid.Children.Add(new Border());

         Assert.True(grid.ColumnDefinitions != null);
         Assert.True(grid.RowDefinitions != null);
         Assert.Equal(0, grid.ColumnDefinitions.Count);
         Assert.Equal(0, grid.RowDefinitions.Count);

         grid.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

         Assert.True(grid.ColumnDefinitions != null);
         Assert.True(grid.RowDefinitions != null);
         Assert.Equal(0, grid.ColumnDefinitions.Count);
         Assert.Equal(0, grid.RowDefinitions.Count);

         grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));

         Assert.True(grid.ColumnDefinitions != null);
         Assert.True(grid.RowDefinitions != null);
         Assert.Equal(0, grid.ColumnDefinitions.Count);
         Assert.Equal(0, grid.RowDefinitions.Count);
      }
   }
}
