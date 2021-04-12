using Avalonia.Media;
using Color = Vortice.Mathematics.Color;

namespace Avalonia.Direct2D1.Media
{
    public class SolidColorBrushImpl : BrushImpl
    {
        public SolidColorBrushImpl(ISolidColorBrush brush, Vortice.Direct2D1.ID2D1RenderTarget target)
        {
            PlatformBrush = target.CreateSolidColorBrush(
                brush?.Color.ToDirect2D() ?? new Color(),
                new Vortice.Direct2D1.BrushProperties
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
        public SolidColorBrushImpl(IConicGradientBrush brush, Vortice.Direct2D1.ID2D1DeviceContext target)
        {
            PlatformBrush = target.CreateSolidColorBrush(
                brush?.GradientStops[0].Color.ToDirect2D() ?? new Color(),
                new Vortice.Direct2D1.BrushProperties
                {
                    Opacity = brush != null ? (float)brush.Opacity : 1.0f,
                    Transform = target.Transform
                }
            );
        }
    }
}
