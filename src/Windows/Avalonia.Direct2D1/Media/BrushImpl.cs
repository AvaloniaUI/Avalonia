using System;
using Vortice.Direct2D1;

namespace Avalonia.Direct2D1.Media
{
    public abstract class BrushImpl : IDisposable
    {
        public ID2D1Brush PlatformBrush { get; set; }

        public virtual void Dispose()
        {
            if (PlatformBrush != null)
            {
                PlatformBrush.Dispose();
            }
        }
    }
}
