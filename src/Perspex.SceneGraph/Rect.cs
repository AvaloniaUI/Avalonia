﻿// -----------------------------------------------------------------------
// <copyright file="Rect.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

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
        /// Initializes a new instance of the <see cref="Rect"/> structure.
        /// </summary>
        /// <param name="topLeft">The top left position of the rectangle.</param>
        /// <param name="bottomRight">The bottom right position of the rectangle.</param>
        public Rect(Point topLeft, Point bottomRight)
        {
            this.x = topLeft.X;
            this.y = topLeft.Y;
            this.width = bottomRight.X - topLeft.X;
            this.height = bottomRight.Y - topLeft.Y;
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
        /// Gets the right position of the rectangle.
        /// </summary>
        public double Right
        {
            get { return this.x + this.width; }
        }

        /// <summary>
        /// Gets the bottom position of the rectangle.
        /// </summary>
        public double Bottom
        {
            get { return this.y + this.height; }
        }

        /// <summary>
        /// Gets the top left point of the rectangle.
        /// </summary>
        public Point TopLeft
        {
            get { return new Point(this.x, this.y); }
        }

        /// <summary>
        /// Gets the top right point of the rectangle.
        /// </summary>
        public Point TopRight
        {
            get { return new Point(this.Right, this.y); }
        }

        /// <summary>
        /// Gets the bottom left point of the rectangle.
        /// </summary>
        public Point BottomLeft
        {
            get { return new Point(this.x, this.Bottom); }
        }

        /// <summary>
        /// Gets the bottom right point of the rectangle.
        /// </summary>
        public Point BottomRight
        {
            get { return new Point(this.Right, this.Bottom); }
        }

        /// <summary>
        /// Gets the center point of the rectangle.
        /// </summary>
        public Point Center
        {
            get { return new Point(this.x + (this.width / 2), this.y + (this.height / 2)); }
        }

        /// <summary>
        /// Gets a value that indicates whether the rectangle is empty.
        /// </summary>
        public bool IsEmpty
        {
            get { return this.width == 0 && this.height == 0; }
        }

        /// <summary>
        /// Checks for equality between two <see cref="Rect"/>s.
        /// </summary>
        /// <param name="left">The first rect.</param>
        /// <param name="right">The second rect.</param>
        /// <returns>True if the rects are equal; otherwise false.</returns>
        public static bool operator ==(Rect left, Rect right)
        {
            return left.Position == right.Position && left.Size == right.Size;
        }

        /// <summary>
        /// Checks for unequality between two <see cref="Rect"/>s.
        /// </summary>
        /// <param name="left">The first rect.</param>
        /// <param name="right">The second rect.</param>
        /// <returns>True if the rects are unequal; otherwise false.</returns>
        public static bool operator !=(Rect left, Rect right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Multiplies a rectangle by a vector.
        /// </summary>
        /// <param name="rect">The rectangle.</param>
        /// <param name="scale">The vector scale.</param>
        /// <returns>The scaled rectangle.</returns>
        public static Rect operator *(Rect rect, Vector scale)
        {
            double centerX = rect.x + (rect.width / 2);
            double centerY = rect.y + (rect.height / 2);
            double width = rect.width * scale.X;
            double height = rect.height * scale.Y;
            return new Rect(
                centerX - (width / 2),
                centerY - (height / 2),
                width,
                height);
        }

        /// <summary>
        /// Transforms a rectangle by a matrix and returns the axis-aligned bounding box.
        /// </summary>
        /// <param name="rect">The rectangle.</param>
        /// <param name="matrix">The matrix.</param>
        /// <returns>The axis-aligned bounding box.</returns>
        public static Rect operator *(Rect rect, Matrix matrix)
        {
            return new Rect(rect.TopLeft * matrix, rect.BottomRight * matrix);
        }

        /// <summary>
        /// Divides a rectangle by a vector.
        /// </summary>
        /// <param name="rect">The rectangle.</param>
        /// <param name="scale">The vector scale.</param>
        /// <returns>The scaled rectangle.</returns>
        public static Rect operator /(Rect rect, Vector scale)
        {
            double centerX = rect.x + (rect.width / 2);
            double centerY = rect.y + (rect.height / 2);
            double width = rect.width / scale.X;
            double height = rect.height / scale.Y;
            return new Rect(
                centerX - (width / 2),
                centerY - (height / 2),
                width,
                height);
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
        /// Centers another rectangle in this rectangle.
        /// </summary>
        /// <param name="rect">The rectangle to center.</param>
        /// <returns>The centered rectangle.</returns>
        public Rect CenterIn(Rect rect)
        {
            return new Rect(
                this.x + ((this.width - rect.width) / 2),
                this.y + ((this.height - rect.height) / 2),
                rect.width,
                rect.height);
        }

        /// <summary>
        /// Inflates the rectangle.
        /// </summary>
        /// <param name="thickness">The thickness.</param>
        /// <returns>The inflated rectangle.</returns>
        public Rect Inflate(double thickness)
        {
            return this.Inflate(thickness);
        }

        /// <summary>
        /// Inflates the rectangle.
        /// </summary>
        /// <param name="thickness">The thickness.</param>
        /// <returns>The inflated rectangle.</returns>
        public Rect Inflate(Thickness thickness)
        {
            return new Rect(
                new Point(this.x - thickness.Left, this.y - thickness.Top),
                this.Size.Inflate(thickness));
        }

        /// <summary>
        /// Deflates the rectangle.
        /// </summary>
        /// <param name="thickness">The thickness.</param>
        /// <returns>The deflated rectangle.</returns>
        /// <remarks>The deflated rectangle size cannot be less than 0.</remarks>
        public Rect Deflate(double thickness)
        {
            return this.Deflate(new Thickness(thickness / 2));
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
        /// Returns a boolean indicating whether the given object is equal to this rectangle.
        /// </summary>
        /// <param name="obj">The object to compare against.</param>
        /// <returns>True if the object is equal to this rectangle; false otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (obj is Rect)
            {
                return this == (Rect)obj;
            }

            return false;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 23) + this.X.GetHashCode();
                hash = (hash * 23) + this.Y.GetHashCode();
                hash = (hash * 23) + this.Width.GetHashCode();
                hash = (hash * 23) + this.Height.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Gets the intersection of two rectangles.
        /// </summary>
        /// <param name="rect">The other rectangle.</param>
        /// <returns>The intersection.</returns>
        public Rect Intersect(Rect rect)
        {
            double x = Math.Max(this.x, rect.x);
            double y = Math.Max(this.y, rect.y);
            double width = Math.Min(this.Right, rect.Right) - x;
            double height = Math.Min(this.Bottom, rect.Bottom) - y;

            if (width < 0 || height < 0)
            {
                return new Rect(
                    double.PositiveInfinity,
                    double.PositiveInfinity,
                    double.NegativeInfinity,
                    double.NegativeInfinity);
            }
            else
            {
                return new Rect(x, y, width, height);
            }
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
