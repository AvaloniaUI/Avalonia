namespace Perspex.Direct2D1.Media
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class SolidColorBrushImpl : BrushImpl
    {
        public SolidColorBrushImpl(Perspex.Media.SolidColorBrush brush, SharpDX.Direct2D1.RenderTarget target, Size destinationSize)
            : base(brush, target, destinationSize)
        {
            this.PlatformBrush = new SharpDX.Direct2D1.SolidColorBrush(target, brush?.Color.ToDirect2D() ?? new SharpDX.Color4());
        }
    }
}
