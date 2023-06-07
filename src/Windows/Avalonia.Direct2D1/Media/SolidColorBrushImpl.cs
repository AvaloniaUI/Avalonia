using Avalonia.Media;

namespace Avalonia.Direct2D1.Media
{
    internal class SolidColorBrushImpl : BrushImpl
    {
        public SolidColorBrushImpl(ISolidColorBrush brush, SharpDX.Direct2D1.RenderTarget target)
        {
            PlatformBrush = new SharpDX.Direct2D1.SolidColorBrush(
                target,
                brush?.Color.ToDirect2D() ?? new SharpDX.Mathematics.Interop.RawColor4(),
                new SharpDX.Direct2D1.BrushProperties
                {
                    Opacity = brush != null ? (float)brush.Opacity : 1.0f,
                    Transform = target.Transform
                }
            );
        }

        /// <summary>
        /// Direct2D has no ConicGradient implementation so fall back to a solid colour brush based on 
        /// the first gradient stop.
        /// </summary>
        public SolidColorBrushImpl(IConicGradientBrush brush, SharpDX.Direct2D1.DeviceContext target)
        {
            PlatformBrush = new SharpDX.Direct2D1.SolidColorBrush(
                target,
                brush?.GradientStops[0].Color.ToDirect2D() ?? new SharpDX.Mathematics.Interop.RawColor4(),
                new SharpDX.Direct2D1.BrushProperties
                {
                    Opacity = brush != null ? (float)brush.Opacity : 1.0f,
                    Transform = target.Transform
                }
            );
        }
    }
}
