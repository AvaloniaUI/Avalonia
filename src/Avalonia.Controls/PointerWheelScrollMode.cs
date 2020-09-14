namespace Avalonia.Controls
{
    /// <summary>
    /// Defines the step to use for scrolling when the PointerWheelChanged event is raised in <see cref="ScrollViewer"/>.
    /// </summary>
    public enum PointerWheelScrollMode
    {
        /// <summary>
        /// Use small (line) change value for the scroll viewer.
        /// </summary>
        SmallChange,
        /// <summary>
        /// Use large (page) change value for the scroll viewer.
        /// </summary>
        LargeChange,
    }
}
