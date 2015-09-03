using System;

namespace Perspex.Direct2D1.Media
{
    public abstract class BrushImpl : IDisposable
    {
        public SharpDX.Direct2D1.Brush PlatformBrush { get; set; }

        public BrushImpl(Perspex.Media.Brush brush, SharpDX.Direct2D1.RenderTarget target, Size destinationSize)
        {
        }

        public virtual void Dispose()
        {
            if (this.PlatformBrush != null)
                this.PlatformBrush.Dispose();
        }
    }
}
