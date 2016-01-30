// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Perspex.Controls.Presenters;
using Perspex.Controls.Shapes;
using Perspex.Controls.Templates;
using Perspex.Markup.Xaml.Templates;
using Perspex.Media;
using Xunit;

namespace Perspex.Controls.UnitTests
{
    public class GridTests
    {
        [Fact]
        public void ConstraintsNotUsedInMeasureOverride()
        {
            Rectangle r = new Rectangle { Width = 50, Height = 50 };
            MyContentControl c = new MyContentControl
            {
                Width = 80,
                Height = 80,
                Child = r
            };

            c.ApplyTemplate();
            c.Measure(new Size(100, 100));
            Assert.Equal(new Size(80, 80), c.MeasureOverrideArg);
            Assert.Equal(new Size(50, 50), c.MeasureOverrideResult);

            Assert.Equal(new Size(50, 50), r.DesiredSize);
            Assert.Equal(new Size(80, 80), c.DesiredSize);
        }

        [Fact]
        public void Defaults()
        {
            Grid g = new Grid();
            Assert.Equal(0, g.GetValue(Grid.ColumnProperty));
            Assert.Equal(1, g.GetValue(Grid.ColumnSpanProperty));
            Assert.Equal(0, g.GetValue(Grid.RowProperty));
            Assert.Equal(1, g.GetValue(Grid.RowSpanProperty));

            Rectangle r1 = new Rectangle();
            Rectangle r2 = new Rectangle();
            g.Children.Add(r1);
            g.Children.Add(r2);

            Assert.Equal(0, Grid.GetColumn(r1));
            Assert.Equal(0, Grid.GetColumn(r2));
            Assert.Equal(0, Grid.GetRow(r1));
            Assert.Equal(0, Grid.GetRow(r2));
        }

        [Fact]
        public void InvalidValues()
        {
            Grid g = new Grid();
            Rectangle r1 = new Rectangle();
            Rectangle r2 = new Rectangle();

            g.Children.Add(r1);
            g.Children.Add(r2);

            Assert.Throws<ArgumentException>(() => r1.SetValue(Grid.ColumnProperty, -1));
            Assert.Throws<ArgumentException>(() => Grid.SetColumn(r1, -1));
            Assert.Throws<ArgumentException>(() => Grid.SetColumnSpan(r1, 0));
            Assert.Throws<ArgumentException>(() => Grid.SetColumnSpan(r1, -1));
            Assert.Throws<ArgumentException>(() => Grid.SetRow(r1, -1));
            Assert.Throws<ArgumentException>(() => Grid.SetRowSpan(r1, 0));
            Assert.Throws<ArgumentException>(() => Grid.SetRowSpan(r1, -1));
        }

