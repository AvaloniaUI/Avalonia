using Avalonia.Media;
using SharpDX;

namespace Avalonia.Direct2D1.Media
{
    internal class BrushWrapper : ComObject
    {
        public BrushWrapper(IBrush brush)
        {
            Brush = brush;
        }

        public IBrush Brush { get; private set; }
    }
}
