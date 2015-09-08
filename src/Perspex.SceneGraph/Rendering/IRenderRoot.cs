





namespace Perspex.Rendering
{
    using Perspex.Platform;

    /// <summary>
    /// Represents the root of a renderable tree.
    /// </summary>
    public interface IRenderRoot
    {
        /// <summary>
        /// Gets the renderer for the tree.
        /// </summary>
        IRenderer Renderer { get; }

        /// <summary>
        /// Gets the render manager which schedules renders.
        /// </summary>
        IRenderManager RenderManager { get; }

        /// <summary>
        /// Translates a point to screen co-ordinates.
        /// </summary>
        /// <param name="p">The point.</param>
        /// <returns>The point in screen co-ordinates.</returns>
        Point TranslatePointToScreen(Point p);
    }
}
