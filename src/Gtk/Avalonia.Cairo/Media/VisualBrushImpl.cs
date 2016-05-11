using System;
using global::Cairo;

namespace Avalonia.Cairo.Media
{
	public class VisualBrushImpl : BrushImpl
	{
		public VisualBrushImpl(Avalonia.Media.VisualBrush brush, Size destinationSize)
		{
			this.PlatformBrush = TileBrushes.CreateTileBrush(brush, destinationSize);
		}
	}
}

