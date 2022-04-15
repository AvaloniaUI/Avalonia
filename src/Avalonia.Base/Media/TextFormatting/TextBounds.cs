using System.Collections.Generic;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// The bounding rectangle of a range of characters
    /// </summary>
    public sealed class TextBounds
    {
        /// <summary>
        /// Constructing TextBounds object
        /// </summary>
        internal TextBounds(Rect bounds, FlowDirection flowDirection)
        {
            Rectangle = bounds;
            FlowDirection = flowDirection;
        }

        /// <summary>
        /// Bounds rectangle
        /// </summary>
        public Rect Rectangle { get; }

        /// <summary>
        /// Text flow direction inside the boundary rectangle
        /// </summary>
        public FlowDirection FlowDirection { get; }
    }
}
