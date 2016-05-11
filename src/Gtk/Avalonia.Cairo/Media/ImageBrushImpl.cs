using System;
using global::Cairo;

namespace Avalonia.Cairo.Media
{
	public class ImageBrushImpl : BrushImpl
	{
		public ImageBrushImpl(Avalonia.Media.ImageBrush brush, Size destinationSize)
		{
			this.PlatformBrush = TileBrushes.CreateTileBrush(brush, destinationSize);
		}
	}
}

