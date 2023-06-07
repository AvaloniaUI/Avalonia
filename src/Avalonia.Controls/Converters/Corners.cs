using System;

namespace Avalonia.Controls.Converters
{
    /// <summary>
    /// Defines constants that specify one or more corners of a <see cref="CornerRadius"/>.
    /// </summary>
    [Flags]
    public enum Corners
    {
        /// <summary>
        /// No corner.
        /// </summary>
        None,

        /// <summary>
        /// The TopLeft corner.
        /// </summary>
        TopLeft = 1,

        /// <summary>
        /// The TopRight corner.
        /// </summary>
        TopRight = 2,

        /// <summary>
        /// The BottomLeft corner.
        /// </summary>
        BottomLeft = 4,

        /// <summary>
        /// The BottomRight corner.
        /// </summary>
        BottomRight = 8
    }
}
