// -----------------------------------------------------------------------
// <copyright file="RelativeRect.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    /// <summary>
    /// Defines a rectangle that may be defined relative to another rectangle.
    /// </summary>
    public struct RelativeRect
    {
        /// <summary>
        /// A rectangle that represents 100% of an area.
        /// </summary>
        public static readonly RelativeRect Fill = new RelativeRect(0, 0, 1, 1, OriginUnit.Percent);

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativeRect"/> structure.
        /// </summary>
        /// <param name="x">The X position.</param>
        /// <param name="y">The Y position.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="unit">The unit of the rect.</param>
        public RelativeRect(double x, double y, double width, double height, OriginUnit unit)
        {
            this.Rect = new Rect(x, y, width, height);
            this.Unit = unit;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativeRect"/> structure.
        /// </summary>
        /// <param name="rect">The rectangle.</param>
        /// <param name="unit">The unit of the rect.</param>
        public RelativeRect(Rect rect, OriginUnit unit)
        {
            this.Rect = rect;
            this.Unit = unit;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativeRect"/> structure.
        /// </summary>
        /// <param name="size">The size of the rectangle.</param>
        /// <param name="unit">The unit of the rect.</param>
        public RelativeRect(Size size, OriginUnit unit)
        {
            this.Rect = new Rect(size);
            this.Unit = unit;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativeRect"/> structure.
        /// </summary>
        /// <param name="position">The position of the rectangle.</param>
        /// <param name="size">The size of the rectangle.</param>
        /// <param name="unit">The unit of the rect.</param>
        public RelativeRect(Point position, Size size, OriginUnit unit)
        {
            this.Rect = new Rect(position, size);
            this.Unit = unit;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativeRect"/> structure.
        /// </summary>
        /// <param name="topLeft">The top left position of the rectangle.</param>
        /// <param name="bottomRight">The bottom right position of the rectangle.</param>
        /// <param name="unit">The unit of the rect.</param>
        public RelativeRect(Point topLeft, Point bottomRight, OriginUnit unit)
        {
            this.Rect = new Rect(topLeft, bottomRight);
            this.Unit = unit;
        }

        /// <summary>
        /// Gets the unit of the rectangle.
        /// </summary>
        public OriginUnit Unit { get; }

        /// <summary>
        /// Gets the rectangle.
        /// </summary>
        public Rect Rect { get; }

        /// <summary>
        /// Converts a <see cref="RelativeRect"/> into pixels.
        /// </summary>
        /// <param name="size">The size of the visual.</param>
        /// <returns>The origin point in pixels.</returns>
        public Rect ToPixels(Size size)
        {
            return this.Unit == OriginUnit.Pixels ?
                this.Rect :
                new Rect(
                    this.Rect.X * size.Width,
                    this.Rect.Y * size.Height,
                    this.Rect.Width * size.Width,
                    this.Rect.Height * size.Height);
        }
    }
}
