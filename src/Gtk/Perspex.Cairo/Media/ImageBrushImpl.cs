using System;
using global::Cairo;

namespace Perspex.Cairo.Media
{
	public class ImageBrushImpl : BrushImpl
	{
		public ImageBrushImpl(Perspex.Media.ImageBrush brush, Size destinationSize)
		{
			this.PlatformBrush = TileBrushes.CreateTileBrush(brush, destinationSize);
		}
	}
}

