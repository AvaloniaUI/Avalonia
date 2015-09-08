





namespace Perspex.Direct2D1.Media
{
    using System;

    public abstract class BrushImpl : IDisposable
    {
        public SharpDX.Direct2D1.Brush PlatformBrush { get; set; }

        public virtual void Dispose()
        {
            if (this.PlatformBrush != null)
            {
                this.PlatformBrush.Dispose();
            }
        }
    }
}
