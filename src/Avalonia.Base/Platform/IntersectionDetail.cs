namespace Avalonia.Platform
{
    /// <summary>
    /// Provides information about the intersection between a hit geometry and a target geometry or visual.
    /// </summary>
    public enum IntersectionDetail
    {
        /// <summary>
        /// The IntersectionDetail value is not calculated.
        /// </summary>
        NotCalculated = 0,

        /// <summary>
        /// There is no intersection between the hit geometry and the
        /// target geometry or visual.
        /// </summary>
        Empty = 1,

        /// <summary>
        /// The target geometry or visual is fully inside the hit geometry.
        /// </summary>
        FullyInside = 2,

        /// <summary>
        /// The target geometry or visual fully contains the hit geometry.
        /// </summary>
        FullyContains = 3,

        /// <summary>
        /// The target geometry or visual overlap the hit geometry and is neither one contains the other.
        /// </summary>
        Intersects = 4
    }
}
