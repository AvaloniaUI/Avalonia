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
        private readonly IPen _line;

        public RelativePointTestPrimitivesHelper(IBrush? brush)
        {
            _brush = brush;
            if (brush != null)
                _line = new Pen(brush, 10);
            
            MinHeight = MaxHeight = Height = MinWidth = MaxWidth = Width = 256;
        }
        
        public override void Render(DrawingContext context)
        {
            context.DrawRectangle(_brush, null, new Rect(20, 20, 200, 60));
            context.DrawEllipse(_brush, null, new Rect(40, 100, 200, 20));
            context.DrawLine(_line, new Point(60, 140), new Point(240, 160));
            
            base.Render(context);
        }
    }
}
