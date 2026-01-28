namespace Avalonia.Controls
{
    /// <summary>
    /// Defines the alignment mode of the lines inside a <see cref="FlexPanel"/> along the cross-axis. 
    /// </summary>
    public enum AlignContent
    {
        /// <summary>
        /// Lines are packed toward the start of the container.
        /// </summary>
        FlexStart,
        
        /// <summary>
        /// Lines are packed toward the end of the container.
        /// </summary>
        FlexEnd,
        
        /// <summary>
        /// Lines are packed toward the center of the container
        /// </summary>
        Center,
        
        /// <summary>
        /// Lines are stretched to take up the remaining space.
        /// </summary>
        /// <remarks>
        /// This is the default value.
        /// </remarks>
        Stretch,
        
        /// <summary>
        /// Lines are evenly distributed in the container, with no space on either end.
        /// </summary>
        SpaceBetween,
        
        /// <summary>
        /// Lines are evenly distributed in the container, with half-size spaces on either end.
        /// </summary>
        SpaceAround,
        
        /// <summary>
        /// Lines are evenly distributed in the container, with equal-size spaces between each line and on either end. 
        /// </summary>
        SpaceEvenly
    }
}
