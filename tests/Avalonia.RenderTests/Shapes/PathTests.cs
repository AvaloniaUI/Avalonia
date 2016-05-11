// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Xunit;

#if AVALONIA_CAIRO
namespace Avalonia.Cairo.RenderTests.Shapes
#elif AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests.Shapes
#endif
{
    using Avalonia.Collections;

    public class PathTests : TestBase
    {
        public PathTests()
            : base(@"Shapes\Path")
        {
        }

        [Fact]
        public void Path_100px_Triangle_Centered()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Path
                {
                    Fill = Brushes.Gray,
                    Stroke = Brushes.Red,
                    StrokeThickness = 2,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Data = StreamGeometry.Parse("M 0,100 L 100,100 50,0 Z"),
                }
            };

            RenderToFile(target);
            CompareImages();
        }
#if AVALONIA_SKIA_SKIP_FAIL
        [Fact(Skip = "Waiting for https://github.com/mono/SkiaSharp/pull/63")]
#else
        [Fact]
#endif
        public void Path_Tick_Scaled()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Path
                {
                    Fill = Brushes.Gray,
                    Stroke = Brushes.Red,
                    StrokeThickness = 2,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Data = StreamGeometry.Parse("M 1145.607177734375,430 C1145.607177734375,430 1141.449951171875,435.0772705078125 1141.449951171875,435.0772705078125 1141.449951171875,435.0772705078125 1139.232177734375,433.0999755859375 1139.232177734375,433.0999755859375 1139.232177734375,433.0999755859375 1138,434.5538330078125 1138,434.5538330078125 1138,434.5538330078125 1141.482177734375,438 1141.482177734375,438 1141.482177734375,438 1141.96875,437.9375 1141.96875,437.9375 1141.96875,437.9375 1147,431.34619140625 1147,431.34619140625 1147,431.34619140625 1145.607177734375,430 1145.607177734375,430 z"),
                }
            };

            RenderToFile(target);
            CompareImages();
        }

#if AVALONIA_SKIA_SKIP_FAIL
        [Fact(Skip = "Waiting for https://github.com/mono/SkiaSharp/pull/63")]
#else
        [Fact]
#endif
        public void Path_Tick_Scaled_Stroke_8px()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Path
                {
                    Fill = Brushes.Gray,
                    Stroke = Brushes.Red,
                    StrokeThickness = 8,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Data = StreamGeometry.Parse("M 1145.607177734375,430 C1145.607177734375,430 1141.449951171875,435.0772705078125 1141.449951171875,435.0772705078125 1141.449951171875,435.0772705078125 1139.232177734375,433.0999755859375 1139.232177734375,433.0999755859375 1139.232177734375,433.0999755859375 1138,434.5538330078125 1138,434.5538330078125 1138,434.5538330078125 1141.482177734375,438 1141.482177734375,438 1141.482177734375,438 1141.96875,437.9375 1141.96875,437.9375 1141.96875,437.9375 1147,431.34619140625 1147,431.34619140625 1147,431.34619140625 1145.607177734375,430 1145.607177734375,430 z"),
                }
            };

            RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public void Path_Expander_With_Border()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    BorderBrush = Brushes.Red,
                    BorderThickness = 1,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new Path
                    {
                        Fill = Brushes.Black,
                        Stroke = Brushes.Black,
                        StrokeThickness = 1,
                        Stretch = Stretch.Uniform,
                        Data = StreamGeometry.Parse("M 0 2 L 4 6 L 0 10 Z"),
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
        }

#if AVALONIA_CAIRO
        [Fact(Skip = "Path with StrokeDashCap, StrokeStartLineCap, StrokeEndLineCap rendering is not implemented in Cairo yet")]
#elif AVALONIA_SKIA_SKIP_FAIL
        [Fact(Skip = "FIXME")]
#else
        [Fact]
#endif
        public void Path_With_PenLineCap()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Path
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 10,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    StrokeDashCap = PenLineCap.Triangle,
                    StrokeDashArray = new AvaloniaList<double>(3, 1),
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Square,
                    Data = StreamGeometry.Parse("M 20,20 L 180,180"),
                }
            };

            RenderToFile(target);
            CompareImages();
        }
    }
}
