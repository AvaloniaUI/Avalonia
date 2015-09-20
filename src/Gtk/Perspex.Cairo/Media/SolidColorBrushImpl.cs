using System;
using global::Cairo;

namespace Perspex.Cairo
{
	public class SolidColorBrushImpl : BrushImpl
	{
		public SolidColorBrushImpl(Perspex.Media.SolidColorBrush brush, double opacityOverride = 1.0f)
		{
			var color = brush?.Color.ToCairo() ?? new Color();

            if (brush != null)
				color.A = Math.Min(brush.Opacity, color.A);
			
            if (opacityOverride < 1.0f)
			    color.A = Math.Min(opacityOverride, color.A);

			this.PlatformBrush = new SolidPattern(color);
		}
	}
}

