namespace Perspex
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Defines a rectangle.
    /// </summary>
    public struct Rect
    {
        /// <summary>
        /// The X position.
        /// </summary>
        private double x;

        /// <summary>
        /// The Y position.
        /// </summary>
        private double y;

        /// <summary>
        /// The width.
        /// </summary>
        private double width;

        /// <summary>
        /// The height.
        /// </summary>
        private double height;

        /// <summary>
        /// Initializes a new instance of the <see cref="Rect"/> structure.
        /// </summary>
        /// <param name="x">The X position.</param>
        /// <param name="y">The Y position.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        public Rect(double x, double y, double width, double height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Rect"/> structure.
        /// </summary>
        /// <param name="size">The size of the rectangle.</param>
        public Rect(Size size)
        {
            this.x = 0;
            this.y = 0;
            this.width = size.Width;
            this.height = size.Height;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Rect"/> structure.
        /// </summary>
        /// <param name="position">The position of the rectangle.</param>
        /// <param name="size">The size of the rectangle.</param>
        public Rect(Point position, Size size)
        {
            this.x = position.X;
            this.y = position.Y;
            this.width = size.Width;
            this.height = size.Height;
        }

        /// <summary>
        /// Gets the X position.
        /// </summary>
        public double X
        {
            get { return this.x; }
        }

        /// <summary>
        /// Gets the Y position.
        /// </summary>
        public double Y
        {
            get { return this.y; }
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
        /// Gets the position of the rectangle.
        /// </summary>
        public Point Position
        {
            get { return new Point(this.x, this.y); }
        }

        /// <summary>
        /// Gets the size of the rectangle.
        /// </summary>
        public Size Size
        {
            get { return new Size(this.width, this.height); }
        }

        /// <summary>
        /// Determines whether a points in in the bounds of the rectangle.
        /// </summary>
        /// <param name="p">The point.</param>
        /// <returns>true if the point is in the bounds of the rectangle; otherwise false.</returns>
        public bool Contains(Point p)
        {
            return p.X >= this.x && p.X < this.x + this.width &&
                   p.Y >= this.y && p.Y < this.y + this.height;
        }

        /// <summary>
        /// Deflates the rectangle by a <see cref="Thickness"/>.
        /// </summary>
        /// <param name="thickness">The thickness.</param>
        /// <returns>The deflated rectangle.</returns>
        /// <remarks>The deflated rectangle size cannot be less than 0.</remarks>
        public Rect Deflate(Thickness thickness)
        {
            return new Rect(
                new Point(this.x + thickness.Left, this.y + thickness.Top),
                this.Size.Deflate(thickness));
        }

        /// <summary>
        /// Returns the string representation of the rectangle.
        /// </summary>
        /// <returns>The string representation of the rectangle.</returns>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture, 
                "{0}, {1}, {2}, {3}", 
                this.x,
                this.y,
                this.width, 
                this.height);
        }
    }
}
