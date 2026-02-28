namespace Avalonia.Controls
{
    
    /// <summary>
    /// Describes the main-axis alignment of items inside a <see cref="FlexPanel"/> line.
    /// </summary>
    public enum JustifyContent
    {
        /// <summary>
        /// Child items are packed toward the start of the line.
        /// </summary>
        /// <remarks>
        /// This is the default value.
        /// </remarks>
        FlexStart,
        
        /// <summary>
        /// Child items are packed toward the end of the line.
        /// </summary>
        FlexEnd,
        
        /// <summary>
        /// Child items are packed toward the center of the line.
        /// </summary>
        /// <remarks>
        /// If the leftover free-space is negative, the child items will overflow equally in both directions.
        /// </remarks>
        Center,
        
        /// <summary>
        /// Child items are evenly distributed in the line, with no space on either end.
        /// </summary>
        /// <remarks>
        /// If the leftover free-space is negative or there is only a single child item on the line,
        /// this value is identical to <see cref="FlexStart"/>.
        /// </remarks>
        SpaceBetween,
        
        /// <summary>
        /// Child items are evenly distributed in the line, with half-size spaces on either end.
        /// </summary>
        /// <remarks>
        /// If the leftover free-space is negative or there is only a single child item on the line,
        /// this value is identical to <see cref="Center"/>.
        /// </remarks>
        SpaceAround,
        
        /// <summary>
        /// Child items are evenly distributed in the line, with equal-size spaces between each item and on either end.
        /// </summary>
        SpaceEvenly
    }
}
