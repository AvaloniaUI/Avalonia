using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Xunit;

namespace Avalonia.Skia.RenderTests
{
    public class StreamGeometryTests : TestBase
    {
        public StreamGeometryTests()
            : base(@"Media\StreamGeometry")
        {
        }
         
        [Fact]
        public async Task PreciseEllipticArc_Produces_Valid_Arcs_In_All_Directions()
        {
            var grid = new Avalonia.Controls.Primitives.UniformGrid() { Columns = 2, Rows = 4, Width = 320, Height = 400 };
            foreach (var sweepDirection in new[] { SweepDirection.Clockwise, SweepDirection.CounterClockwise })
                foreach (var isLargeArc in new[] { false, true })
                    foreach (var isPrecise in new[] { false, true })
                    {
                        Point Pt(double x, double y) => new Point(x, y);
                        Size Sz(double w, double h) => new Size(w, h);
                        var streamGeometry = new StreamGeometry();
                        using (var context = streamGeometry.Open())
                        {
                            context.BeginFigure(Pt(20, 20), true);

                            if(isPrecise)
                                context.PreciseArcTo(Pt(40, 40), Sz(20, 20), 0, isLargeArc, sweepDirection);
                            else
                                context.ArcTo(Pt(40, 40), Sz(20, 20), 0, isLargeArc, sweepDirection);
                            context.LineTo(Pt(40, 20));
                            context.LineTo(Pt(20, 20));
                            context.EndFigure(true);
                        }
                        var pathShape = new Avalonia.Controls.Shapes.Path();
                        pathShape.Data = streamGeometry;
                        pathShape.Stroke = new SolidColorBrush(Colors.CornflowerBlue);
                        pathShape.Fill = new SolidColorBrush(Colors.Gold);
                        pathShape.StrokeThickness = 2;
                        pathShape.Margin = new Thickness(20);
                        grid.Children.Add(pathShape);
                    }
            await RenderToFile(grid);
        }

        [Fact]
        public async Task Stretched_Path_With_Transformed_Geometry()
        {
            var geometry = new StreamGeometry();

            using (var context = geometry.Open())
            {
                context.BeginFigure(new Point(0, 0), true);
                context.LineTo(new Point(80, 0));
                context.LineTo(new Point(80, 40));
                context.LineTo(new Point(0, 40));
                context.EndFigure(true);
            }

            geometry.Transform = new RotateTransform(30);

            var target = new Path
            {
                Data = geometry,
                Fill = new SolidColorBrush(Colors.CornflowerBlue),
                Stroke = new SolidColorBrush(Colors.Black),
                StrokeThickness = 2,
                Stretch = Stretch.Uniform,
                Width = 200,
                Height = 200
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Transformed_Geometry_Can_Be_Reopened()
        {
            var geometry = new StreamGeometry { Transform = new TranslateTransform(60, 30) };

            using (var context = geometry.Open())
            {
                context.BeginFigure(new Point(20, 20), true);
                context.LineTo(new Point(120, 20));
                context.LineTo(new Point(70, 100));
                context.EndFigure(true);
            }

            var target = new Path
            {
                Data = geometry,
                Fill = new SolidColorBrush(Colors.Gold),
                Stroke = new SolidColorBrush(Colors.Black),
                StrokeThickness = 2,
                Width = 200,
                Height = 200
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task PathGeometry_Can_Be_Opened()
        {
            var geometry = new PathGeometry();

            using (var context = geometry.Open())
            {
                context.BeginFigure(new Point(20, 20), false);
                context.LineTo(new Point(100, 100));
                context.LineTo(new Point(180, 20));
                context.EndFigure(false);
            }

            var target = new Path
            {
                Data = geometry,
                Stroke = new SolidColorBrush(Colors.Crimson),
                StrokeThickness = 6,
                Width = 200,
                Height = 200
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Stretched_Path_With_PathGeometry_Data()
        {
            var target = new Path
            {
                Data = PathGeometry.Parse("M 3,2 L 17,2 L 17,7 L 10,18 L 3,7 Z"),
                Fill = new SolidColorBrush(Colors.MediumSeaGreen),
                Stretch = Stretch.Uniform,
                Width = 200,
                Height = 200
            };

            await RenderToFile(target);
            CompareImages();
        }
    }
}
