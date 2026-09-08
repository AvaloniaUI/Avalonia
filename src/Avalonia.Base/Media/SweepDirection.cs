namespace Avalonia.Media
{
    /// <summary>
    /// Defines the direction an which elliptical arc is drawn.
    /// </summary>
#if !BUILDTASK
    public
#endif
    enum SweepDirection
    {
        /// <summary>
        /// Specifies that arcs are drawn in a counter clockwise (negative-angle) direction.
        /// </summary>
        CounterClockwise,

        /// <summary>
        /// Specifies that arcs are drawn in a clockwise (positive-angle) direction.
        /// </summary>
        Clockwise,
    }
}
