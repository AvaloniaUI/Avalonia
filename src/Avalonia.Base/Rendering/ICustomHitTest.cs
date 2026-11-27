using Avalonia.Media;

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

        /// <summary>
        /// Determines how geometry intersects the hit-test area.
        /// </summary>
        /// <param name="geometry">
        /// The geometry to hit test, mapped to local coordinate space.
        /// </param>
        /// <returns>An <see cref="IntersectionResult"/> describing how the geometry and the hit-test area intersect.</returns>
        /// <remarks>
        /// Does not hit test children. Test manually if you need children to be tested.
        /// </remarks>
        IntersectionResult HitTest(Geometry geometry)
        {
            return IntersectionResult.Empty;
        }
    }
}
