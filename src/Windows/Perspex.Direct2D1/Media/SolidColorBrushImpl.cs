





namespace Perspex.Direct2D1.Media
{
    public class SolidColorBrushImpl : BrushImpl
    {
        public SolidColorBrushImpl(Perspex.Media.SolidColorBrush brush, SharpDX.Direct2D1.RenderTarget target)
        {
            this.PlatformBrush = new SharpDX.Direct2D1.SolidColorBrush(
                target, 
                brush?.Color.ToDirect2D() ?? new SharpDX.Color4(), 
                new SharpDX.Direct2D1.BrushProperties 
                { 
                    Opacity = brush != null ? (float)brush.Opacity : 1.0f, 
                    Transform = target.Transform 
                }
            );
        }
    }
}
