using System.Linq;
using System.Numerics;
using Avalonia.Media;
using Vortice.Direct2D1;

namespace Avalonia.Direct2D1.Media
{
    public class RadialGradientBrushImpl : BrushImpl
    {
        public RadialGradientBrushImpl(
            IRadialGradientBrush brush,
            ID2D1RenderTarget target,
            Size destinationSize,
            float opacity)
        {
            if (brush.GradientStops.Count == 0)
            {
                return;
            }

            var gradientStops = brush.GradientStops.Select(s => new Vortice.Direct2D1.GradientStop
            {
                Color = s.Color.ToDirect2D(),
                Position = (float)s.Offset
            }).ToArray();

            var centerPoint = brush.Center.ToPixels(destinationSize);
            var gradientOrigin = brush.GradientOrigin.ToPixels(destinationSize) - centerPoint;
            
            // Note: Direct2D supports RadiusX and RadiusY but Cairo backend supports only Radius property
            var radiusX = brush.Radius * destinationSize.Width;
            var radiusY = brush.Radius * destinationSize.Height;

            using (var stops = target.CreateGradientStopCollection(
                gradientStops,
                brush.SpreadMethod.ToDirect2D()))
            {
                PlatformBrush = target.CreateRadialGradientBrush(
                    new RadialGradientBrushProperties
                    {
                        Center = centerPoint.ToSharpDX(),
                        GradientOriginOffset = gradientOrigin.ToSharpDX(),
                        RadiusX = (float)radiusX,
                        RadiusY = (float)radiusY
                    },
                    new BrushProperties((float)brush.Opacity * opacity)
                    {
                        Opacity = (float)brush.Opacity * opacity,
                        Transform = Matrix3x2.Identity,
                    },
                    stops);
            }
        }
    }
}
