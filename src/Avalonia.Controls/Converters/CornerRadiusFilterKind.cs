using System;

namespace Avalonia.Controls.Converters
{
    /// <summary>
    /// Defines constants that specify the filter type for a <see cref="CornerRadiusFilterConverter"/> instance.
    /// </summary>
    [Flags]
    public enum CornerRadiusFilterKinds
    {
        /// <summary>
        /// No filter applied.
        /// </summary>
        None,
        /// <summary>
        /// Filters TopLeft value.
        /// </summary>
        TopLeft = 1,
        /// <summary>
        /// Filters TopRight value.
        /// </summary>
        TopRight = 2,
        /// <summary>
        /// Filters BottomLeft value.
        /// </summary>
        BottomLeft = 4,
        /// <summary>
        /// Filters BottomRight value.
        /// </summary>
        BottomRight = 8
    }
}
