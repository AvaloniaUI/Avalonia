using System.Linq;
using Avalonia.Media;

namespace Avalonia.Direct2D1.Media
{
    public class LinearGradientBrushImpl : BrushImpl
    {
        public LinearGradientBrushImpl(
            ILinearGradientBrush brush,
            SharpDX.Direct2D1.RenderTarget target,
            Rect destinationRect)
        {
            if (brush.GradientStops.Count == 0)
            {
                return;
            }

            var gradientStops = brush.GradientStops.Select(s => new SharpDX.Direct2D1.GradientStop
            {
                Color = s.Color.ToDirect2D(),
                Position = (float)s.Offset
            }).ToArray();

            var position = destinationRect.Position;
            var startPoint = position + brush.StartPoint.ToPixels(destinationRect.Size);
            var endPoint = position + brush.EndPoint.ToPixels(destinationRect.Size);

            using (var stops = new SharpDX.Direct2D1.GradientStopCollection(
                target,
                gradientStops,
                brush.SpreadMethod.ToDirect2D()))
            {
                PlatformBrush = new SharpDX.Direct2D1.LinearGradientBrush(
                    target,
                    new SharpDX.Direct2D1.LinearGradientBrushProperties
                    {
                        StartPoint = startPoint.ToSharpDX(),
                        EndPoint = endPoint.ToSharpDX()
                    },
                    new SharpDX.Direct2D1.BrushProperties
                    {
                        Opacity = (float)brush.Opacity,
                        Transform = PrimitiveExtensions.Matrix3x2Identity,
                    },
                    stops);
            }
        }
    }
}
