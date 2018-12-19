using Avalonia.Media;

namespace Avalonia.VisualTree
{
    public interface IRenderTimeCriticalVisual
    {
        /// <summary>
        /// Checks if visual has time critical content. You need to call InvalidateVisual for this property to be re-queried.
        /// </summary>
        bool HasRenderTimeCriticalContent { get; }
        /// <summary>
        /// Checks if visual has a new frame. This property can be read from any thread.
        /// </summary>
        bool ThreadSafeHasNewFrame { get; }
        /// <summary>
        /// Draws the new frame during the render pass. That can happen on any thread
        /// </summary>
        /// <param name="context">DrawingContext. The available API is limited by stuff that can run on non-UI thread (brushes has to be immutable, no visual brush, etc)</param>
        /// <param name="logicalSize">Logical size of the visual during the layout pass associated with the current frame</param>
        /// <param name="scaling">DPI scaling of the target surface associated with the current frame</param>
        void ThreadSafeRender(DrawingContext context, Size logicalSize, double scaling);
    }
}
