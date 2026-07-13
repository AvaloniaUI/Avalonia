using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Rendering
{

    /// <summary>
    /// Allows customization of hit-testing
    /// </summary>
    public interface ICustomHitTest
    {
        /// <param name="point">The point to hit test in global coordinate space.</param>
        bool HitTest(Point point);

        /// <summary>
        /// Hit test the geometry in this node.
        /// </summary>
        /// <param name="geometry">The geometry in global coordinates.</param>
        /// <returns>The <see cref="IntersectionDetail"/> describing the intersecting between the hit geometry and the node's geometry.</returns>
        /// <remarks>
        /// This method does not recurse to childs, if you want
        /// to hit test children they must be hit tested manually.
        /// </remarks>
        IntersectionDetail HitTest(Geometry geometry)
        {
            return IntersectionDetail.Empty;
        }
    }
}
