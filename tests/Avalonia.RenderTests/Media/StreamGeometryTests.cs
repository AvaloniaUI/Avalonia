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
        public void Clone_Should_Keep_Transform()
        {
            var geometry = StreamGeometry.Parse("M10,190 l190,-190");
            geometry.Transform = new TranslateTransform(50, 50);

            var clone = geometry.Clone();

            Assert.Equal(geometry.Transform.Value, clone.Transform!.Value);
            Assert.Equal(geometry.Bounds, clone.Bounds);
        }

        [Fact]
        public void Open_Should_Update_Transformed_Geometry()
        {
            var geometry = StreamGeometry.Parse("M10,190 l190,-190");
            geometry.Transform = new TranslateTransform(50, 50);
            var untransformed = StreamGeometry.Parse("M10,190 l190,-190");

            // Reading the bounds first makes the geometry cache its transformed copy.
            var before = geometry.Bounds;

            foreach (var target in new[] { geometry, untransformed })
            {
                using (var context = target.Open())
                {
                    context.BeginFigure(new Point(300, 300), true);
                    context.LineTo(new Point(400, 400));
                    context.EndFigure(false);
                }
            }

            Assert.NotEqual(before, geometry.Bounds);
            Assert.Equal(untransformed.Bounds.Translate(new Vector(50, 50)), geometry.Bounds);
        }
    }
}
