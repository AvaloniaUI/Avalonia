using Avalonia.Media;

namespace Avalonia.VisualTree
{
    public interface IRenderTimeCriticalVisual
    {
        bool HasRenderTimeCriticalContent { get; }
        bool ThreadSafeHasNewFrame { get; }
        void ThreadSafeRender(DrawingContext context, Size logicalSize, double scaling);
    }
}
