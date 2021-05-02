namespace Avalonia.Controls.Converters
{
    /// <summary>
    /// Defines constants that specify the filter type for a <see cref="CornerRadiusFilterConverter"/> instance.
    /// </summary>
    public enum CornerRadiusFilterKind
    {
        /// <summary>
        /// No filter applied.
        /// </summary>
        None,
        /// <summary>
        /// Filters TopLeft and TopRight values, sets BottomLeft and BottomRight to 0.
        /// </summary>
        Top,
        /// <summary>
        /// Filters TopRight and BottomRight values, sets TopLeft and BottomLeft to 0.
        /// </summary>
        Right,
        /// <summary>
        /// Filters BottomLeft and BottomRight values, sets TopLeft and TopRight to 0.
        /// </summary>
        Bottom,
        /// <summary>
        /// Filters TopLeft and BottomLeft values, sets TopRight and BottomRight to 0.
        /// </summary>
        Left,
        /// <summary>
        /// Gets the double value of TopLeft corner.
        /// </summary>
        TopLeftValue,
        /// <summary>
        /// Gets the double value of BottomRight corner.
        /// </summary>
        BottomRightValue
    }
}
