#nullable enable
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests.Media
#endif
{
    public class RelativePointTestPrimitivesHelper : Control
    {
        private readonly IBrush? _brush;
        private readonly bool _shadow;
        private readonly IPen _line;
        private static readonly Geometry s_Geometry = Geometry.Parse("m 80 200 c 40 20 150 -40 160 0 l 0 30 c -40 -30 -160 10 -160 -30 z");

        public RelativePointTestPrimitivesHelper(IBrush? brush, bool shadow = false)
        {
            _brush = brush;
            _shadow = shadow;
            if (brush != null)
                _line = new Pen(brush, 10);
            
            MinHeight = MaxHeight = Height = MinWidth = MaxWidth = Width = 256;
        }
        
        public override void Render(DrawingContext context)
        {
            if (_shadow)
            {
                var full = new Rect(default, Bounds.Size);
                context.DrawRectangle(Brushes.White, null, full);
                using (context.PushOpacity(0.3))
                    context.DrawRectangle(_brush, null, full);
            }

            context.DrawRectangle(_brush, null, new Rect(20, 20, 200, 60));
            context.DrawEllipse(_brush, null, new Rect(40, 100, 200, 20));
            context.DrawLine(_line, new Point(60, 140), new Point(240, 160));
            context.DrawGeometry(_brush, null, s_Geometry);
            
            base.Render(context);
        }
    }
}
