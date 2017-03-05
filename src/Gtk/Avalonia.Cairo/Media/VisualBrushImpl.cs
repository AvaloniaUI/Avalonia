using System;
using Avalonia.Media;
using global::Cairo;

namespace Avalonia.Cairo.Media
{
	public class VisualBrushImpl : BrushImpl
	{
		public VisualBrushImpl(IVisualBrush brush, Size destinationSize)
		{
			this.PlatformBrush = TileBrushes.CreateTileBrush(brush, destinationSize);
		}
	}
}

