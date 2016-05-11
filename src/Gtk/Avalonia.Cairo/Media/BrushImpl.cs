using System;
using global::Cairo;

namespace Avalonia.Cairo
{
	public abstract class BrushImpl : IDisposable
	{
		public Pattern PlatformBrush { get; protected set; }

		public void Dispose() 
		{
			if (this.PlatformBrush != null)
				this.PlatformBrush.Dispose();
		}
	}
}

