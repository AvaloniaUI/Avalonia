namespace Avalonia.Rendering
{

    /// <summary>
    /// Allows hit-testing to use a custom area instead of the area of the rendered content.
    /// </summary>
    public interface ICustomHitTest
    {
        /// <summary>
        /// Determines whether a point is within the hit-test area.
        /// </summary>
        /// <param name="point">
        /// The point to hit test in local coordinate space, i.e., (0, 0)
        /// is the top-left corner of the visual element, not the top-left corner of the window.
        /// </param>
        bool HitTest(Point point);
    }
}
