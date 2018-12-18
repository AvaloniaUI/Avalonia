using Avalonia.Media;

namespace Avalonia.VisualTree
{
    public interface IRenderTimeCriticalVisual
    {
        bool HasNewFrame { get; }
        void ThreadSafeRender(DrawingContext context, Size logicalSize, double scaling);
    }
}
