// -----------------------------------------------------------------------
// <copyright file="Pen.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    /// <summary>
    /// Describes how a stroke is drawn.
    /// </summary>
    public class Pen
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Pen"/> class.
        /// </summary>
        /// <param name="brush">The brush used to draw.</param>
        /// <param name="thickness">The stroke thickness.</param>
        public Pen(Brush brush, double thickness)
        {
            this.Brush = brush;
            this.Thickness = thickness;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Pen"/> class.
        /// </summary>
        /// <param name="color">The stroke color.</param>
        /// <param name="thickness">The stroke thickness.</param>
        public Pen(uint color, double thickness)
        {
            this.Brush = new SolidColorBrush(color);
            this.Thickness = thickness;
        }

        /// <summary>
        /// Gets the brush used to draw the stroke.
        /// </summary>
        public Brush Brush { get; private set; }

        /// <summary>
        /// Gets the stroke thickness.
        /// </summary>
        public double Thickness { get; private set; }
    }
}
