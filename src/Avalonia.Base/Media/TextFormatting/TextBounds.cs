using System.Collections.Generic;
using System.Diagnostics;

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
        [DebuggerStepThrough]
        internal TextBounds(Rect bounds, FlowDirection flowDirection, IList<TextRunBounds> runBounds)
        {
            Rectangle = bounds;
            FlowDirection = flowDirection;
            TextRunBounds = runBounds;
        }

        /// <summary>
        /// Bounds rectangle
        /// </summary>
        public Rect Rectangle { get; internal set; }

        /// <summary>
        /// Text flow direction inside the boundary rectangle
        /// </summary>
        public FlowDirection FlowDirection { get; }

        /// <summary>
        /// Get a list of run bounding rectangles
        /// </summary>
        /// <returns>Array of text run bounds</returns>
        public IList<TextRunBounds> TextRunBounds { get; }
    }
}
