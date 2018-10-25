// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Media
{
    /// <summary>
    /// Holds a hit test result from a <see cref="FormattedText"/>.
    /// </summary>
    public class TextHitTestResult
    {
        /// <summary>
        /// Gets the first index within the hit region.
        /// </summary>
        public int TextPosition { get; set; }

        /// <summary>
        /// The number of text positions within the hit region. 
        /// </summary>
        /// <value>
        /// The number of text positions.
        /// </value>
        public int Length { get; set; }

        /// <summary>
        /// Gets the bounding box of the hit region.
        /// </summary>
        /// <value>
        /// The bounding box.
        /// </value>
        public Rect Bounds { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the point is inside the bounds of the text.
        /// </summary>
        public bool IsInside { get; set; }

        /// <summary>
        /// Gets a value indicating whether the hit is on the trailing edge of the character.
        /// </summary>
        public bool IsTrailing { get; set; }
    }
}
