// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests.Shapes
#endif
{
    using System.Threading.Tasks;
    using Avalonia.Collections;

    public class PathTests : TestBase
    {
        public PathTests()
            : base(@"Shapes\Path")
        {
        }


        [Fact]
        public async Task Line_Absolute()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Path
                {
                    Stroke = Brushes.Red,
                    StrokeThickness = 1,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Data = StreamGeometry.Parse("M 10,190 L 190,10 M0,0M200,200"),
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
        
        [Fact]
        public async Task Line_Relative()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Path
                {
                    Stroke = Brushes.Red,
                    StrokeThickness = 1,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Data = StreamGeometry.Parse("M10,190 l190,-190 M0,0M200,200"),
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
        
        [Fact]
        public async Task HorizontalLine_Absolute()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Path
                {
                    Stroke = Brushes.Red,
                    StrokeThickness = 1,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Data = StreamGeometry.Parse("M190,100 H10 M0,0M200,200"),
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
        
        [Fact]
        public async Task HorizontalLine_Relative()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Path
                {
                    Stroke = Brushes.Red,
                    StrokeThickness = 1,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Data = StreamGeometry.Parse("M190,100 h-180 M0,0M200,200"),
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
        
        [Fact]
        public async Task VerticalLine_Absolute()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Path
                {
                    Stroke = Brushes.Red,
                    StrokeThickness = 1,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Data = StreamGeometry.Parse("M100,190 V10 M0,0M200,200"),
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
        
        [Fact]
        public async Task VerticalLine_Relative()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Path
                {
                    Stroke = Brushes.Red,
                    StrokeThickness = 1,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Data = StreamGeometry.Parse("M100,190 V-180 M0,0M200,200"),
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
        
        [Fact]
        public async Task CubicBezier_Absolute()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Path
                {
                    Fill = Brushes.Gray,
                    Stroke = Brushes.Red,
                    StrokeThickness = 1,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Data = StreamGeometry.Parse("M190,0 C10,10 190,190 10,190 M0,0M200,200"),
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
        
        [Fact]
        public async Task CubicBezier_Relative()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Path
                {
                    Fill = Brushes.Gray,
                    Stroke = Brushes.Red,
                    StrokeThickness = 1,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Data = StreamGeometry.Parse("M190,0 c-180,10 0,190 -180,190 M0,0M200,200"),
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
        
        [Fact]
        public async Task Arc_Absolute()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Path
                {
                    Fill = Brushes.Gray,
                    Stroke = Brushes.Red,
                    StrokeThickness = 1,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Data = StreamGeometry.Parse("M190,100 A90,90 0 1,0 10,100  M0,0M200,200"),
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
        
        [Fact]
        public async Task Arc_Relative()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Path
                {
                    Fill = Brushes.Gray,
                    Stroke = Brushes.Red,
                    StrokeThickness = 1,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Data = StreamGeometry.Parse("M190,100 a90,90 0 1,0 -180,0  M0,0M200,200"),
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Path_100px_Triangle_Centered()
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

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Path_Tick_Scaled()
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

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Path_Tick_Scaled_Stroke_8px()
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

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Path_Expander_With_Border()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    BorderBrush = Brushes.Red,
                    BorderThickness = new Thickness(1),
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

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Path_With_PenLineCap()
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
                    StrokeDashArray = new AvaloniaList<double>(3, 1),
                    StrokeLineCap = PenLineCap.Round,
                    Data = StreamGeometry.Parse("M 20,20 L 180,180"),
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Path_With_Rotated_Geometry()
        {
            var target = new Border
            {
                Width = 200,
                Height = 200,
                Background = Brushes.White,
                Child = new Path
                {
                    Fill = Brushes.Red,
                    Data = new RectangleGeometry
                    {
                        Rect = new Rect(50, 50, 100, 100),
                        Transform = new RotateTransform(45),
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
    }
}
