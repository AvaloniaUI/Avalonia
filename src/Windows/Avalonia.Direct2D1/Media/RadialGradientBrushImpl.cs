using System.Linq;
using Avalonia.Media;
using Vortice.Direct2D1;

namespace Avalonia.Direct2D1.Media
{
    public class RadialGradientBrushImpl : BrushImpl
    {
        public RadialGradientBrushImpl(
            IRadialGradientBrush brush,
            Vortice.Direct2D1.ID2D1RenderTarget target,
            Size destinationSize)
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
                Gamma.StandardRgb,
                brush.SpreadMethod.ToDirect2D()))
            {
                PlatformBrush = target.CreateRadialGradientBrush(
                    new Vortice.Direct2D1.RadialGradientBrushProperties
                    {
                        Center = centerPoint.ToSharpDX(),
                        GradientOriginOffset = gradientOrigin.ToSharpDX(),
                        RadiusX = (float)radiusX,
                        RadiusY = (float)radiusY
                    },
                    new Vortice.Direct2D1.BrushProperties
                    {
                        Opacity = (float)brush.Opacity,
                        Transform = PrimitiveExtensions.Matrix3x2Identity,
                    },
                    stops);
            }
        }
    }
}
