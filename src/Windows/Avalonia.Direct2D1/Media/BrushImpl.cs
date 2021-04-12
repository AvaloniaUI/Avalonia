using System;

namespace Avalonia.Direct2D1.Media
{
    public abstract class BrushImpl : IDisposable
    {
        public Vortice.Direct2D1.ID2D1Brush PlatformBrush { get; set; }

        public virtual void Dispose()
        {
            if (PlatformBrush != null)
            {
                PlatformBrush.Dispose();
            }
        }
    }
}
