using System;
using global::Cairo;

namespace Perspex.Cairo
{
	public class SolidColorBrushImpl : BrushImpl
	{
		public SolidColorBrushImpl(Perspex.Media.SolidColorBrush brush, double opacityOverride = 1.0f)
		{
			var color = brush?.Color.ToCairo() ?? new Color();

			if (brush != null && brush.Opacity > 1)
				color.A = Math.Min(brush.Opacity, color.A);
			
			color.A = Math.Min(opacityOverride, color.A);
			this.PlatformBrush = new SolidPattern(brush?.Color.ToCairo() ?? new Color());
		}
	}
}

