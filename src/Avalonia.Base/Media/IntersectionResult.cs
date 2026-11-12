namespace Avalonia.Media
{
    /// <summary>
    /// Provides information about the intersection between a hit geometry and a target geometry or visual.
    /// </summary>
    public enum IntersectionResult
    {
        /// <summary>
        /// The IntersectionDetail value is not calculated.
        /// </summary>
        NotCalculated = 0,

        /// <summary>
        /// The <see cref="Geometry"/> hit test parameter and the target visual, or geometry, do not intersect.
        /// </summary>
        Empty = 1,

        /// <summary>
        /// The target geometry or visual is fully inside the hit test <see cref="Geometry"/>.
        /// </summary>
        FullyInside = 2,

        /// <summary>
        /// The <see cref="Geometry"/> hit test parameter is fully contained within the boundary of the target visual or geometry.
        /// </summary>
        FullyContains = 3,

        /// <summary>
        /// The <see cref="Geometry"/> hit test parameter and the target visual, or geometry, intersect. This means that the two elements overlap, but neither element contains the other.
        /// </summary>
        Intersects = 4
    }
}
