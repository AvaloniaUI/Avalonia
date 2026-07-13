using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;

namespace Avalonia.Base.UnitTests.Rendering
{
    internal class CustomHitTestBorder : Border, ICustomHitTest
    {
        public bool HitTest(Point point)
        {
            // Move hit testing window halfway to the left
            return new Rect( -Bounds.Width / 2,0, Bounds.Width, Bounds.Height)
                .Contains(point);
        }

         public IntersectionDetail HitTest(Geometry geometry)
        {
            return new RectangleGeometry(new Rect(-Bounds.Width / 2, 0, Bounds.Width, Bounds.Height)).FillContains(geometry) ?? IntersectionDetail.Empty;
        }
    }
}
