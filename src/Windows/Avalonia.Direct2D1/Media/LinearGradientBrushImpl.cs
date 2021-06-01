using System.Linq;
using System.Numerics;
using Avalonia.Media;
using Vortice.Direct2D1;

namespace Avalonia.Direct2D1.Media
{
    public class LinearGradientBrushImpl : BrushImpl
    {
        public LinearGradientBrushImpl(
            ILinearGradientBrush brush,
            ID2D1RenderTarget target,
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

            var startPoint = brush.StartPoint.ToPixels(destinationSize);
            var endPoint = brush.EndPoint.ToPixels(destinationSize);

            using (var stops = target.CreateGradientStopCollection(
                gradientStops,
                Gamma.StandardRgb,
                brush.SpreadMethod.ToDirect2D()))
            {
                PlatformBrush = target.CreateLinearGradientBrush(
                    new LinearGradientBrushProperties
                    {
                        StartPoint = startPoint.ToVortice(),
                        EndPoint = endPoint.ToVortice()
                    },
                    new BrushProperties
                    {
                        Opacity = (float)brush.Opacity,
                        Transform = Matrix3x2.Identity,
                    },
                    stops);
            }
        }
    }
}