        [Fact]
        public void ComputeActualWidth()
        {
            var c = new Grid();

            Assert.Equal(new Size(0, 0), c.DesiredSize);
            Assert.Equal(new Size(0, 0), new Size(c.Bounds.Width, c.Bounds.Height));

            c.MaxWidth = 25;
            c.Width = 50;
            c.MinHeight = 33;

            Assert.Equal(new Size(0, 0), c.DesiredSize);
            Assert.Equal(new Size(0, 0), new Size(c.Bounds.Width, c.Bounds.Height));

            c.Measure(new Size(100, 100));

            Assert.Equal(new Size(25, 33), c.DesiredSize);
            Assert.Equal(new Size(0, 0), new Size(c.Bounds.Width, c.Bounds.Height));
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

        [Fact]
        public void ChildMargin_constWidth_constHeight_singleCell()
        {
            MyGrid g = new MyGrid();

            RowDefinition rdef;
            ColumnDefinition cdef;

            rdef = new RowDefinition();
            rdef.Height = new GridLength(200);
            g.RowDefinitions.Add(rdef);

            cdef = new ColumnDefinition();
            cdef.Width = new GridLength(200);
            g.ColumnDefinitions.Add(cdef);

            g.Margin = new Thickness(5);

            Canvas c = new Canvas();

            Grid.SetRow(c, 0);
            Grid.SetColumn(c, 0);

            g.Children.Add(new MyContentControl { Child = c });

            // first test with the child sized larger than the row/column definitions
            c.Width = 400;
            c.Height = 400;

            g.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            g.CheckMeasureArgs("#MeasureOverrideArgs", new Size(200, 200));
            g.Reset();
            Assert.Equal(new Size(200, 200), c.DesiredSize);
            Assert.Equal(new Size(210, 210), g.DesiredSize);

            g.Measure(new Size(100, 100));
            g.CheckMeasureArgs("#MeasureOverrideArgs 2"); // MeasureOverride shouldn't be called.
            g.Reset();
            Assert.Equal(new Size(100, 100), g.DesiredSize);

            // now test with the child sized smaller than the row/column definitions
            c.Width = 100;
            c.Height = 100;

            g.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            g.CheckMeasureArgs("#MeasureOverrideArgs 3", new Size(200, 200));
            g.Reset();
            Assert.Equal(new Size(210, 210), g.DesiredSize);

            g.Measure(new Size(100, 100));
            g.CheckMeasureArgs("#MeasureOverrideArgs 4"); // MeasureOverride won't be called.
            g.Reset();
            Assert.Equal(new Size(100, 100), g.DesiredSize);
        }

        [Fact]
        public void ChildMargin_starWidth_starHeight_singleCell()
        {
            MyGrid g = new MyGrid();

            RowDefinition rdef;
            ColumnDefinition cdef;

            rdef = new RowDefinition();
            rdef.Height = new GridLength(1, GridUnitType.Star);
            g.RowDefinitions.Add(rdef);

            cdef = new ColumnDefinition();
            cdef.Width = new GridLength(1, GridUnitType.Star);
            g.ColumnDefinitions.Add(cdef);

            g.Margin = new Thickness(5);

            Canvas c = new Canvas();

            Grid.SetRow(c, 0);
            Grid.SetColumn(c, 0);

            g.Children.Add(new MyContentControl { Child = c });

            // first test with the child sized larger than the row/column definitions
            c.Width = 400;
            c.Height = 400;

            g.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            g.CheckMeasureArgs("#MeasureOverrideArg", Size.Infinity);
            g.Reset();
            Assert.Equal(new Size(410, 410), g.DesiredSize);

            g.Measure(new Size(100, 100));
            g.CheckMeasureArgs("#MeasureOverrideArg 2", new Size(90, 90));
            g.Reset();
            Assert.Equal(new Size(100, 100), g.DesiredSize);

            // now test with the child sized smaller than the row/column definitions
            c.Width = 100;
            c.Height = 100;

            g.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            g.CheckMeasureArgs("#MeasureOverrideArg 3", Size.Infinity);
            g.Reset();
            Assert.Equal(new Size(110, 110), g.DesiredSize);

            g.Measure(new Size(100, 100));
            g.CheckMeasureArgs("#MeasureOverrideArg 4", new Size(90, 90));
            g.Reset();
            Assert.Equal(new Size(100, 100), g.DesiredSize);
        }

        [Fact]
        public void ChildMargin_autoWidth_autoHeight_singleCell()
        {
            MyGrid g = new MyGrid();

            RowDefinition rdef;
            ColumnDefinition cdef;

            rdef = new RowDefinition();
            rdef.Height = GridLength.Auto;
            g.RowDefinitions.Add(rdef);

            cdef = new ColumnDefinition();
            cdef.Width = GridLength.Auto;
            g.ColumnDefinitions.Add(cdef);

            g.Margin = new Thickness(5);

            Canvas c = new Canvas();

            Grid.SetRow(c, 0);
            Grid.SetColumn(c, 0);

            g.Children.Add(new MyContentControl { Child = c });

            // first test with the child sized larger than the row/column definitions
            c.Width = 400;
            c.Height = 400;

            g.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            g.CheckMeasureArgs("#MeasureOverrideArg", Size.Infinity);
            g.Reset();
            Assert.Equal(new Size(410, 410), g.DesiredSize);

            g.Measure(new Size(100, 100));
            g.CheckMeasureArgs("#MeasureOverrideArg 2"); // MeasureOverride is not called
            g.Reset();
            Assert.Equal(new Size(100, 100), g.DesiredSize);

            // now test with the child sized smaller than the row/column definitions
            c.Width = 100;
            c.Height = 100;

            g.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            g.CheckMeasureArgs("#MeasureOverrideArg 3", Size.Infinity);
            g.Reset();
            Assert.Equal(new Size(110, 110), g.DesiredSize);

            g.Measure(new Size(100, 100));
            g.CheckMeasureArgs("#MeasureOverrideArg 4"); // MeasureOverride is not called
            g.Reset();
            Assert.Equal(new Size(100, 100), g.DesiredSize);
        }

        // two children, two columns, one row.  the children
        // are both explicitly sized, but the column
        // definitions are 1* and 2* respectively.
        [Fact]
        public void TwoChildrenMargin_2Columns_1Star_and_2Star_1Row_constSize()
        {
            MyGrid g = new MyGrid();

            RowDefinition rdef;
            ColumnDefinition cdef;

            rdef = new RowDefinition();
            rdef.Height = new GridLength(200);
            g.RowDefinitions.Add(rdef);

            cdef = new ColumnDefinition();
            cdef.Width = new GridLength(1, GridUnitType.Star);
            g.ColumnDefinitions.Add(cdef);

            cdef = new ColumnDefinition();
            cdef.Width = new GridLength(2, GridUnitType.Star);
            g.ColumnDefinitions.Add(cdef);

            g.Margin = new Thickness(5);

            Canvas c;
            MyContentControl mc;
            c = new Canvas();
            c.Width = 400;
            c.Height = 400;
            mc = new MyContentControl { Child = c };
            Grid.SetRow(mc, 0);
            Grid.SetColumn(mc, 0);
            g.Children.Add(mc);

            c = new Canvas();
            c.Width = 400;
            c.Height = 400;
            mc = new MyContentControl { Child = c };
            Grid.SetRow(mc, 0);
            Grid.SetColumn(mc, 1);
            g.Children.Add(mc);

            g.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            g.CheckMeasureArgs("#MeasureOverrideArg", new Size(double.PositiveInfinity, 200), new Size(double.PositiveInfinity, 200));
            g.Reset();
            Assert.Equal(new Size(810, 210), g.DesiredSize);

            g.Measure(new Size(100, 100));
            g.CheckMeasureArgs("#MeasureOverrideArg 2", new Size(30, 200), new Size(60, 200));
            g.Reset();
            Assert.Equal(new Size(100, 100), g.DesiredSize);
        }

        // two children, two columns, one row.  the children
        // are both explicitly sized, but the column
        // definitions are 1* and 2* respectively.
        [Fact]
        public void Child_ColSpan2_2Columns_constSize_and_1Star_1Row_constSize()
        {
            MyGrid g = new MyGrid();

            RowDefinition rdef;
            ColumnDefinition cdef;

            rdef = new RowDefinition();
            rdef.Height = new GridLength(200);
            g.RowDefinitions.Add(rdef);

            cdef = new ColumnDefinition();
            cdef.Width = new GridLength(200);
            g.ColumnDefinitions.Add(cdef);

            cdef = new ColumnDefinition();
            cdef.Width = new GridLength(2, GridUnitType.Star);
            g.ColumnDefinitions.Add(cdef);

            g.Margin = new Thickness(5);

            Canvas c;
            MyContentControl mc;

            c = new Canvas();
            c.Width = 400;
            c.Height = 400;
            mc = new MyContentControl { Child = c };
            Grid.SetRow(mc, 0);
            Grid.SetColumn(mc, 0);
            Grid.SetColumnSpan(mc, 2);
            g.Children.Add(mc);

            g.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            g.CheckMeasureArgs("#MeasureOverrideArg", new Size(double.PositiveInfinity, 200));
            g.Reset();
            Assert.Equal(new Size(400, 200), c.DesiredSize);

            Assert.Equal(new Size(410, 210), g.DesiredSize);

            g.Measure(new Size(100, 100));
            g.CheckMeasureArgs("#MeasureOverrideArg 2", new Size(200, 200));
            g.Reset();
            Assert.Equal(new Size(100, 100), g.DesiredSize);
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
            MyGrid g = new MyGrid();

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
            MyContentControl mc;

            // child1
            child1 = new Canvas();
            child1.Width = 200;
            child1.Height = 200;
            mc = new MyContentControl { Child = child1 };
            Grid.SetRow(mc, 0);
            Grid.SetColumn(mc, 0);
            Grid.SetColumnSpan(mc, 2);
            g.Children.Add(mc);

            // child2
            child2 = new Canvas();
            child2.Width = 150;
            child2.Height = 200;
            mc = new MyContentControl { Child = child2 };
            Grid.SetRow(mc, 0);
            Grid.SetColumn(mc, 0);
            g.Children.Add(mc);

            // child3
            child3 = new Canvas();
            child3.Width = 200;
            child3.Height = 200;
            mc = new MyContentControl { Child = child3 };
            Grid.SetRow(mc, 0);
            Grid.SetColumn(mc, 0);
            g.Children.Add(mc);

            g.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            g.CheckMeasureArgs(
                "#MeasureOverrideArg", 
                new Size(double.PositiveInfinity, 200), 
                new Size(double.PositiveInfinity, 200), 
                new Size(double.PositiveInfinity, 200));
            g.Reset();
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
            MyGrid g = new MyGrid();

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
            MyContentControl mc = new MyContentControl { Child = r };
            Grid.SetRow(mc, 0);
            Grid.SetColumn(mc, 0);

            g.Children.Add(mc);

            g.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            g.CheckMeasureArgs("#MeasureOverrideArg", new Size(100, 50));
            g.Reset();
            Assert.Equal(new Size(0, 0), new Size(r.Bounds.Width, r.Bounds.Height));
            Assert.Equal(new Size(0, 0), new Size(g.Bounds.Width, g.Bounds.Height));

            g.Arrange(new Rect(0, 0, g.DesiredSize.Width, g.DesiredSize.Height));
            g.CheckRowHeights("#RowHeights", 50);
            Assert.Equal(new Size(0, 0), r.DesiredSize);
            Assert.Equal(new Size(110, 60), g.DesiredSize);

            Assert.Equal(new Rect(0, 0, 100, 50), r.Bounds);
            Assert.Equal(new Size(100, 50), new Size(r.Bounds.Width, r.Bounds.Height));
            Assert.Equal(new Size(100, 50), new Size(g.Bounds.Width, g.Bounds.Height));
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

            Assert.Equal(new Rect(0, 0, 100, 50), ra.Bounds);
            Assert.Equal(new Size(100, 50), new Size(ra.Bounds.Width, ra.Bounds.Height));
            Assert.Equal(new Size(20, 50), new Size(rb.Bounds.Width, rb.Bounds.Height));
            Assert.Equal(new Size(120, 50), new Size(g.Bounds.Width, g.Bounds.Height));
        }

        [Fact]
        public void ArrangeDefaultDefinitions()
        {
            MyGrid grid = new MyGrid();

            Border b = new Border();
            b.Background = new SolidColorBrush(Colors.Red);

            Border b2 = new Border();
            b2.Background = new SolidColorBrush(Colors.Green);
            b2.Width = b2.Height = 50;

            grid.Children.Add(new MyContentControl { Child = b });
            grid.Children.Add(new MyContentControl { Child = b2 });

            grid.Measure(Size.Infinity);
            grid.CheckMeasureArgs("#MeasureOverrideArg", Size.Infinity, Size.Infinity);
            grid.Reset();

            grid.Measure(new Size(400, 300));
            grid.CheckMeasureArgs("#MeasureOverrideArg 2", new Size(400, 300), new Size(400, 300));
            grid.Reset();

            grid.Width = 100;
            grid.Height = 100;

            grid.Measure(new Size(400, 300));
            grid.CheckMeasureArgs("#MeasureOverrideArg 3", new Size(100, 100), new Size(100, 100));
            grid.Reset();
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

        [Fact]
        public void StaticMethods_Null()
        {
            Assert.Throws<NullReferenceException>(delegate {
                Grid.GetColumn(null);
            });
            Assert.Throws<NullReferenceException>(delegate {
                Grid.GetColumnSpan(null);
            });
            Assert.Throws<NullReferenceException>(delegate {
                Grid.GetRow(null);
            });
            Assert.Throws<NullReferenceException>(delegate {
                Grid.GetRowSpan(null);
            });

            Assert.Throws<NullReferenceException>(delegate {
                Grid.SetColumn(null, 0);
            });
            Assert.Throws<NullReferenceException>(delegate {
                Grid.SetColumnSpan(null, 0);
            });
            Assert.Throws<NullReferenceException>(delegate {
                Grid.SetRow(null, 0);
            });
            Assert.Throws<NullReferenceException>(delegate {
                Grid.SetRowSpan(null, 0);
            });
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

        protected static void IsBetween(double min, double max, double actual)
        {
            if (actual > max || actual < min)
                throw new Exception(string.Format("Actual value '{0}' is not between '{1}' and '{2}'). ", actual, min, max));
        }

        protected class LayoutPoker : Panel
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
                //Tester.WriteLine(string.Format("Panel available size is {0}", availableSize));
                return MeasureResult;
            }

            protected override Size ArrangeOverride(Size finalSize)
            {
                ArrangeArg = finalSize;
                ArrangeResult = ArrangeFunc != null ? ArrangeFunc() : ArrangeResult;
                //Tester.WriteLine(string.Format("Panel final size is {0}", finalSize));
                return ArrangeResult;
            }
        }

        protected class MyContentControl : ContentControl
        {
            public bool IsArranged { get; private set; }
            public bool IsMeasured { get; private set; }
            public Action<Size> ArrangeHook = delegate { };
            public Action<Size> MeasureHook = delegate { };
            public Size MeasureOverrideArg;
            public Size ArrangeOverrideArg;
            public Size MeasureOverrideResult;
            public Size ArrangeOverrideResult;

            public MyContentControl()
            {
                Template = CreateTemplate();
            }

            public MyContentControl(int width, int height)
            {
                Content = new Rectangle { Width = width, Height = height, Fill = new SolidColorBrush(Colors.Green) };
                Template = CreateTemplate();
            }

            public Control Child
            {
                get { return (Control)Content;  }
                set { Content = value; }
            }

            protected override Size ArrangeOverride(Size finalSize)
            {
                IsArranged = true;
                MyGrid grid = Parent as MyGrid;
                if (grid != null)
                    grid.ArrangedElements.Add(new KeyValuePair<MyContentControl, Size>(this, finalSize));

                ArrangeOverrideArg = finalSize;
                if (ArrangeHook != null)
                    ArrangeHook(finalSize);

                ArrangeOverrideResult = base.ArrangeOverride(finalSize);

                if (grid != null)
                    grid.ArrangeResultElements.Add(new KeyValuePair<MyContentControl, Size>(this, ArrangeOverrideResult));

                return ArrangeOverrideResult;
            }

            protected override Size MeasureOverride(Size availableSize)
            {
                ((ContentPresenter)this.Presenter).UpdateChild();

                IsMeasured = true;
                MyGrid grid = Parent as MyGrid;
                if (grid != null)
                    grid.MeasuredElements.Add(new KeyValuePair<MyContentControl, Size>(this, availableSize));

                MeasureOverrideArg = availableSize;
                if (MeasureHook != null)
                    MeasureHook(availableSize);

                MeasureOverrideResult = base.MeasureOverride(availableSize);

                if (grid != null)
                    ((MyGrid)Parent).MeasureResultElements.Add(new KeyValuePair<MyContentControl, Size>(this, MeasureOverrideResult));

                return MeasureOverrideResult;
            }

            private IControlTemplate CreateTemplate()
            {
                return new FuncControlTemplate<MyContentControl>(parent =>
                {
                    return new ContentPresenter
                    {
                        Name = "PART_ContentPresenter",
                        [!ContentPresenter.ContentProperty] = parent[!MyContentControl.ContentProperty]
                    };
                });
            }
        }

        protected class MyGrid : Grid
        {
            public List<KeyValuePair<MyContentControl, Size>> ArrangedElements = new List<KeyValuePair<MyContentControl, Size>>();
            public List<KeyValuePair<MyContentControl, Size>> ArrangeResultElements = new List<KeyValuePair<MyContentControl, Size>>();
            public List<KeyValuePair<MyContentControl, Size>> MeasuredElements = new List<KeyValuePair<MyContentControl, Size>>();
            public List<KeyValuePair<MyContentControl, Size>> MeasureResultElements = new List<KeyValuePair<MyContentControl, Size>>();

            public void AddChild(Control element, int row, int column, int rowspan, int columnspan)
            {
                if (row != -1)
                    Grid.SetRow(element, row);
                if (rowspan != 0)
                    Grid.SetRowSpan(element, rowspan);
                if (column != -1)
                    Grid.SetColumn(element, column);
                if (columnspan != 0)
                    Grid.SetColumnSpan(element, columnspan);
                Children.Add(element);
            }

            public void CheckMeasureArgs(string message, params Size[] sizes)
            {
                Assert.Equal(sizes.Length, MeasuredElements.Count);

                for (int i = 0; i < MeasuredElements.Count; i++)
                {
                    try
                    {
                        IsBetween(sizes[i].Height - 0.55, sizes[i].Height + 0.55, MeasuredElements[i].Value.Height);
                        IsBetween(sizes[i].Width - 0.55, sizes[i].Width + 0.55, MeasuredElements[i].Value.Width);
                    }
                    catch
                    {
                        throw new Exception(string.Format(
                            "{2}.{3} Expected measure argument to be {0} but was {1}", 
                            sizes[i], 
                            MeasuredElements[i].Value, 
                            message, 
                            i));
                    }
                }
            }

            public void CheckMeasureResult(string message, params Size[] sizes)
            {
                for (int i = 0; i < Children.Count; i++)
                {
                    var poker = (MyContentControl)Children[i];
                    var result = poker.MeasureOverrideResult;
                    if (!result.Equals(sizes[i]))
                    {
                        throw new Exception(string.Format(
                            "{2}.{3} Expected measure result to be {0} but was {1}",
                            sizes[i],
                            result,
                            message,
                            i));

                    }
                }
            }

            public void CheckMeasureOrder(string message, params int[] childIndexes)
            {
                var measured = MeasuredElements.Select(d => d.Key).ToArray();
                for (int i = 0; i < childIndexes.Length; i++)
                {
                    Assert.Same(Children[childIndexes[i]], measured[i]);
                }
            }

            public void CheckMeasureSizes(string message, params Size[] sizes)
            {
                for (int i = 0; i < Children.Count; i++)
                {
                    var poker = (MyContentControl)Children[i];
                    var arg = poker.MeasureOverrideArg;
                    if (!arg.Equals(sizes[i]))
                    {
                        throw new Exception(string.Format(
                            "{2}.{3} Expected measure argument to be {0} but was {1}",
                            sizes[i],
                            arg,
                            message,
                            i));
                    }
                }
            }

            public void CheckFinalMeasureArg(string message, params Size[] sizes)
            {
                Assert.Equal(sizes.Length, Children.Count);
                for (int i = 0; i < sizes.Length; i++)
                    Assert.Equal(sizes[i], ((MyContentControl)Children[i]).MeasureOverrideArg);
            }

            public void CheckArrangeArgs(string message, params Size[] sizes)
            {
                Assert.Equal(sizes.Length, ArrangedElements.Count);

                for (int i = 0; i < ArrangedElements.Count; i++)
                {
                    try
                    {
                        IsBetween(sizes[i].Height - 0.55, sizes[i].Height + 0.55, ArrangedElements[i].Value.Height);
                        IsBetween(sizes[i].Width - 0.55, sizes[i].Width + 0.55, ArrangedElements[i].Value.Width);
                    }
                    catch
                    {
                        throw new Exception(string.Format(
                            "{2}.{3} Expected arrange argument to be {0} but was {1}",
                            sizes[i],
                            ArrangedElements[i].Value,
                            message,
                            i));
                    }
                }
            }

            public void CheckArrangeResult(string message, params Size[] sizes)
            {
                Assert.Equal(sizes.Length, ArrangeResultElements.Count);

                for (int i = 0; i < ArrangeResultElements.Count; i++)
                {
                    try
                    {
                        IsBetween(sizes[i].Height - 0.55, sizes[i].Height + 0.55, ArrangeResultElements[i].Value.Height);
                        IsBetween(sizes[i].Width - 0.55, sizes[i].Width + 0.55, ArrangeResultElements[i].Value.Width);
                    }
                    catch
                    {
                        throw new Exception(string.Format(
                            "{2}.{3} Expected arrange result to be {0} but was {1}",
                            sizes[i],
                            ArrangeResultElements[i].Value,
                            message,
                            i));
                    }
                }
            }

            public void CheckColWidths(string message, params double[] widths)
            {
                for (int i = 0; i < ColumnDefinitions.Count; i++)
                    IsBetween(widths[i] - 0.55, widths[i] + 0.55, ColumnDefinitions[i].ActualWidth);
            }

            public void CheckRowHeights(string message, params double[] heights)
            {
                for (int i = 0; i < RowDefinitions.Count; i++)
                    IsBetween(heights[i] - 0.55, heights[i] + 0.55, RowDefinitions[i].ActualHeight);
            }

            public void CheckDesired(string message, params Size[] sizes)
            {
                for (int i = 0; i < Children.Count; i++)
                {
                    var poker = (MyContentControl)Children[i];
                    if (!poker.DesiredSize.Equals(sizes[i]))
                        throw new Exception(string.Format(
                            "{2}.{3} Expected measure result to be {0} but was {1}",
                            sizes[i],
                            poker.DesiredSize,
                            message,
                            i));
                }
            }

            public void ChangeCol(int childIndex, int newRow)
            {
                Grid.SetColumn((Control)Children[childIndex], newRow);
            }

            public void ChangeRow(int childIndex, int newRow)
            {
                Grid.SetRow((Control)Children[childIndex], newRow);
            }

            public void ChangeColSpan(int childIndex, int newRow)
            {
                Grid.SetColumnSpan((Control)Children[childIndex], newRow);
            }

            public void ChangeRowSpan(int childIndex, int newRow)
            {
                Grid.SetRowSpan((Control)Children[childIndex], newRow);
            }

            public void Reset()
            {
                ArrangedElements.Clear();
                ArrangeResultElements.Clear();
                MeasuredElements.Clear();
                MeasureResultElements.Clear();
            }

            public void ReverseChildren()
            {
                List<IControl> children = new List<IControl>(Children);
                children.Reverse();
                Children.Clear();
                children.ForEach(Children.Add);
            }
        }
    }
}
