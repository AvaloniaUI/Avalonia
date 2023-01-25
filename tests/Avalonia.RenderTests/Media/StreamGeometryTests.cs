using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests.Media
#endif
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
    }
}
