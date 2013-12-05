namespace Perspex
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Defines a size.
    /// </summary>
    public struct Size
    {
        /// <summary>
        /// The width.
        /// </summary>
        private double width;

        /// <summary>
        /// The height.
        /// </summary>
        private double height;

        /// <summary>
        /// Initializes a new instance of the <see cref="Size"/> structure.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        public Size(double width, double height)
        {
            this.width = width;
            this.height = height;
        }

        /// <summary>
        /// Gets the width.
        /// </summary>
        public double Width
        {
            get { return this.width; }
        }

        /// <summary>
        /// Gets the height.
        /// </summary>
        public double Height
        {
            get { return this.height; }
        }

        /// <summary>
        /// Constrains the size.
        /// </summary>
        /// <param name="constraint">The size to constrain to.</param>
        /// <returns>The constrained size.</returns>
        public Size Constrain(Size constraint)
        {
            return new Size(
                Math.Min(this.width, constraint.width),
                Math.Min(this.height, constraint.height));
        }

        /// <summary>
        /// Deflates the size by a <see cref="Thickness"/>.
        /// </summary>
        /// <param name="thickness">The thickness.</param>
        /// <returns>The deflated size.</returns>
        /// <remarks>The deflated size cannot be less than 0.</remarks>
        public Size Deflate(Thickness thickness)
        {
            return new Size(
                Math.Max(0, this.width - thickness.Left - thickness.Right),
                Math.Max(0, this.height - thickness.Top - thickness.Bottom));
        }

        /// <summary>
        /// Inflates the size by a <see cref="Thickness"/>.
        /// </summary>
        /// <param name="thickness">The thickness.</param>
        /// <returns>The inflated size.</returns>
        public Size Inflate(Thickness thickness)
        {
            return new Size(
                this.width + thickness.Left + thickness.Right,
                this.height + thickness.Top + thickness.Bottom);
        }

        /// <summary>
        /// Returns the string representation of the size.
        /// </summary>
        /// <returns>The string representation of the size</returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}, {1}", this.width, this.height);
        }
    }
}
