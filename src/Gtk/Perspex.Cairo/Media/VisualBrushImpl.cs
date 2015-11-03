using System;
using global::Cairo;

namespace Perspex.Cairo.Media
{
	public class VisualBrushImpl : BrushImpl
	{
		public VisualBrushImpl(Perspex.Media.VisualBrush brush, Size destinationSize)
		{
			this.PlatformBrush = TileBrushes.CreateTileBrush(brush, destinationSize);
		}
	}
}

