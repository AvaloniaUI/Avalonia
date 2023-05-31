using Avalonia.Metadata;

namespace Avalonia.Rendering
{
    /// <summary>
    /// Represents the root of a renderable tree.
    /// </summary>
    [NotClientImplementable]
    public interface IRenderRoot
    {
        /// <summary>
        /// Gets the client size of the window.
        /// </summary>
        Size ClientSize { get; }

        /// <summary>
        /// Gets the renderer for the window.
        /// </summary>
        public IRenderer Renderer { get; }
        
        public IHitTester HitTester { get; }

        /// <summary>
        /// The scaling factor to use in rendering.
        /// </summary>
        double RenderScaling { get; }

        /// <summary>
        /// Converts a point from screen to client coordinates.
        /// </summary>
        /// <param name="point">The point in screen device coordinates.</param>
        /// <returns>The point in client coordinates.</returns>
        Point PointToClient(PixelPoint point);

        /// <summary>
        /// Converts a point from client to screen coordinates.
        /// </summary>
        /// <param name="point">The point in client coordinates.</param>
        /// <returns>The point in screen device coordinates.</returns>
        PixelPoint PointToScreen(Point point);
    }
}
