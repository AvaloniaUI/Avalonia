using System;

namespace Avalonia.Direct2D1.Media
{
    internal abstract class BrushImpl : IDisposable
    {
        public SharpDX.Direct2D1.Brush PlatformBrush { get; set; }

        public virtual void Dispose()
        {
            if (PlatformBrush != null)
            {
                PlatformBrush.Dispose();
            }
        }
    }
}
