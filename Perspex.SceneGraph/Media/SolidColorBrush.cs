// -----------------------------------------------------------------------
// <copyright file="SolidColorBrush.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    /// <summary>
    /// Fills an area with a solid color.
    /// </summary>
    public class SolidColorBrush : Brush
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SolidColorBrush"/> class.
        /// </summary>
        /// <param name="color">The color to use.</param>
        public SolidColorBrush(Color color)
        {
            this.Color = color;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SolidColorBrush"/> class.
        /// </summary>
        /// <param name="color">The color to use.</param>
        public SolidColorBrush(uint color)
            : this(Color.FromUInt32(color))
        {
        }

        /// <summary>
        /// Gets the color of the brush.
        /// </summary>
        public Color Color
        {
            get;
            private set;
        }

        /// <summary>
        /// Returns a string representation of the brush.
        /// </summary>
        /// <returns>A string representation of the brush.</returns>
        public override string ToString()
        {
            return this.Color.ToString();
        }
    }
}
