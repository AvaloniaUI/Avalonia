namespace Avalonia.Controls
{
    /// <summary>
    /// Describes the orientation and direction along which items are placed inside the <see cref="FlexPanel"/>
    /// </summary>
    public enum FlexDirection
    {
        /// <summary>
        /// Items are placed along the horizontal axis, starting from the left
        /// </summary>
        /// <remarks>
        /// This is the default value.
        /// </remarks>
        Row,
        
        /// <summary>
        /// Items are placed along the horizontal axis, starting from the right
        /// </summary>
        RowReverse,
        
        /// <summary>
        /// Items are placed along the vertical axis, starting from the top
        /// </summary>
        Column,
        
        /// <summary>
        /// Items are placed along the vertical axis, starting from the bottom
        /// </summary>
        ColumnReverse
    }
}
