namespace Avalonia.Input
{
    /// <summary>
    /// Specifies how to navigate using XYFocus when multiple candidate targets exist
    /// </summary>
    public enum XYFocusNavigationStrategy
    {
        /// <summary>
        /// Navigation strategy is inherited from element's ancestors. If all ancestors have
        /// a value of Auto, the fallback is projection
        /// </summary>
        Auto,

        /// <summary>
        /// Indicates that focus moves to the first element encountered when projecting the 
        /// edge of the currently focused element in the direction of navigation
        /// </summary>
        Projection,

        /// <summary>
        /// Indicates that focus moves to the element clsoes to the axis of the navigation direction
        /// </summary>
        NavigationDirectionDistance,

        /// <summary>
        /// Indicate that focus moves to the closest element based on the shortest 2D distance 
        /// (Manhattan metric)
        /// </summary>
        RectilinearDistance
    }
}
