using System;
using Avalonia.Media;
using global::Cairo;

namespace Avalonia.Cairo.Media
{
	public class ImageBrushImpl : BrushImpl
	{
		public ImageBrushImpl(IImageBrush brush, Size destinationSize)
		{
			this.PlatformBrush = TileBrushes.CreateTileBrush(brush, destinationSize);
		}
	}
}

