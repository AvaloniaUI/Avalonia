using System;
using Avalonia.Metadata;

namespace Avalonia.Direct2D1.Media
{
    [Unstable]
    public abstract class BrushImpl : IDisposable
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
