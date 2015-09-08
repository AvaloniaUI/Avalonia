





namespace Perspex.Direct2D1.Media
{
    using Perspex.Media;
    using SharpDX;

    internal class BrushWrapper : ComObject
    {
        public BrushWrapper(Brush brush)
        {
            this.Brush = brush;
        }

        public Brush Brush { get; private set; }
    }
}
