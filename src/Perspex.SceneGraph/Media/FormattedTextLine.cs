// -----------------------------------------------------------------------
// <copyright file="FormattedTextLine.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    /// <summary>
    /// Stores information about a line of <see cref="FormattedText"/>.
    /// </summary>
    public class FormattedTextLine
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedTextLine"/> class.
        /// </summary>
        /// <param name="length">The length of the line, in characters.</param>
        /// <param name="height">The height of the line, in pixels.</param>
        public FormattedTextLine(int length, double height)
        {
            this.Length = length;
            this.Height = height;
        }

        /// <summary>
        /// Gets the length of the line, in characters.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Gets the height of the line, in pixels.
        /// </summary>
        public double Height { get; }
    }
}
