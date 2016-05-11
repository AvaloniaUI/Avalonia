using System;
using global::Cairo;

namespace Avalonia.Cairo
{
	public class RadialGradientBrushImpl : BrushImpl
	{
		public RadialGradientBrushImpl(Avalonia.Media.RadialGradientBrush brush, Size destinationSize)
		{
			var center = brush.Center.ToPixels(destinationSize);
			var gradientOrigin = brush.GradientOrigin.ToPixels(destinationSize);
            var radius = brush.Radius;

			this.PlatformBrush = new RadialGradient(center.X, center.Y, radius, gradientOrigin.X, gradientOrigin.Y, radius);

            foreach (var stop in brush.GradientStops)
            {
                ((RadialGradient)this.PlatformBrush).AddColorStop(stop.Offset, stop.Color.ToCairo());
            }

			((RadialGradient)this.PlatformBrush).Extend = Extend.Pad;
		}
	}
}

