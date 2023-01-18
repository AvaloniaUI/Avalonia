namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Specify how panning snap points are processed for gesture input.
    /// </summary>
    public enum SnapPointsType
    {
        /// <summary>
        /// No snapping behavior.
        /// </summary>
        None,

        /// <summary>
        /// Content always stops at the snap point closest to where inertia would naturally stop along the direction of inertia.
        /// </summary>
        Mandatory,

        /// <summary>
        /// Content always stops at the snap point closest to the release point along the direction of inertia.
        /// </summary>
        MandatorySingle
    }
}
