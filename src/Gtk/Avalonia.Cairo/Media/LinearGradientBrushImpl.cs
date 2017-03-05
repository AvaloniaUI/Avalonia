using System;
using Avalonia.Media;
using global::Cairo;

namespace Avalonia.Cairo
{
	public class LinearGradientBrushImpl : BrushImpl
	{
		public LinearGradientBrushImpl(ILinearGradientBrush brush, Size destinationSize)
		{
			var start = brush.StartPoint.ToPixels(destinationSize);
			var end = brush.EndPoint.ToPixels(destinationSize);

			this.PlatformBrush = new LinearGradient(start.X, start.Y, end.X, end.Y);

			foreach (var stop in brush.GradientStops)
				((LinearGradient)this.PlatformBrush).AddColorStop(stop.Offset, stop.Color.ToCairo());

			((LinearGradient)this.PlatformBrush).Extend = Extend.Pad;
		}
	}
}

